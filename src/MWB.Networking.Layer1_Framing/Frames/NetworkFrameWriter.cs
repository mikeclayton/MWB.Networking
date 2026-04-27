using MWB.Networking.Layer1_Framing.Encoding.Abstractions;
using MWB.Networking.Layer1_Framing.Serialization;

namespace MWB.Networking.Layer1_Framing.Frames;

/// <summary>
/// Serves as the head of the NetworkFrame transformation pipeline,
/// converting semantic NetworkFrame instances into their serialized
/// byte representation for further encoding and transport.
/// </summary>
public sealed class NetworkFrameWriter
{
    private readonly IFrameEncoderSink _sink;

    public NetworkFrameWriter(IFrameEncoderSink sink)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    public ValueTask WriteAsync(
        NetworkFrame frame,
        CancellationToken ct)
    {
        var bytes = NetworkFrameSerializer.SerializeFrame(frame);
        return _sink.OnFrameEncodedAsync(bytes, ct);
    }
}
