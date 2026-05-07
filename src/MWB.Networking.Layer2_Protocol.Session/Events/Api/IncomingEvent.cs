namespace MWB.Networking.Layer2_Protocol.Session.Events.Api;

public sealed class IncomingEvent
{
    internal IncomingEvent(
        ProtocolSession session,
        uint? eventType)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.EventType = eventType;
    }

    internal ProtocolSession Session
    {
        get;
    }

    public uint? EventType
    {
        get;
    }
}
