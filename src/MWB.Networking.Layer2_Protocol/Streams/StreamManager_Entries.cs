using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

namespace MWB.Networking.Layer2_Protocol.Streams;

internal sealed partial class StreamManager
{
    // ------------------------------------------------------------------
    // Stream handling
    // ------------------------------------------------------------------

    private StreamContexts StreamContexts
    {
        get;
    } = new();

    internal IReadOnlyCollection<uint> GetStreamIds()
    {
        return this.StreamContexts.GetStreamIds();
    }

    internal bool IsValidInboundStreamId(uint streamId)
    {
        return this.StreamIdProvider.IsValidInbound(streamId);
    }

    internal bool RemoveStream(uint streamId)
    {
        // no-op if if doesn't exist
        var removed = this.StreamContexts.Remove(streamId);
        if (!removed)
        {
            // already gone, fine
            // this.Logger.Warn($"{nameof(RemoveStream)} called for non-existent stream {streamId}");
        }
        return removed;
    }
}
