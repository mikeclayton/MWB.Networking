namespace MWB.Networking.Layer0_Transport.Memory;

/// <summary>
/// Represents the two endpoints of a duplex in-memory transport.
/// Exactly two connections are created, sharing two unidirectional buffers.
/// </summary>
internal sealed class InMemoryConnectionPair
{
    public InMemoryConnectionPair()
    {
        // One buffer for each direction
        var bufferAtoB = new SegmentedMemoryBuffer();
        var bufferBtoA = new SegmentedMemoryBuffer();

        this.ConnectionAtoB = new InMemoryNetworkConnection(
            reader: bufferBtoA.Reader,
            writer: bufferAtoB.Writer);

        this.ConnectionBtoA = new InMemoryNetworkConnection(
            reader: bufferAtoB.Reader,
            writer: bufferBtoA.Writer);
    }

    public INetworkConnection ConnectionAtoB
    {
        get;
    }

    public INetworkConnection ConnectionBtoA
    {
        get;
    }
}
