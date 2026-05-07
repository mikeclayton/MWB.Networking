using MWB.Networking.Layer0_Transport.Driver.Abstractions;
using MWB.Networking.Layer1_Framing.Codec;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer1_Framing.Driver.Abstractions;
using MWB.Networking.Layer1_Framing.Pipeline;
using System.Buffers;

namespace MWB.Networking.Layer0_Transport.Driver;

/// <summary>
/// Owns transport execution for a single connection.
///
/// Responsibilities:
/// - Read bytes from <see cref="ITransportStack"/> on a dedicated thread-pool task
/// - Accumulate bytes across reads and decode complete <see cref="NetworkFrame"/>s
/// - Encode and send outbound <see cref="NetworkFrame"/>s synchronously
/// - Propagate backpressure via the blocking <see cref="ITransportByteSink.Write"/> contract
/// - Signal clean closure and fault conditions to subscribers
///
/// Non-responsibilities:
/// - No protocol semantics
/// - No session logic
/// - No reconnection or retry policy
/// </summary>
public sealed class TransportDriver :
    INetworkFrameIO,
    IDisposable
{
    private readonly ITransportStack _transport;
    private readonly NetworkPipeline _pipeline;

    private readonly CancellationTokenSource _cts = new();
    private Task? _ioTask;

    private volatile bool _shutdown;

    // ------------------------------------------------------------------
    // Inbound decode accumulation
    // ------------------------------------------------------------------
    // Bytes arrive in arbitrary chunk sizes; a single frame may span
    // several reads. This buffer holds the bytes that have arrived but
    // not yet formed a complete frame.
    //
    // _decodeHead: index of the first unconsumed byte
    // _decodeTail: index one past the last valid byte
    //
    // Invariants:
    //   0 <= _decodeHead <= _decodeTail <= _decodeBuffer.Length
    //   unconsumed byte count = _decodeTail - _decodeHead

    private byte[] _decodeBuffer = new byte[65536];
    private int _decodeHead;
    private int _decodeTail;

    // ------------------------------------------------------------------
    // Events
    // ------------------------------------------------------------------

    /// <summary>
    /// Raised when the transport closes cleanly (EOF).
    /// </summary>
    public event Action? Closed;

    /// <summary>
    /// Raised when the transport faults (read error or invalid frame encoding).
    /// </summary>
    public event Action<Exception>? Faulted;

    /// <summary>
    /// Raised when a complete <see cref="NetworkFrame"/> has been decoded.
    /// Frames are raised in strict arrival order on the driver's I/O task.
    /// </summary>
    public event Action<NetworkFrame>? FrameReceived;

    // ------------------------------------------------------------------
    // Construction
    // ------------------------------------------------------------------

    public TransportDriver(
        ITransportStack transport,
        NetworkPipeline pipeline)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));

        _transport.TransportClosed += OnTransportClosed;
        _transport.TransportFaulted += OnTransportFaulted;
    }

    // ------------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------------

    /// <summary>
    /// Starts the read-and-decode loop. Must be called exactly once.
    /// </summary>
    public void Start()
    {
        _ioTask = Task.Run(ReadAndDecodeLoop);
    }

    // ------------------------------------------------------------------
    // Outbound: SessionAdapter -> Transport
    // ------------------------------------------------------------------

    /// <summary>
    /// Encodes <paramref name="frame"/> and writes the resulting byte segments
    /// to the transport synchronously.
    ///
    /// Backpressure propagates naturally: if <see cref="ITransportByteSink.Write"/>
    /// blocks, this call blocks, which in turn blocks the outbound event chain.
    /// </summary>
    public void Send(NetworkFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ThrowIfShutdown();

        var segments = _pipeline.Encode(frame);
        foreach (var segment in segments)
        {
            _transport.Write(segment.Span);
        }
    }

    // ------------------------------------------------------------------
    // Inbound: single read-and-decode loop
    // ------------------------------------------------------------------

    // Runs on a dedicated thread-pool task (via Task.Run in Start).
    // The blocking transport Read is intentional: it is the correct pattern
    // for a synchronous ITransportByteSource. The thread pool thread is
    // parked waiting for data, which is cheaper than spinning.
    private void ReadAndDecodeLoop()
    {
        var readBuffer = new byte[8192];

        try
        {
            while (!_cts.IsCancellationRequested)
            {
                var bytesRead = _transport.Read(readBuffer);

                if (bytesRead == 0)
                {
                    // Clean EOF from peer
                    HandleClosed();
                    return;
                }

                AppendToDecodeBuffer(readBuffer.AsSpan(0, bytesRead));
                DrainDecodeBuffer();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown path — CTS was cancelled by Dispose or HandleFault
        }
        catch (Exception ex)
        {
            HandleFault(ex);
        }
    }

    // ------------------------------------------------------------------
    // Decode accumulation helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Appends <paramref name="data"/> to the decode buffer, compacting or
    /// growing the backing array as required.
    /// </summary>
    private void AppendToDecodeBuffer(ReadOnlySpan<byte> data)
    {
        var unconsumed = _decodeTail - _decodeHead;

        // If the new data does not fit after the current tail, reclaim space.
        if (_decodeTail + data.Length > _decodeBuffer.Length)
        {
            if (_decodeHead > 0 && unconsumed + data.Length <= _decodeBuffer.Length)
            {
                // Compact: shifting unconsumed bytes to the start is sufficient.
                _decodeBuffer.AsSpan(_decodeHead, unconsumed).CopyTo(_decodeBuffer);
            }
            else
            {
                // Grow: even a fully compacted buffer is too small.
                var newSize = Math.Max(_decodeBuffer.Length * 2, unconsumed + data.Length);
                var newBuffer = new byte[newSize];
                _decodeBuffer.AsSpan(_decodeHead, unconsumed).CopyTo(newBuffer);
                _decodeBuffer = newBuffer;
            }

            _decodeTail = unconsumed;
            _decodeHead = 0;
        }

        data.CopyTo(_decodeBuffer.AsSpan(_decodeTail));
        _decodeTail += data.Length;
    }

    /// <summary>
    /// Repeatedly attempts to decode complete frames from the accumulated
    /// bytes, raising <see cref="FrameReceived"/> for each, until no further
    /// complete frame is available or a fault occurs.
    /// </summary>
    private void DrainDecodeBuffer()
    {
        while (_decodeHead < _decodeTail)
        {
            var available = _decodeTail - _decodeHead;
            var sequence = new ReadOnlySequence<byte>(_decodeBuffer, _decodeHead, available);

            var result = _pipeline.Decode(ref sequence, out var frame);

            // Advance _decodeHead by however many bytes the codec consumed.
            // On NeedsMoreData the codec does not advance the sequence, so
            // consumed is zero and _decodeHead is unchanged.
            var consumed = available - (int)sequence.Length;
            _decodeHead += consumed;

            switch (result)
            {
                case FrameDecodeResult.NeedsMoreData:
                    // Partial frame — wait for the next read.
                    return;

                case FrameDecodeResult.InvalidFrameEncoding:
                    // Protocol-level framing error — fatal for this connection.
                    throw new InvalidOperationException(
                        "Received data that cannot be decoded as a valid frame; " +
                        "the connection will be closed.");

                case FrameDecodeResult.Success:
                    // A well-behaved codec must always consume at least one byte
                    // on success. Zero consumption with a Success result would
                    // cause this loop to spin indefinitely on the same position.
                    if (consumed == 0)
                        throw new InvalidOperationException(
                            "Codec reported Success but consumed zero bytes; " +
                            "this indicates a codec implementation defect.");
                    FrameReceived?.Invoke(frame!);
                    break;
            }
        }

        // Buffer is fully drained — reset pointers to the start to avoid
        // continually creeping toward the end of the array.
        _decodeHead = 0;
        _decodeTail = 0;
    }

    // ------------------------------------------------------------------
    // Transport event handlers
    // ------------------------------------------------------------------

    // These are raised by the transport on its own thread. Both delegate
    // immediately to the shared shutdown helpers which are idempotent.

    private void OnTransportClosed() => HandleClosed();
    private void OnTransportFaulted(Exception ex) => HandleFault(ex);

    // ------------------------------------------------------------------
    // Shutdown helpers
    // ------------------------------------------------------------------

    // Interlocked.Exchange<bool> is available from .NET 10 onward.
    // It returns the previous value: true means someone already won
    // the race to initiate shutdown, so this call is a no-op.

    private void HandleClosed()
    {
        if (Interlocked.Exchange(ref _shutdown, true))
            return;

        _cts.Cancel();
        Closed?.Invoke();
    }

    private void HandleFault(Exception ex)
    {
        if (Interlocked.Exchange(ref _shutdown, true))
            return;

        _cts.Cancel();
        Faulted?.Invoke(ex);
    }

    private void ThrowIfShutdown()
    {
        if (_shutdown)
            throw new InvalidOperationException("TransportDriver is shut down.");
    }

    // ------------------------------------------------------------------
    // Disposal
    // ------------------------------------------------------------------

    // Dispose signals shutdown and detaches transport event subscriptions.
    //
    // Note: if the I/O task is currently blocked on a synchronous
    // ITransportByteSource.Read, cancelling the CTS alone will not
    // unblock it — the transport itself must be closed first to cause Read
    // to return. The expected usage pattern is:
    //
    //   1. Close / dispose the transport (Read returns 0 or throws).
    //   2. The I/O loop exits and fires Closed or Faulted.
    //   3. Dispose the driver.
    //
    // Disposing in isolation is safe: the driver will stop after the
    // current blocking Read returns.

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _shutdown, true))
            return;

        _cts.Cancel();

        _transport.TransportClosed -= OnTransportClosed;
        _transport.TransportFaulted -= OnTransportFaulted;

        _cts.Dispose();
    }
}
