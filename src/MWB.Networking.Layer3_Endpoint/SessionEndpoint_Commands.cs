using MWB.Networking.Layer2_Protocol.Session.Requests.Api;
using MWB.Networking.Layer2_Protocol.Session.Streams.Api;

namespace MWB.Networking.Layer3_Endpoint;

public sealed partial class SessionEndpoint
{
    // ------------------------------------------------------------
    // Events
    // ------------------------------------------------------------

    public void SendEvent(ReadOnlyMemory<byte> payload = default)
    {
        this.GetActiveSession().Commands.SendEvent(payload);
    }

    public void SendEvent(uint? eventType = null, ReadOnlyMemory<byte> payload = default)
    {
        this.GetActiveSession().Commands.SendEvent(eventType, payload);
    }

    // ------------------------------------------------------------
    // Requests
    // ------------------------------------------------------------

    public OutgoingRequest SendRequest(ReadOnlyMemory<byte> payload)
    {
        return this.GetActiveSession().Commands.SendRequest(null, payload);
    }

    public OutgoingRequest SendRequest(uint? requestType = null, ReadOnlyMemory<byte> payload = default)
    {
        return this.GetActiveSession().Commands.SendRequest(requestType, payload);
    }

    // ------------------------------------------------------------------
    // Streams
    // ------------------------------------------------------------------

    public OutgoingStream OpenSessionStream(ReadOnlyMemory<byte> metadata)
    {
        return this.GetActiveSession().Commands.OpenSessionStream(null, metadata);
    }

    public OutgoingStream OpenSessionStream(uint? streamType = null, ReadOnlyMemory<byte> metadata = default)
    {
        return this.GetActiveSession().Commands.OpenSessionStream(streamType, metadata);
    }
}
