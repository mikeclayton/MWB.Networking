using MWB.Networking.Layer0_Transport;
using MWB.Networking.Layer1_Framing.Encoding;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;
using MWB.Networking.Layer1_Framing.Encoding.Helpers;

namespace MWB.Networking.Layer1_Framing.Hosting;

/// <summary>
/// Builds the network encoding/decoding pipeline.
///
/// - Encoders are appended in conceptual order (gzip → aes → framing)
/// - Encoders are constructed in reverse order
/// - Decoders are constructed in forward order
/// - FrameEncoderBridge is always inserted automatically
/// </summary>
public sealed class NetworkPipelineFactory
{
    public NetworkPipelineFactory()
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

    private INetworkConnectionProvider? ConnectionProvider
    {
        get;
        set;
    } = null;

    /// <summary>
    /// Appends a frame encoder to the outbound pipeline and its corresponding
    /// decoder to the inbound pipeline.
    ///
    /// Order matters: encoders are applied in the order they are appended.
    /// </summary>
    public NetworkPipelineFactory AppendFrameCodec(
        IFrameEncoder encoder,
        IFrameDecoder decoder)
    {
        ArgumentNullException.ThrowIfNull(encoder);
        ArgumentNullException.ThrowIfNull(decoder);

        this.FrameEncoders.Add(encoder);
        this.FrameDecoders.Add(decoder);

        return this;
    }

    /// <summary>
    /// Configures the network connection factory used as the terminal
    /// of the pipeline (outbound) and origin (inbound).
    /// </summary>
    public NetworkPipelineFactory UseConnectionProvider(
        INetworkConnectionProvider connectionProvider)
    {
        ArgumentNullException.ThrowIfNull(connectionProvider);

        this.ConnectionProvider = connectionProvider;
        return this;
    }

    /// <summary>
    /// Materializes the pipeline.
    /// </summary>
    public async Task<NetworkPipeline> CreatePipelineAsync(CancellationToken cancellationToken = default)
    {
        // ----------------------------------------------------------
        // Validation
        // ----------------------------------------------------------

        if (this.ConnectionProvider is null)
        {
            throw new InvalidOperationException("No connection provider configured.");
        }

        if (this.FrameEncoders.Count != this.FrameDecoders.Count)
        {
            throw new InvalidOperationException(
                "Encoder / decoder count mismatch.");
        }

        // ----------------------------------------------------------
        // Transport
        // ----------------------------------------------------------

        var connectionHandle = await this.ConnectionProvider
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        // ----------------------------------------------------------
        // Outbound: encoders (reverse construction order)
        // ----------------------------------------------------------

        // terminal point at the end of the pipeline
        var encoderBridge =
            new FrameEncoderBridge(connectionHandle.Connection);

        // build the pipeline and receive the entrypoint
        var encoderSink =
            FramePipelineHelper.BuildEncoderPipeline(
                this.FrameEncoders,
                encoderBridge);

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

        var pipeline = new NetworkPipeline(
            connectionHandle.Connection,
            frameWriter,
            frameReader,
            rootDecoder);

        return pipeline;
    }
}
