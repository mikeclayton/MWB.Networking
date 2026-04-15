using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Session;

namespace MWB.Networking.Layer2_Protocol.Events;

internal sealed partial class EventManager
{
    internal EventManager(ProtocolSession session)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
    }

    private ProtocolSession Session
    {
        get;
    }

    // ------------------------------------------------------------------
    // Event handling
    // ------------------------------------------------------------------

    internal void SendEvent(uint eventType, ReadOnlyMemory<byte> payload)
    {
        this.Session.EnqueueOutboundFrame(ProtocolFrames.Event(eventType, payload));
    }

    internal void ProcessEventFrame(ProtocolFrame frame)
    {
        if (frame.EventType is null)
        {
            throw new ProtocolException(
                ProtocolErrorKind.InvalidFrameSequence,
                "Event frame missing EventType");
        }

        // Layer 2 does not interpret events.
        // Just surface them upward (or store them for a higher layer).
        this.Session.OnEventReceived(frame.EventType.Value, frame.Payload);
    }
}
