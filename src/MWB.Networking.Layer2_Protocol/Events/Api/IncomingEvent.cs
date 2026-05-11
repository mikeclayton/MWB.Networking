namespace MWB.Networking.Layer2_Protocol.Events.Api;

public sealed class IncomingEvent
{
    internal IncomingEvent(
        uint? eventType)
    {
        this.EventType = eventType;
    }

    public uint? EventType
    {
        get;
    }
}
