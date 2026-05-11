using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer2_Protocol.Session;

internal sealed partial class ProtocolSession : IProtocolSessionDiagnostics
{
    private IProtocolSessionDiagnostics AsDiagnostics()
        => this;

    ProtocolSnapshot IProtocolSessionDiagnostics.GetSnapshot()
    {
        return new ProtocolSnapshot(
            OpenRequests: this.RequestManager.GetRequestIds().ToArray(),
            OpenStreams: this.StreamManager.GetStreamIds().ToArray());
    }
}
