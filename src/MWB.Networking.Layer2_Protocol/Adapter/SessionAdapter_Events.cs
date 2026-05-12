using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer2_Protocol.Events.Api;
using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer2_Protocol.Adapter;

public sealed partial class SessionAdapter
{
    public event Action<IncomingEvent, ReadOnlyMemory<byte>>? EventReceived;

    void IIncomingActionSink.PublishIncomingEvent(
        IncomingEvent evt,
        ReadOnlyMemory<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(evt);

        // Adapter owns threading policy for callbacks
        this.EventReceived?.Invoke(evt, payload);
    }

    void IOutgoingActionSink.TransmitOutgoingEvent(
        OutgoingEvent evt,
        ReadOnlyMemory<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(evt);

        var frame = NetworkFrames.Event(
            evt.EventType,
            payload);

        _transport.Send(frame);
    }
}
