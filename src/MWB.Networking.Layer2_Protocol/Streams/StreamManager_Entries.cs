using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;
using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol.Streams;

internal sealed class StreamManager
{

    // ------------------------------------------------------------------
    // Stream handling
    // ------------------------------------------------------------------

    private StreamEntries StreamEntries
    {
        get;
    } = new();

    internal IEnumerable<uint> GetStreamIds()
    {
        return this.StreamEntries.GetStreamEntryIds();
    }

    internal bool IsValidInboundStreamId(uint streamId)
    {
        return this.StreamIdProvider.IsValidInbound(streamId);
    }

    internal bool TryGetStreamEntry(uint streamId, [NotNullWhen(true)] out StreamEntry? result)
    {
        return this.StreamEntries.TryGetStreamEntry(streamId, out result);
    }

    internal bool RemoveStream(uint streamId)
    {
        // no-op if if doesn't exist
        var removed = this.StreamEntries.RemoveStreamEntry(streamId);
        if (!removed)
        {
            // already gone, fine
            // this.Logger.Warn($"{nameof(RemoveStream)} called for non-existent stream {streamId}");
        }
        return removed;
    }

    internal void TearDownStream(uint streamId)
    {
        if (!this.StreamEntries.TryGetStreamEntry(streamId, out var entry))
        {
            // already gone, fine
            // this.Logger.Warn($"{nameof(TearDownStream)} called for non-existent stream {streamId}");
            return;
        }
        entry.Context.Close();
        this.RemoveStream(streamId);
    }
}
