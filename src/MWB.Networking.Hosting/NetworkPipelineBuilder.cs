using MWB.Networking.Layer0_Transport;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer1_Framing.Encoding;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;
using MWB.Networking.Layer1_Framing.Encoding.Helpers;

namespace MWB.Networking.Hosting;

/// <summary>
/// Builds the network encoding/decoding pipeline.
///
/// - Encoders are appended in conceptual order (gzip → aes → framing)
/// - Encoders are constructed in reverse order
/// - Decoders are constructed in forward order
/// - FrameEncoderBridge is always inserted automatically
/// </summary>
public sealed class NetworkPipelineBuilder
{
    public NetworkPipelineBuilder()
    {
    }

    /// <summary>
    /// Encoders in conceptual (append) order:
    /// e.g. [gzip] -> [aes] -> [length-prefix]
    /// </summary>
    private List<IFrameEncoder> FrameEncoders
    {
        get;
    } = [];

    /// <summary>
    /// Matching decoders in conceptual order:
    /// [length-prefix] -> [aes] -> [gzip]
    /// </summary>
    private List<IFrameDecoder> FrameDecoders
    {
        get;
    } = [];

    private Func<INetworkConnection>? ConnectionFactory
    {
        get;
        set;
    } = null;

    private bool IsBuilt
    {
        get;
        set;
    } = false;


    /// <summary>
    /// Appends a frame encoder to the outbound pipeline and its corresponding
    /// decoder to the inbound pipeline.
    ///
    /// Order matters: encoders are applied in the order they are appended.
    /// </summary>
    public NetworkPipelineBuilder AppendFrameCodec(
        IFrameEncoder encoder,
        IFrameDecoder decoder)
    {
        ArgumentNullException.ThrowIfNull(encoder);
        ArgumentNullException.ThrowIfNull(decoder);
        if (this.IsBuilt)
        {
            throw new InvalidOperationException("Pipeline has already been built.");
        }

        this.FrameEncoders.Add(encoder);
        this.FrameDecoders.Add(decoder);

        return this;
    }

    /// <summary>
    /// Configures the network connection factory used as the terminal
    /// of the pipeline (outbound) and origin (inbound).
    /// </summary>
    public NetworkPipelineBuilder UseConnection(
        Func<INetworkConnection> connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        if (this.IsBuilt)
        {
            throw new InvalidOperationException("Pipeline has already been built.");
        }

        this.ConnectionFactory = connectionFactory;
        return this;
    }

    /// <summary>
    /// Materializes the pipeline.
    /// Internal: only hosting/session builders should call this.
    /// </summary>
    public BuiltNetworkPipeline Build()
    {
        // ----------------------------------------------------------
        // Validation
        // ----------------------------------------------------------

        if (this.ConnectionFactory is null)
        {
            throw new InvalidOperationException("No connection configured.");
        }

        if (this.FrameEncoders.Count != this.FrameDecoders.Count)
        {
            throw new InvalidOperationException(
                "Encoder / decoder count mismatch.");
        }

        // ----------------------------------------------------------
        // Transport
        // ----------------------------------------------------------

        var connection = this.ConnectionFactory();

        // ----------------------------------------------------------
        // Outbound: encoders (reverse construction order)
        // ----------------------------------------------------------

        // Final sink crosses from frames → bytes → transport
        var terminalEncoderSink =
            new FrameEncoderBridge(connection);

        // Delegate actual pipeline composition to Layer 1
        var encoderSink =
            FramePipelineHelper.BuildEncoderPipeline(
                this.FrameEncoders,
                terminalEncoderSink);

        var frameWriter =
            new NetworkFrameWriter(encoderSink);

        // ------------------------------------------------------
        // Inbound: decoders (forward order, wrapping decoders sinks)
        // ------------------------------------------------------

        var frameReader = new NetworkFrameReader();

        // Delegate decoding pipeline composition to Layer 1
        var rootDecoder =
            FramePipelineHelper.BuildDecoderPipeline(
                this.FrameDecoders,
                frameReader);

        // ----------------------------------------------------------
        // Return materialized pipeline
        // ----------------------------------------------------------

        var pipeline = new BuiltNetworkPipeline(
            connection,
            frameWriter,
            frameReader,
            rootDecoder);

        return pipeline;
    }
}