using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;

namespace MWB.Networking.Layer2_Protocol.Hosting;

public sealed class ProtocolSessionBuilder
{
    private ILogger? _logger;
    private OddEvenStreamIdParity _parity = OddEvenStreamIdParity.Odd;

    public ProtocolSessionBuilder UseLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        return this;
    }

    public ProtocolSessionBuilder UseStreamIdParity(
          OddEvenStreamIdParity parity)
    {
        _parity = parity;
        return this;
    }

    // Optional convenience
    public ProtocolSessionBuilder UseOddStreamIds()
        => this.UseStreamIdParity(OddEvenStreamIdParity.Odd);

    public ProtocolSessionBuilder UseEvenStreamIds()
        => this.UseStreamIdParity(OddEvenStreamIdParity.Even);

    public ProtocolSessionHandle Build()
    {
        var logger = _logger
            ?? throw new InvalidOperationException("A logger must be configured.");

        var options = new ProtocolSessionOptions(
            new OddEvenStreamIdProvider(_parity));

        var session = new ProtocolSession(logger, options);

        return session.AsHandle();
    }
}
