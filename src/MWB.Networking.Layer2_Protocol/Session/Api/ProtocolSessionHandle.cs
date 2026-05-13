namespace MWB.Networking.Layer2_Protocol.Session.Api;

internal sealed class ProtocolSessionHandle
{
    internal ProtocolSessionHandle(ProtocolSession session)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.Diagnostics = session;
    }

    internal ProtocolSession Session
    {
        get;
    }

    internal IProtocolSessionDiagnostics Diagnostics
    {
        get;
    }
}
