using MWB.Networking.Layer1_Framing.Encoding.Abstractions;

namespace MWB.Networking.Layer1_Framing.Encoding.Helpers;

public static class FramePipelineHelper
{
    /// <summary>
    /// Assembles an encoder pipeline from stateless encoders and a terminal sink.
    /// </summary>
    public static IFrameEncoderSink BuildEncoderPipeline(
        IReadOnlyList<IFrameEncoder> encoders,
        IFrameEncoderSink terminalSink)
    {
        var sink = terminalSink;

        for (int i = encoders.Count - 1; i >= 0; i--)
        {
            sink = new PipelineEncoderSink(encoders[i], sink);
        }

        return sink;
    }

    public static IFrameDecoder BuildDecoderPipeline(
        IReadOnlyList<IFrameDecoder> decoders,
        IFrameDecoderSink terminalSink)
    {
        IFrameDecoderSink sink = terminalSink;

        for (int i = decoders.Count - 1; i >= 0; i--)
        {
            sink = new PipelineDecoderSink(decoders[i], sink);
        }

        return (IFrameDecoder)sink;
    }

}
