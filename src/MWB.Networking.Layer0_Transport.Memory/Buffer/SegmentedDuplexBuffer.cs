using MWB.Networking.Buffers.Segmented;

namespace MWB.Networking.Layer0_Transport.Memory.Buffer;

/// <summary>
/// Provides two in-memory full-duplex transport endpoints
/// backed by segmented buffers. This type owns only data
/// plumbing and has no lifecycle or status semantics.
/// </summary>
public sealed class SegmentedDuplexBuffer
{
    public SegmentedDuplexBuffer()
    {
        var aToB = new SegmentedBuffer();
        var bToA = new SegmentedBuffer();

        this.ConnectionA = new(
            reader: bToA.Reader,
            writer: aToB.Writer);

        this.ConnectionB = new(
            reader: aToB.Reader,
            writer: bToA.Writer);
    }

    internal InMemoryNetworkConnection ConnectionA
    {
        get;
    }

    internal InMemoryNetworkConnection ConnectionB
    {
        get;
    }

    internal InMemoryNetworkConnection GetConnection(SegmentedDuplexBufferSide endpoint)
    {
        return endpoint switch
        {
            SegmentedDuplexBufferSide.SideA => this.ConnectionA,
            SegmentedDuplexBufferSide.SideB => this.ConnectionB,
            _ => throw new ArgumentOutOfRangeException(nameof(endpoint))
        };
    }
}
