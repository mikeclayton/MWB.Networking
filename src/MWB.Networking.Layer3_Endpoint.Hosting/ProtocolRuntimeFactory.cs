using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.Driver;
using MWB.Networking.Layer0_Transport.Stack;
using MWB.Networking.Layer0_Transport.Stack.Abstractions;
using MWB.Networking.Layer1_Framing.Pipeline;
using MWB.Networking.Layer2_Protocol.Adapter;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Session.Streams.Infrastructure;

namespace MWB.Networking.Layer3_Endpoint.Hosting;

/// <summary>
/// Creates a fully wired <see cref="ProtocolRuntime"/> for each connection attempt.
///
/// Responsibilities:
/// - Establish the underlying transport connection.
/// - Instantiate and wire the transport driver, network pipeline,
///   protocol session, and session adapter.
/// - Return the composed <see cref="ProtocolRuntime"/> to <see cref="SessionEndpoint"/>.
///
/// This factory is intentionally stateless with respect to individual runtimes:
/// each <see cref="CreateAsync"/> call produces a completely isolated object graph.
/// </summary>
internal sealed class ProtocolRuntimeFactory : IProtocolRuntimeFactory
{
    private readonly ILogger _logger;
    private readonly INetworkConnectionProvider _connectionProvider;
    private readonly INetworkPipelineFactory _pipelineFactory;
    private readonly OddEvenStreamIdParity _streamIdParity;

    internal ProtocolRuntimeFactory(
        ILogger logger,
        INetworkConnectionProvider connectionProvider,
        INetworkPipelineFactory pipelineFactory,
        OddEvenStreamIdParity streamIdParity)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _pipelineFactory = pipelineFactory ?? throw new ArgumentNullException(nameof(pipelineFactory));
        _streamIdParity = streamIdParity;
    }

    /// <inheritdoc />
    public async Task<ProtocolRuntime> CreateAsync(CancellationToken ct)
    {
        // 1. Establish a transport connection (does not own the provider).
        var transportStack = new TransportStack(_logger, _connectionProvider, ownsProvider: false);
        await transportStack.ConnectAsync(ct).ConfigureAwait(false);
        await transportStack.AwaitConnectedAsync(ct).ConfigureAwait(false);

        // 2. Wrap the async TransportStack in a synchronous ITransportStack
        //    suitable for TransportDriver's dedicated read thread.
        var transportAdapter = new NetworkConnectionTransportAdapter(transportStack);

        // 3. Build the network codec pipeline.
        var pipeline = _pipelineFactory.CreatePipeline();

        // 4. Create the transport driver that owns the read-and-decode loop.
        var driver = new TransportDriver(transportAdapter, pipeline);

        // 5. Create the protocol session.
        var streamIdProvider = new OddEvenStreamIdProvider(_streamIdParity);
        var sessionConfig = new ProtocolSessionConfig(streamIdProvider);
        var session = new ProtocolSession(_logger, sessionConfig);
        var handle = session.AsHandle();

        // 6. Wire the session adapter (bridges protocol frames ↔ network frames).
        var adapter = new SessionAdapter(_logger, handle.FrameIO, driver);

        // 7. Return the composed runtime.
        return new ProtocolRuntime
        {
            Session = handle,
            Adapter = adapter,
            Driver = new TransportDriverAdapter(driver)
        };
    }
}
