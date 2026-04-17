using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;
using MWB.Networking.Layer1_Framing.Serialization;
using System.Threading.Channels;

namespace MWB.Networking.Layer1_Framing;

public sealed class NetworkFrameReader : IFrameDecoderSink
{
    private readonly Channel<NetworkFrame> _channel =
        Channel.CreateUnbounded<NetworkFrame>();

    public ValueTask OnFrameDecodedAsync(
        ByteSegments frame,
        CancellationToken ct)
    {
        var decoded = NetworkFrameSerializer.Deserialize(frame);
        _channel.Writer.TryWrite(decoded);
        return ValueTask.CompletedTask;
    }

    public async Task<NetworkFrame> ReadFrameAsync(
        CancellationToken ct)
    {
        return await _channel.Reader.ReadAsync(ct).ConfigureAwait(false);
    }
}
