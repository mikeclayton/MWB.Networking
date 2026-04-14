namespace MWB.Networking.Layer2_Protocol.Session;

public sealed class ProtocolSessionHandle
{
    internal ProtocolSessionHandle(ProtocolSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        this.Observer = session;
        this.Runtime = session;
        this.Commands = session;
    }

    public IProtocolSessionObserver Observer
    {
        get;
    }

    public IProtocolSessionRuntime Runtime
    {
        get;
    }
    public IProtocolSessionCommands Commands
    {
        get;
    }
}
