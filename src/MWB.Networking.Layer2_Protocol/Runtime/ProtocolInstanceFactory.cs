using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer2_Protocol.Driver;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;

namespace MWB.Networking.Layer2_Protocol.Runtime;

/// <summary>
/// Default implementation of <see cref="IProtocolRuntimeFactory"/>.
/// Responsible for atomically constructing a runnable protocol instance
/// (session + driver) from a network pipeline.
/// </summary>
public sealed class ProtocolInstanceFactory : IProtocolInstanceFactory
{
    private readonly ILogger _logger;
    private readonly Func<CancellationToken, Task<NetworkPipeline>> _pipelineFactory;
    private readonly OddEvenStreamIdParity _streamIdParity;
    private readonly Action<ProtocolSessionHandle>? _applyObservers;

    public ProtocolInstanceFactory(
        ILogger logger,
        Func<CancellationToken, Task<NetworkPipeline>> pipelineFactory,
        OddEvenStreamIdParity streamIdParity,
        Action<ProtocolSessionHandle>? applyObservers)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pipelineFactory = pipelineFactory ?? throw new ArgumentNullException(nameof(pipelineFactory));
        _streamIdParity = streamIdParity;
        _applyObservers = applyObservers;
    }

    /// <summary>
    /// Creates a fully wired protocol instance.
    /// </summary>
    public async Task<ProtocolInstance> CreateAsync(CancellationToken cancellationToken)
    {
        // ------------------------------------------------------------
        // 1. Build Layer 1 pipeline (property bag)
        // ------------------------------------------------------------

        var pipeline = await _pipelineFactory(cancellationToken).ConfigureAwait(false);

        // ------------------------------------------------------------
        // 2. Create protocol session (semantic authority)
        // ------------------------------------------------------------

        var sessionConfig = new ProtocolSessionConfig(
            new OddEvenStreamIdProvider(_streamIdParity));

        var session = new ProtocolSession(_logger, sessionConfig);
        var sessionHandle = session.AsHandle();

        // Apply observers before execution begins
        _applyObservers?.Invoke(sessionHandle);

        // ------------------------------------------------------------
        // 3. Create adapter (Layer 1 -> Layer 2 bridge)
        // ------------------------------------------------------------

        var adapter = new NetworkAdapter(
            _logger,
            pipeline.FrameWriter,
            pipeline.FrameReader);

        // ------------------------------------------------------------
        // 4. Create protocol driver (execution engine)
        // ------------------------------------------------------------

        var driver = new ProtocolDriver(
            _logger,
            sessionHandle.Runtime,
            pipeline.Connection,
            pipeline.RootDecoder,
            pipeline.FrameReader,
            adapter);

        // Bind session <-> driver invariant
        session.AttachProtocolDriver(driver);

        // ------------------------------------------------------------
        // Return runnable protocol instance
        // ------------------------------------------------------------

        return new ProtocolInstance(sessionHandle, driver);
    }
}
