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
/// <remarks>
/// Each call to <see cref="Build"/> creates a completely new and isolated
/// protocol session object graph, including the pipeline, driver, queues,
/// observers, and background loops.
///
/// The builder is therefore reusable and may be treated as a session template
/// or factory. No runtime objects are retained or shared between builds.
/// </remarks>
public sealed partial class ProtocolSessionBuilder
{
    /// <summary>
    /// Builds and returns a fully wired protocol session.
    /// </summary>
    public ProtocolSessionHandle Build()
    {
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
            logger,
            pipeline.FrameWriter,
            pipeline.FrameReader);

        var driverOptions = new ProtocolDriverOptions(
            pipeline.Connection,
            pipeline.RootDecoder,
            pipeline.FrameReader,
            adapter);

        // ------------------------------------------------------------
        // Create protocol session (semantic authority)
        // ------------------------------------------------------------

        var session = ProtocolSessions.CreateSession(
            logger,
            _streamIdParity.Value,
            driverOptions
        );

        // ------------------------------------------------------------
        // Configure event handlers
        // ------------------------------------------------------------

        ProtocolSessionBuilder.AssignObservers(session, _observerConfig);

        return session;
    }
}
