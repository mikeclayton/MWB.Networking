namespace MWB.Networking.Layer2_Protocol.Streams.Infrastructure;

/// <summary>
/// Inbound and Outbound streams are managed in a single shared identity
/// (i.e. stream id) space. To avoid stream id collisions between peers,
/// the stream id space is partitioned by parity (odd versus even). One
/// party in a connection will generate *odd* stream ids and the
/// other party will generate *even* ids.
///
/// The parity of a StreamManager is determined by outside factors (e.g. who
/// initiated the connection) and assigned to the StreamManager at creation.
/// StreamManager itself just honours its own role does not decide its parity,
/// it just enforces the role it was assigned.
///
/// If a StreamManager receives an Inbound stream with the wrong parity
/// it will raise a protocol error.
/// </summary>
internal sealed class OddEvenStreamIdProvider
{
    internal OddEvenStreamIdProvider(OddEvenStreamIdParity outboundParity)
    {
        this.OutboundParity = outboundParity;
        this.NextStreamId = this.OutboundParity switch
        {
            OddEvenStreamIdParity.Odd => 1,
            OddEvenStreamIdParity.Even => 2,
            _ => throw new ArgumentException("Invalid stream id parity", nameof(outboundParity))
        };
    }

    private OddEvenStreamIdParity OutboundParity
    {
        get;
    }

    private uint NextStreamId
    {
        get;
        set;
    }

    internal uint AllocateOutbound()
    {
        var streamId = this.NextStreamId;
        this.NextStreamId += 2;
        return streamId;
    }

    internal bool IsValidInbound(uint streamId)
    {
        var isOdd = (streamId & 1) == 1;
        return this.OutboundParity == OddEvenStreamIdParity.Even
            ? isOdd
            : !isOdd;
    }
}
