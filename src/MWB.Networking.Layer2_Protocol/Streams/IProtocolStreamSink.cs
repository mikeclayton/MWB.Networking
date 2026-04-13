namespace MWB.Networking.Layer2_Protocol.Streams;

internal interface IProtocolStreamSink
{
    void SendData(uint streamId, ReadOnlyMemory<byte> payload);

    void SendClose(uint streamId);

    void SendError(uint streamId, ReadOnlyMemory<byte> payload);
}
