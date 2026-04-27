using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Pipeline;
using MWB.Networking.Layer2_Protocol.Driver;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;

namespace MWB.Networking.Layer2_Protocol.Runtime;

/// <summary>
/// Assembles protocol execution internals (session, driver, pipeline).
///
/// This type performs construction only; it has no lifecycle, policy,
/// or execution responsibilities.
/// </summary>
public sealed class ProtocolRuntimeFactory : IProtocolRuntimeFactory
{
    private readonly ILogger _logger;
    private readonly INetworkPipelineFactory _pipelineFactory;
    private readonly OddEvenStreamIdParity _streamIdParity;

    public ProtocolRuntimeFactory(
        ILogger logger,
        INetworkPipelineFactory pipelineFactory,
        OddEvenStreamIdParity streamIdParity)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pipelineFactory = pipelineFactory ?? throw new ArgumentNullException(nameof(pipelineFactory));
        _streamIdParity = streamIdParity;
    }

    public async Task<(
        ProtocolSessionHandle Session,
        ProtocolDriver Driver,
        NetworkPipeline Pipeline
    )> CreateAsync(CancellationToken cancellationToken = default)
    {
        // ------------------------------------------------------------
        // 1. Build Layer 1 pipeline
        // ------------------------------------------------------------

        var pipeline =
            await _pipelineFactory
                .CreatePipelineAsync(cancellationToken)
                .ConfigureAwait(false);

        // ------------------------------------------------------------
        // 2. Create protocol session (pure semantics)
        // ------------------------------------------------------------

        var sessionConfig =
            new ProtocolSessionConfig(
                new OddEvenStreamIdProvider(_streamIdParity));

        var session =
            new ProtocolSession(_logger, sessionConfig).AsHandle();

        // ------------------------------------------------------------
        // 3. Create protocol driver (execution engine)
        // ------------------------------------------------------------

        var driver =
            new ProtocolDriver(
                _logger,
                session.Processor,
                pipeline);

        // ------------------------------------------------------------
        // 4. Return assembled internals
        // ------------------------------------------------------------

        return (session, driver, pipeline);
    }
}
