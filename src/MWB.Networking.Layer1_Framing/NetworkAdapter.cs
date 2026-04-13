using MWB.Networking.Layer0_Transport;

namespace MWB.Networking.Layer1_Framing;

public sealed class NetworkAdapter
{
    public NetworkAdapter(
        INetworkConnection connection,
        INetworkFrameWriter frameWriter,
        INetworkFrameReader frameReader)
    {
        this.NetworkConnection = connection ?? throw new ArgumentNullException(nameof(connection));
        this.FrameWriter = frameWriter ?? throw new ArgumentNullException(nameof(frameWriter));
        this.FrameReader = frameReader ?? throw new ArgumentNullException(nameof(frameReader));
    }

    private INetworkConnection NetworkConnection
    {
        get;
    }

    private INetworkFrameWriter FrameWriter
    {
        get;
    }

    private INetworkFrameReader FrameReader
    {
        get;
    }

    /// <summary>
    /// Writes a single NetworkFrame to the underlying connection.
    /// Blocks until the underlying connection is available or throws on failure.
    /// </summary>
    public async Task WriteFrameAsync(
        NetworkFrame frame,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(frame);
        // ensure the transport is connected
        await this.NetworkConnection.WaitUntilConnectedAsync(ct);
        // write a single frame to the connection
        await this.FrameWriter.WriteFrameAsync(this.NetworkConnection, frame, ct);
    }

    /// <summary>
    /// Reads a single NetworkFrame from the underlying connection.
    /// Blocks until a frame is received or throws on disconnect.
    /// </summary>
    public async Task<NetworkFrame> ReadFrameAsync(
        CancellationToken ct = default)
    {
        // ensure the transport is connected
        await this.NetworkConnection.WaitUntilConnectedAsync(ct);
        // read a single frame from the connection
        var frame = await this.FrameReader.ReadFrameAsync(this.NetworkConnection, ct);
        return frame;
    }
}
