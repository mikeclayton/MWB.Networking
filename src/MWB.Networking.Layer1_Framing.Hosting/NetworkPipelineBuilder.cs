using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;
using MWB.Networking.Layer1_Framing.Pipeline;

namespace MWB.Networking.Layer1_Framing.Hosting;

public sealed class NetworkPipelineBuilder
{
    public NetworkPipelineBuilder()
    {
    }

    // ------------------------------------------------------------
    // Logger
    // ------------------------------------------------------------

    private ILogger? _logger;

    /// <summary>
    /// Configures the logger.
    /// </summary>
    public NetworkPipelineBuilder UseLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        return this;
    }

    // ------------------------------------------------------------
    // Codecs
    // ------------------------------------------------------------

    /// <summary>
    /// A codec is a conceptual encoder and decoder pair:
    /// e.g.
    ///
    /// encoders: [gzip] -> [aes] -> [length-prefix]
    /// decoders: [length-prefix] -> [aes] -> [gzip]
    /// </summary>
    private readonly List<(IFrameEncoder Encoder, IFrameDecoder Decoder)>
        _codecs = [];

    /// <summary>
    /// Appends a frame codec pair to the pipeline.
    ///
    /// Encoders are applied in append order.
    /// Decoders are applied in reverse order.
    /// </summary>
    public NetworkPipelineBuilder AppendFrameCodec(
        IFrameEncoder encoder,
        IFrameDecoder decoder)
    {
        ArgumentNullException.ThrowIfNull(encoder);
        ArgumentNullException.ThrowIfNull(decoder);
        _codecs.Add((encoder, decoder));
        return this;
    }

    // ------------------------------------------------------------
    // ConnectionProvider
    // ------------------------------------------------------------

    private INetworkConnectionProvider? _connectionProvider;

    /// <summary>
    /// Configures the network connection factory used as the terminal
    /// of the pipeline (outbound) and origin (inbound).
    /// </summary>
    public NetworkPipelineBuilder UseConnectionProvider(
        INetworkConnectionProvider connectionProvider)
    {
        ArgumentNullException.ThrowIfNull(connectionProvider);
        _connectionProvider = connectionProvider;
        return this;
    }

    // ------------------------------------------------------------
    // Materialization
    // ------------------------------------------------------------

    public INetworkPipelineFactory BuildFactory()
    {
        if (_logger is null)
        {
            throw new InvalidOperationException("No logger configured.");
        }

        if (_connectionProvider is null)
        {
            throw new InvalidOperationException("No connection provider configured.");
        }

        return new NetworkPipelineFactory(
            _logger,
            _connectionProvider,
            _codecs);
    }
}
