using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Logging;
using System.Buffers;
using System.Diagnostics;

namespace MWB.Networking.Layer3_Runtime;

/// <summary>
/// Drives a ProtocolSession using a NetworkAdapter.
///
/// Owns execution, concurrency, cancellation, and lifetime.
/// Contains no protocol semantics and no transport logic beyond
/// driving the byte->frame decoding pipeline.
/// </summary>
public sealed class ProtocolDriver : IHasLogger
{
    public ProtocolDriver(
        ILogger logger,
        INetworkConnection connection,
        IFrameDecoder decoder,
        NetworkFrameReader frameReader,
        NetworkAdapter adapter,
        ProtocolSessionHandle session)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        this.Decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        this.FrameReader = frameReader ?? throw new ArgumentNullException(nameof(frameReader));
        this.Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
    }

#if ENABLE_PROTOCOL_FRAME_DIAGNOSTICS
    private RingBuffer<ProtocolFrame> RecentInboundFrames
    {
        get;
    } = new(capacity: 100_000);

    private RingBuffer<ProtocolFrame> RecentOutboundFrames
    {
        get;
    } = new(capacity: 100_000);
#endif

    public ILogger Logger
    { 
        get;
    }

    private INetworkConnection Connection
    {
        get;
    }
    
    private IFrameDecoder Decoder
    {
        get;
    }

    private NetworkFrameReader FrameReader
    { 
        get;
    }
    
    private NetworkAdapter Adapter
    {
        get;
    }

    private ProtocolSessionHandle Session
    {
        get;
    }

    private SemaphoreSlim SessionGate
    {
        get;
    } = new(1, 1);

    private TaskCompletionSource ReadySource
    {
        get;
    } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Ready
        => this.ReadySource.Task;

    /// <summary>
    /// Runs the protocol driver until cancelled or a fatal error occurs.
    /// </summary>
    [LogMethod]
    public async Task RunAsync(CancellationToken ct)
    {
        using var scope = this.Logger.BeginMethodScope(this);

        var readTask = this.RunReadLoopAsync(ct);
        var writeTask = this.RunWriteLoopAsync(ct);

        this.ReadySource.TrySetResult();

        await Task.WhenAny(readTask, writeTask).ConfigureAwait(false);
    }

    /// <summary>
    /// Drives the transport read loop and feeds bytes into the frame decoder.
    /// Decoded frames are delivered asynchronously via NetworkFrameReader.
    /// </summary>
    [LogMethod]
    private async Task RunReadLoopAsync(CancellationToken ct)
    {
        using var scope = this.Logger.BeginMethodScope(this);
        this.Logger.LogDebug("[DRIVER READ LOOP] entering");

        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                int bytesRead;
                try
                {
                    bytesRead = await Connection
                        .ReadAsync(buffer, ct)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    this.Logger.LogDebug("[DRIVER READ LOOP] cancelled");
                    return;
                }

                if (bytesRead == 0)
                {
                    this.Logger.LogDebug("[DRIVER READ LOOP] EOF");
                    return;
                }

                var sequence = new ReadOnlySequence<byte>(
                    buffer.AsMemory(0, bytesRead));

                await this.Decoder
                    .DecodeFrameAsync(sequence, this.FrameReader, ct)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            this.Logger.LogDebug("[DRIVER READ LOOP] leaving");
        }
    }

    /// <summary>
    /// Continuously drains outbound frames from the protocol session
    /// and writes them to the adapter.
    /// </summary>
    [LogMethod]
    private async Task RunWriteLoopAsync(CancellationToken ct)
    {
        using var scope = this.Logger.BeginMethodScope(this);

        while (!ct.IsCancellationRequested)
        {
            await this.Session.Runtime
                .WaitForOutboundFrameAsync(ct)
                .ConfigureAwait(false);

            ProtocolFrame? protocolFrame;

            await this.SessionGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!this.Session.Runtime.TryDequeueOutboundFrame(out protocolFrame))
                {
                    protocolFrame = null;
                }
            }
            finally
            {
                this.SessionGate.Release();
            }

            if (protocolFrame is null)
            {
                await Task.Yield();
                continue;
            }

#if ENABLE_PROTOCOL_FRAME_DIAGNOSTICS
            RecentOutboundFrames.Write(protocolFrame);
#endif

            var networkFrame = FrameConverter.ToNetworkFrame(protocolFrame);

            try
            {
                await this.Adapter
                    .WriteFrameAsync(networkFrame, ct)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Consumes decoded NetworkFrames from the adapter and feeds them
    /// into the protocol session.
    /// </summary>
    [LogMethod]
    public async Task ConsumeFramesAsync(CancellationToken ct)
    {
        using var scope = this.Logger.BeginMethodScope(this);

        while (!ct.IsCancellationRequested)
        {
            NetworkFrame networkFrame =
                await this.Adapter.ReadFrameAsync(ct).ConfigureAwait(false);

            var protocolFrame = FrameConverter.ToProtocolFrame(networkFrame);

#if ENABLE_PROTOCOL_FRAME_DIAGNOSTICS
            protocolFrame.Diagnostics.ReceivedTimestamp = Stopwatch.GetTimestamp();
            this.RecentInboundFrames.Write(protocolFrame);
#endif

            await this.SessionGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                this.Session.Runtime.ProcessFrame(protocolFrame);
            }
            finally
            {
                this.SessionGate.Release();
            }
        }
    }
}

