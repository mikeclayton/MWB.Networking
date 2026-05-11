using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Driver.Abstractions;
using MWB.Networking.Layer2_Protocol.Adapter;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;

namespace MWB.Networking.Layer2_Protocol.Hosting;

public sealed class ProtocolAdapterBuilder
{
    private ILogger? _logger;
    private OddEvenStreamIdParity _parity = OddEvenStreamIdParity.Odd;

    public ProtocolAdapterBuilder UseLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        return this;
    }

    public ProtocolAdapterBuilder UseStreamIdParity(
          OddEvenStreamIdParity parity)
    {
        _parity = parity;
        return this;
    }

    // Optional convenience
    public ProtocolAdapterBuilder UseOddStreamIds()
        => this.UseStreamIdParity(OddEvenStreamIdParity.Odd);

    public ProtocolAdapterBuilder UseEvenStreamIds()
        => this.UseStreamIdParity(OddEvenStreamIdParity.Even);

    public ProtocolAdapterBuilder UseTransportDriver(INetworkFrameIO _networkFrameIO)
        => this.UseStreamIdParity(OddEvenStreamIdParity.Even);

    public SessionAdapter Build()
    {
        var logger = _logger
            ?? throw new InvalidOperationException("A logger must be configured.");

        var options = new ProtocolSessionOptions(
            new OddEvenStreamIdProvider(_parity));

        var session = new ProtocolSession(logger, options);

        var networkFrameIO = _networkFrameIO
            ?? throw new InvalidOperationException("A transport driver must be configured.");

        var adapter = new SessionAdapter(logger, session);

        return adapter;
    }
}
