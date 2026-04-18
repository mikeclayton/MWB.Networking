using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;

namespace MWB.Networking.Layer1_Framing.Encoding;

internal sealed class PipelineEncoderSink : IFrameEncoderSink
{
    private readonly IFrameEncoder _encoder;
    private readonly IFrameEncoderSink _next;

    public PipelineEncoderSink(
        IFrameEncoder encoder,
        IFrameEncoderSink next)
    {
        _encoder = encoder;
        _next = next;
    }

    public ValueTask OnFrameEncodedAsync(
        ByteSegments frame,
        CancellationToken ct)
    {
        return _encoder.EncodeFrameAsync(frame, _next, ct);
    }
}
