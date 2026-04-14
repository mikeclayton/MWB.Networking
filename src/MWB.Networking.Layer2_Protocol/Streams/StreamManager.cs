using MWB.Networking.Layer2_Protocol.Session;
using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol.Streams;

public sealed partial class StreamManager
{
    internal StreamManager(ProtocolSession session, OddEvenStreamIdProvider streamIdProvider)
    {
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.StreamIdProvider = streamIdProvider ?? throw new ArgumentNullException(nameof(streamIdProvider));
    }

    private ProtocolSession Session
    {
        get;
    }

    private OddEvenStreamIdProvider StreamIdProvider
    {
        get;
    }

    // ------------------------------------------------------------------
    // Stream handling
    // ------------------------------------------------------------------

    private Dictionary<uint, StreamEntry> StreamEntries
    {
        get;
    } = [];

    internal IEnumerable<uint> GetStreamIds()
    {
        return this.StreamEntries.Keys;
    }

    internal bool TryGetStreamEntry(uint key, [NotNullWhen(true)] out StreamEntry? result)
    {
        return this.StreamEntries.TryGetValue(key, out result);
    }

    private void RemoveStream(uint streamId)
    {
        // no-op if if doesn't exist
        if (!this.StreamEntries.Remove(streamId))
        {
            // already gone, fine
            // this.Logger.Warn($"{nameof(RemoveStream)} called for non-existent stream {streamId}");
        }
    }

    internal void TearDownStream(uint streamId)
    {
        if (!this.StreamEntries.TryGetValue(streamId, out var entry))
        {
            // already gone, fine
            // this.Logger.Warn($"{nameof(TearDownStream)} called for non-existent stream {streamId}");
            return;
        }

        entry.Context.Close();
        this.RemoveStream(streamId);
    }

    internal void TearDownRequestStreams(uint requestId)
    {
        // Iterate over a snapshot to avoid modifying during enumeration
        var snapshot = this.StreamEntries.ToArray();

        foreach (var (streamId, entry) in snapshot)
        {
            if (entry.Context.OwningRequest?.RequestId == requestId)
            {
                entry.Context.Close();
                this.TearDownStream(streamId);
            }
        }
    }

}
