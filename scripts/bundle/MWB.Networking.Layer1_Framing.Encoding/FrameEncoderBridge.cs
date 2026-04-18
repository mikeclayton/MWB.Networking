using MWB.Networking.Layer0_Transport;
using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;

namespace MWB.Networking.Layer1_Framing.Encoding;

/// <summary>
/// Bridges the frame encoder pipeline to the underlying network transport.
/// This type performs no encoding; it only forwards encoded frames to the transport.
/// </summary>
public sealed class FrameEncoderBridge : IFrameEncoderSink
{
    private readonly INetworkConnection _connection;

    public FrameEncoderBridge(INetworkConnection connection)
    {
        _connection = connection
            ?? throw new ArgumentNullException(nameof(connection));
    }

    public ValueTask OnFrameEncodedAsync(
        ByteSegments frame,
        CancellationToken ct)
    {
        return _connection.WriteAsync(frame, ct);
    }
}
