using MWB.Networking.Layer2_Protocol.Internal;

namespace MWB.Networking.Layer2_Protocol;

public sealed partial class ProtocolSession : IProtocolSession
{

    // ------------------------------------------------------------------
    // Event handling
    // ------------------------------------------------------------------

    public event Action<uint /* EventType */, ReadOnlyMemory<byte> /* Payload */>? EventReceived;

    public void SendEvent(uint eventType, ReadOnlyMemory<byte> payload)
    {
        this.EnqueueOutboundFrame(ProtocolFrames.Event(eventType, payload));
    }

    private void RaiseEventReceived(uint eventType, ReadOnlyMemory<byte> payload)
        => this.EventReceived?.Invoke(eventType, payload);

    private void ProcessEventFrame(ProtocolFrame frame)
    {
        if (frame.EventType is null)
        {
            throw new ProtocolException(
                ProtocolErrorKind.InvalidFrameSequence,
                "Event frame missing EventType");
        }

        // Layer 2 does not interpret events.
        // Just surface them upward (or store them for a higher layer).
        this.RaiseEventReceived(frame.EventType.Value, frame.Payload);
    }
}
