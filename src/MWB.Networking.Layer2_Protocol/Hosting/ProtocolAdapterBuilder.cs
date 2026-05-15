using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Driver.Abstractions;
using MWB.Networking.Layer2_Protocol.Adapter;

namespace MWB.Networking.Layer2_Protocol.Hosting;

public sealed class SessionAdapterBuilder
{
    // ------------------------------------------------------------------
    // Logger
    // ------------------------------------------------------------------

    private ILogger? _logger;

    public SessionAdapterBuilder UseLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        return this;
    }

    // ------------------------------------------------------------------
    // Session configuration
    // ------------------------------------------------------------------

    private readonly ProtocolSessionBuilder _sessionBuilder
        = new ProtocolSessionBuilder();

    public SessionAdapterBuilder ConfigureSession(
        Action<ProtocolSessionBuilder> configure)
    {
        configure?.Invoke(_sessionBuilder);
        return this;
    }

    // ------------------------------------------------------------------
    // Transport
    // ------------------------------------------------------------------

    private INetworkFrameIO? _transport;

    public SessionAdapterBuilder UseTransportDriver(INetworkFrameIO transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        return this;
    }

    // ------------------------------------------------------------------
    // Build
    // ------------------------------------------------------------------

    public SessionAdapter Build()
    {
        var logger = _logger
            ?? throw new InvalidOperationException("A logger must be configured.");

        var transport = _transport
            ?? throw new InvalidOperationException("A transport driver must be configured.");

        // SessionAdapter will call builder.Build(this, this)
        var adapter = new SessionAdapter(
            logger,
            _sessionBuilder,
            transport);

        return adapter;
    }
}
