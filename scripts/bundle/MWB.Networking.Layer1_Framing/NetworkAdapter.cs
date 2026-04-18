namespace MWB.Networking.Layer1_Framing;

public sealed class NetworkAdapter
{
    public NetworkAdapter(
        NetworkFrameWriter frameWriter,
        NetworkFrameReader frameReader)
    {
        this.FrameWriter = frameWriter ?? throw new ArgumentNullException(nameof(frameWriter));
        this.FrameReader = frameReader ?? throw new ArgumentNullException(nameof(frameReader));
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
        ArgumentNullException.ThrowIfNull(frame);
        return FrameWriter.WriteAsync(frame, ct).AsTask();
    }

    /// <summary>
    /// Reads the next decoded NetworkFrame.
    /// Blocks until a frame is available or cancellation is requested.
    /// </summary>
    public Task<NetworkFrame> ReadFrameAsync(
        CancellationToken ct = default)
    {
        return FrameReader.ReadFrameAsync(ct);
    }
}