namespace MWB.Networking.Layer2_Protocol.Session.Api;

public sealed class ProtocolSessionHandle
{
    internal ProtocolSessionHandle(ProtocolSession session)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.Commands = session;
        this.Diagnostics = session;
        this.Observer = session;
        this.Processor = session;
    }

    internal ProtocolSession Session
    {
        get;
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

    internal IProtocolSessionProcessor Processor
    {
        get;
    }
}
