namespace MWB.Networking.Layer2_Protocol.Session.Api;

public sealed class ProtocolSessionHandle
{
    internal ProtocolSessionHandle(ProtocolSession session)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.Commands = session;
        this.Diagnostics = session;
        this.Lifecycle = session;
        this.Observer = session;
        this.Runtime = session;
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

    public IProtocolSessionLifecycle Lifecycle
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

    /// <summary>
    /// Starts the protocol session, enabling transport I/O and event delivery.
    /// This is a convenience forwarder to <c>ProtocolSessionHandle.Lifecycle.StartAsync</c>.
    /// </summary>
    public Task StartAsync(CancellationToken ct = default)
    {
        return this.Lifecycle.StartAsync(ct);
    }

    public Task WhenReady
        => this.Lifecycle.WhenReady;
}
