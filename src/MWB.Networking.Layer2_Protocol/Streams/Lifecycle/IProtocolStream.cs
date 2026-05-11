namespace MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

internal interface IProtocolStream
{
    uint StreamId
    {
        get;
    }

    // Internal lifecycle hooks only
    void Close();
}
