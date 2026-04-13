namespace MWB.Networking.Layer2_Protocol.Requests;

internal interface IProtocolRequestSink
{
    void SendResponse(uint requestId, ReadOnlyMemory<byte> payload);

    void SendError(uint requestId, ReadOnlyMemory<byte> payload);

    void SendCancel(uint requestId);

    void CompleteRequest(uint requestId);
}
