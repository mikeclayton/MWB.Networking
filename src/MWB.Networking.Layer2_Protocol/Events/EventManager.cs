using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Events.Api;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Logging;

namespace MWB.Networking.Layer2_Protocol.Events;

internal sealed partial class EventManager : IHasLogger
{
    internal EventManager(ILogger logger, ProtocolSession session)
    {
        this.Logger = logger ?? throw new ArgumentOutOfRangeException(nameof(logger));
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public ILogger Logger
    {
        get;
    }
    private ProtocolSession Session
    {
        get;
    }

    // ------------------------------------------------------------------
    // Event handling
    // ------------------------------------------------------------------

    internal void SendOutboundEvent(uint? eventType, ReadOnlyMemory<byte> payload)
    {
        this.Session.SendOutboundFrame(
            ProtocolFrames.Event(eventType, payload));
    }

    internal void ProcessInboundEventFrame(ProtocolFrame frame)
    {
        if (frame.EventType is null)
        {
            throw new ProtocolException(
                ProtocolErrorKind.InvalidFrameSequence,
                "Event frame missing EventType");
        }

        var eventType = frame.EventType;
        var incomingEvent = new IncomingEvent(this.Session, eventType);

        // Layer 2 does not interpret events.
        // Just surface them upward (or store them for a higher layer).

        this.Session.OnEventReceived(incomingEvent, frame.Payload);
    }
}
