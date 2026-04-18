using MWB.Networking.Layer0_Transport.Encoding;

namespace MWB.Networking.Layer1_Framing.Encoding.Abstractions;

/// <summary>
/// Receives decoded frame bytes from an upstream decoder.
/// </summary>
public interface IFrameDecoderSink
{
    ValueTask OnFrameDecodedAsync(
         ByteSegments frame, CancellationToken ct);
}
