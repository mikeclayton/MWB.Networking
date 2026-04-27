using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport;
using MWB.Networking.Layer1_Framing.Encoding;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;
using MWB.Networking.Layer1_Framing.Encoding.Helpers;
using MWB.Networking.Layer1_Framing.Frames;

namespace MWB.Networking.Layer1_Framing.Pipeline;

/// <summary>
/// Builds the network encoding/decoding pipeline.
///
/// - Encoders are appended in conceptual order (gzip → aes → framing)
/// - Encoders are constructed in reverse order
/// - Decoders are constructed in forward order
/// - FrameEncoderBridge is always inserted automatically
/// </summary>
public sealed class NetworkPipelineFactory : INetworkPipelineFactory
{
    public NetworkPipelineFactory(
        ILogger logger,
        INetworkConnectionProvider connectionProvider,
        IEnumerable<(IFrameEncoder Encoder, IFrameDecoder Decoder)> codecs)
    {
        this.Logger = logger ?? throw new InvalidOperationException(nameof(logger));
        this.Codecs = (codecs ?? throw new InvalidOperationException(nameof(codecs))).ToList().AsReadOnly();
        this.ConnectionProvider = connectionProvider;
    }

    private ILogger Logger
    {
        get;
    }

    private IReadOnlyList<(IFrameEncoder Encoder, IFrameDecoder Decoder)> Codecs
    {
        get;
    }

    private INetworkConnectionProvider ConnectionProvider
    {
        get;
    }

    /// <summary>
    /// Materializes the pipeline.
    /// </summary>
    public async Task<NetworkPipeline> CreatePipelineAsync(CancellationToken cancellationToken = default)
    {
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
                this.Codecs.Select(c => c.Encoder).ToList(),
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
                this.Codecs.Select(c => c.Decoder).ToList(),
                frameReader);

        // ----------------------------------------------------------
        // Return materialized pipeline
        // ----------------------------------------------------------

        var pipeline = new NetworkPipeline(
            this.Logger,
            connectionHandle.Connection,
            frameWriter,
            frameReader,
            rootDecoder);

        return pipeline;
    }
}
