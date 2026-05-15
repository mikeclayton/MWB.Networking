namespace MWB.Networking.Layer2_Protocol.Session.Api;

internal sealed class SessionDiagnostics
{
    internal SessionDiagnostics(ProtocolSession session)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
    }

    private ProtocolSession Session
    {
        get;
    }

    internal ProtocolSnapshot GetSnapshot()
    {
        return new ProtocolSnapshot(
            OpenRequests: this.Session.RequestManager.GetRequestIds().ToArray(),
            OpenStreams: this.Session.StreamManager.GetStreamIds().ToArray());
    }
}
