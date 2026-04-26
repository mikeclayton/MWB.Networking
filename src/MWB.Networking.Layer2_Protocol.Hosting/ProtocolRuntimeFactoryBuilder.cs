using MWB.Networking.Layer2_Protocol.Runtime;
using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer2_Protocol.Hosting;

public sealed partial class ProtocolRuntimeFactoryBuilder
{
    public IProtocolInstanceFactory Build()
    {
        if (_logger is null)
        {
            throw new InvalidOperationException(
                "Logger not configured.");
        }

        // ------------------------------------------------------------
        // Protocol Session
        // ------------------------------------------------------------

        if (_streamIdParity is null)
        {
            throw new InvalidOperationException(
                "Stream ID parity not configured.");
        }

        // ------------------------------------------------------------
        // Protocol Driver
        // ------------------------------------------------------------

        if (_pipelineFactory is null)
        {
            throw new InvalidOperationException(
                "No pipeline factory configured.");
        }

        // Adapt observer configuration (hosting -> core boundary)
        Action<ProtocolSessionHandle>? applyObservers = null;
        if (_observerConfig is not null)
        {
            applyObservers =
                session => _observerConfig.ApplyObservers(session);
        }

        return new ProtocolInstanceFactory(
            _logger,
            _pipelineFactory,
            _streamIdParity.Value,
            applyObservers);
    }
}
