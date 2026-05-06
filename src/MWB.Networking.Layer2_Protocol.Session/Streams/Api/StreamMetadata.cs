namespace MWB.Networking.Layer2_Protocol.Session.Streams.Api;

/// <summary>
/// Application-defined metadata associated with a stream.
/// 
/// The protocol treats this payload as opaque and does not interpret,
/// validate, or transform it. Applications are free to define their own
/// metadata formats (e.g. filesystem metadata, clipboard formats,
/// compression hints, database schemas, etc).
public sealed class StreamMetadata
{
    public StreamMetadata(ReadOnlyMemory<byte> payload)
    {
        this.Payload = payload;
    }

    public ReadOnlyMemory<byte> Payload
    {
        get;
    }
}
