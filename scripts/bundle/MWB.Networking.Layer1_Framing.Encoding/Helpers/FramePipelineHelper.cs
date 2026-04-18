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
        ArgumentNullException.ThrowIfNull(encoders);
        ArgumentNullException.ThrowIfNull(terminalSink);

        var sink = terminalSink;

        // Empty encoder list is valid:
        // frames flow straight into the terminal sink.
        for (int i = encoders.Count - 1; i >= 0; i--)
        {
            var encoder = encoders[i]
                ?? throw new ArgumentException(
                    "Encoder list contains a null entry.",
                    nameof(encoders));

            sink = new PipelineEncoderSink(encoder, sink);
        }

        return sink;
    }
    public static IFrameDecoder BuildDecoderPipeline(
        IReadOnlyList<IFrameDecoder> decoders,
        IFrameDecoderSink terminalSink)
    {
        ArgumentNullException.ThrowIfNull(decoders);
        ArgumentNullException.ThrowIfNull(terminalSink);

        if (decoders.Count == 0)
        {
            throw new ArgumentException(
                "At least one decoder must be provided.",
                nameof(decoders));
        }

        // Start with the terminal sink (e.g. NetworkFrameReader)
        var sink = terminalSink;

        // Wrap decoders from LAST to SECOND
        for (int i = decoders.Count - 1; i > 0; i--)
        {
            sink = new PipelineDecoderSink(decoders[i], sink);
        }

        // The FIRST decoder is the root entry point
        return decoders[0];
    }
}
