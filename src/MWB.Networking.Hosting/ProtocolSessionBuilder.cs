using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer2_Protocol.Driver;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Hosting;

/// <summary>
/// Application-facing builder for protocol sessions.
///
/// This type owns network pipeline wiring and delegates
/// final session creation to the protocol layer.
/// </summary>
public sealed partial class ProtocolSessionBuilder
{
    private bool _built;

    private void EnsureNotBuilt()
    {
        if (_built)
        {
            throw new InvalidOperationException("Builder already used.");
        }
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
            new ProtocolDriverOptions(
                pipeline.Connection,
                pipeline.RootDecoder,
                pipeline.FrameReader,
                adapter)
        );

        return session;
    }
}
