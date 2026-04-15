using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer2_Protocol.Session.Api;

public sealed class ProtocolSessionHandle
{
    internal ProtocolSessionHandle(ProtocolSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        this.Commands = session;
        this.Diagnostics = session;
        this.Observer = session;
        this.Runtime = session;
    }

    public IProtocolSessionCommands Commands
    {
        get;
    }

    internal IProtocolSessionDiagnostics Diagnostics
    {
        get;
    }

    public IProtocolSessionObserver Observer
    {
        get;
    }

    public IProtocolSessionRuntime Runtime
    {
        get;
    }
}
