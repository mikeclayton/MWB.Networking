using Microsoft.Extensions.Logging;
using MWB.Networking.Logging;

namespace MWB.Networking.Layer1_Framing;

public sealed class NetworkAdapter
{
    public NetworkAdapter(
        ILogger logger,
        NetworkFrameWriter frameWriter,
        NetworkFrameReader frameReader)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.FrameWriter = frameWriter ?? throw new ArgumentNullException(nameof(frameWriter));
        this.FrameReader = frameReader ?? throw new ArgumentNullException(nameof(frameReader));
    }

    public ILogger Logger
    {
        get;
    }

    private NetworkFrameWriter FrameWriter
    {
        get;
    }

    private NetworkFrameReader FrameReader
    {
        get;
    }

    /// <summary>
    /// Writes a single NetworkFrame to the encoding pipeline.
    /// Completion indicates the frame has been handed off to the transport sink.
    /// </summary>
    public Task WriteFrameAsync(
        NetworkFrame frame,
        CancellationToken ct = default)
    {
        using var loggerScope = this.Logger.EnterMethod(this);
        this.Logger.LogDebug(
            "kind: {FrameKind}, eventType: {EventType}, requestId: {RequestId}, streamId: {StreamId}",
            frame.Kind, frame.EventType, frame.RequestId, frame.StreamId);

        ArgumentNullException.ThrowIfNull(frame);
        var writeFrameTask = this.FrameWriter.WriteAsync(frame, ct).AsTask();

        this.Logger.LeaveMethod();

        return writeFrameTask;
    }

    /// <summary>
    /// Reads the next decoded NetworkFrame.
    /// Blocks until a frame is available or cancellation is requested.
    /// </summary>
    public Task<NetworkFrame> ReadFrameAsync(
        CancellationToken ct = default)
    {
        using var loggerScope = this.Logger.BeginMethodLoggingScope(this);

        return FrameReader.ReadFrameAsync(ct);
    }
}