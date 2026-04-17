using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer2_Protocol.Driver;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;

namespace MWB.Networking.Hosting;

/// <summary>
/// Application-facing builder for protocol sessions.
///
/// This type owns network pipeline wiring and delegates
/// final session creation to the protocol layer.
/// </summary>
public sealed class ProtocolSessionBuilder
{
    private ILogger? _logger;
    private Action<NetworkPipelineBuilder>? _pipelineConfig;
    private OddEvenStreamIdParity? _streamIdParity;
    private bool _built;

    /// <summary>
    /// Sets the logger used by the protocol runtime.
    /// </summary>
    public ProtocolSessionBuilder WithLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this.EnsureNotBuilt();

        _logger = logger;
        return this;
    }

    public ProtocolSessionBuilder UseOddEvenStreamIdParity(
        OddEvenStreamIdParity parity)
    {
        _streamIdParity = parity;
        return this;
    }

    // Optional convenience
    public ProtocolSessionBuilder UseOddStreamIds()
        => UseOddEvenStreamIdParity(OddEvenStreamIdParity.Odd);

    public ProtocolSessionBuilder UseEvenStreamIds()
        => UseOddEvenStreamIdParity(OddEvenStreamIdParity.Even);

    private void EnsureNotBuilt()
    {
        if (_built)
        {
            throw new InvalidOperationException("Builder already used.");
        }
    }

    /// <summary>
    /// Configures the network pipeline (encoders, transport).
    /// </summary>
    public ProtocolSessionBuilder ConfigurePipeline(
        Action<NetworkPipelineBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        this.EnsureNotBuilt();

        _pipelineConfig = configure;
        return this;
    }

    /// <summary>
    /// Builds and returns a fully wired protocol session.
    /// </summary>
    public ProtocolSessionHandle Build()
    {
        this.EnsureNotBuilt();
        _built = true;

        if (_pipelineConfig is null)
        {
            throw new InvalidOperationException(
                "Network pipeline not configured. Call ConfigurePipeline().");
        }

        if (_streamIdParity is null)
        {
            throw new InvalidOperationException(
                "Stream ID parity not configured. Call UseOddStreamIds() or UseEvenStreamIds().");
        }

        var logger = _logger ?? NullLogger.Instance;

        // ------------------------------------------------------------
        // Build network pipeline (Layer 1)
        // ------------------------------------------------------------

        var pipelineBuilder = new NetworkPipelineBuilder();
        _pipelineConfig(pipelineBuilder);

        var pipeline = pipelineBuilder.Build();

        // ------------------------------------------------------------
        // Adapt framing into protocol-friendly surface
        // ------------------------------------------------------------

        var adapter = new NetworkAdapter(
            pipeline.FrameWriter,
            pipeline.FrameReader);

        // ------------------------------------------------------------
        // Create protocol session (semantic authority)
        // ------------------------------------------------------------

        var session = ProtocolSessions.CreateSession(
            logger,
            _streamIdParity.Value,
            runtime => new ProtocolDriver(
                logger,
                pipeline.Connection,
                pipeline.RootDecoder,
                pipeline.FrameReader,
                adapter,
                runtime));

        return session;
    }
}