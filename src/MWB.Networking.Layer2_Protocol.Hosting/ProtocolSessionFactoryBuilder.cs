using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer2_Protocol.Hosting;

public sealed partial class ProtocolSessionFactoryBuilder
{
    public ProtocolSessionFactory Build()
    {
        if (_logger is null)
        {
            throw new InvalidOperationException(
                "Logger not configured.");
        }

        if (_streamIdParity is null)
        {
            throw new InvalidOperationException(
                "Stream ID parity not configured.");
        }

        // Adapt observer configuration (hosting -> core boundary)
        Action<ProtocolSessionHandle>? applyObservers = null;
        if (_observerConfig is not null)
        {
            applyObservers =
                session => _observerConfig.ApplyObservers(session);
        }

        return new ProtocolSessionFactory(
            _logger,
            _streamIdParity.Value,
            applyObservers);
    }
}
