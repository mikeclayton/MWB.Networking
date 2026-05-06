using MWB.Networking.Layer0_Transport.Driver.Abstractions;
using MWB.Networking.Layer0_Transport.Segmented;
using MWB.Networking.Layer1_Framing.Codec;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer1_Framing.Driver.Abstractions;
using MWB.Networking.Layer1_Framing.Pipeline;

namespace MWB.Networking.Layer0_Transport.Driver;

/// <summary>
/// Owns transport execution for a single connection.
///
/// Responsibilities:
/// - Read bytes from ITransportStack
/// - Buffer bytes (SegmentedBuffer)
/// - Decode NetworkFrames (NetworkPipeline)
/// - Encode NetworkFrames to bytes
/// - Enforce ordering and backpressure
/// - React to transport closure or faults
///
/// Non-responsibilities:
/// - No protocol semantics
/// - No session logic
/// - No reconnection / retry policy
/// </summary>
internal sealed class TransportDriver :
    INetworkFrameSink,
    INetworkFrameSource,
    IDisposable
{
    private readonly ITransportStack _transport;
    private readonly NetworkPipeline _pipeline;
    private readonly SegmentedBuffer _buffer;

    private readonly CancellationTokenSource _cts = new();
    private Task? _readTask;
    private Task? _decodeTask;

    private volatile bool _shutdown;

    // ------------------------------------------------------------------
    // Events
    // ------------------------------------------------------------------

    /// <summary>
    /// Raised when the transport closes cleanly.
    /// </summary>
    public event Action? Closed;

    /// <summary>
    /// Raised when the transport faults.
    /// </summary>
    public event Action<Exception>? Faulted;

    /// <summary>
    /// Raised when a decoded NetworkFrame is available.
    /// </summary>
    public event Action<NetworkFrame>? FrameReceived;

    // ------------------------------------------------------------------
    // Construction
    // ------------------------------------------------------------------

    internal TransportDriver(
        ITransportStack transport,
        NetworkPipeline pipeline,
        SegmentedBuffer buffer)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));

        // Subscribe to factual transport signals
        _transport.TransportClosed += OnTransportClosed;
        _transport.TransportFaulted += OnTransportFaulted;
    }

    // ------------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------------

    /// <summary>
    /// Starts transport execution (read + decode loops).
    /// Must be called exactly once.
    /// </summary>
    internal void Start()
    {
        _readTask = Task.Run(ReadLoopAsync);
        _decodeTask = Task.Run(DecodeLoopAsync);
    }

    // ------------------------------------------------------------------
    // Outbound: SessionAdapter -> Transport
    // ------------------------------------------------------------------

    /// <summary>
    /// Encodes and sends a NetworkFrame synchronously.
    /// Backpressure is applied via ITransportStack.Write.
    /// </summary>
    public void Send(NetworkFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ThrowIfShutdown();

        // Pure encode: no side effects
        var segments = _pipeline.Encode(frame);

        // Backpressure propagates here
        foreach (var segment in segments)
        {
            _transport.Write(segment.Span);
        }
    }

    // ------------------------------------------------------------------
    // Inbound: Transport -> Buffer
    // ------------------------------------------------------------------

    private async Task ReadLoopAsync()
    {
        var writer = _buffer.Writer;
        var readBuffer = new byte[8192];

        try
        {
            while (!_cts.IsCancellationRequested)
            {
                int bytesRead = _transport.Read(readBuffer);

                if (bytesRead == 0)
                {
                    // Clean EOF
                    writer.Complete();
                    HandleClosed();
                    return;
                }

                await writer.WriteAsync(
                    readBuffer.AsMemory(0, bytesRead),
                    _cts.Token);
            }
        }
        catch (Exception ex)
        {
            writer.Complete();      // buffer signals EOF only
            HandleFault(ex);        // driver owns the cause
        }
    }

    // ------------------------------------------------------------------
    // Inbound: Buffer -> Pipeline -> FrameReceived
    // ------------------------------------------------------------------

    private async Task DecodeLoopAsync()
    {
        var reader = _buffer.Reader;
        var readBuffer = new byte[8192];

        try
        {
            while (true)
            {
                int bytesRead =
                    await reader.ReadAsync(readBuffer, _cts.Token);

                if (bytesRead == 0)
                {
                    // End-of-stream
                    return;
                }

                // Append new bytes to the decode sequence
                AppendToDecodeSequence(
                    readBuffer.AsMemory(0, bytesRead));

                // Attempt to decode frames
                while (true)
                {
                    var sequence = _decodeSequence;

                    var result = _pipeline.Decode(
                        ref sequence,
                        out NetworkFrame? frame);

                    if (result == FrameDecodeResult.NeedsMoreData)
                        break;

                    // Advance the decode buffer
                    _decodeSequence = sequence;

                    if (frame is not null)
                    {
                        FrameReceived?.Invoke(frame);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
    }

    // ------------------------------------------------------------------
    // Transport event handlers
    // ------------------------------------------------------------------

    private void OnTransportClosed()
    {
        _buffer.Writer.Complete();
        HandleClosed();
    }

    private void OnTransportFaulted(Exception ex)
    {
        _buffer.Writer.Complete();
        HandleFault(ex);
    }

    // ------------------------------------------------------------------
    // Shutdown / fault handling
    // ------------------------------------------------------------------

    private void HandleClosed()
    {
        if (_shutdown)
            return;

        _shutdown = true;
        _cts.Cancel();

        Closed?.Invoke();
    }

    private void HandleFault(Exception ex)
    {
        if (_shutdown)
            return;

        _shutdown = true;
        _cts.Cancel();

        Faulted?.Invoke(ex);
    }

    private void ThrowIfShutdown()
    {
        if (_shutdown)
            throw new InvalidOperationException(
                "TransportDriver is shut down.");
    }

    // ------------------------------------------------------------------
    // Teardown
    // ------------------------------------------------------------------

    public void Dispose()
    {
        if (_shutdown)
            return;

        _shutdown = true;

        _cts.Cancel();

        _transport.TransportClosed -= OnTransportClosed;
        _transport.TransportFaulted -= OnTransportFaulted;

        _cts.Dispose();
    }
}