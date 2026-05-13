using MWB.Networking.Layer2_Protocol.Internal;
using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

internal sealed class StreamEntries
{
    // ------------------------------------------------------------------
    // Cached stream entries
    // ------------------------------------------------------------------

    private readonly Dictionary<uint, StreamEntry> _streamEntries = [];

    internal void AddStreamEntry(StreamEntry entry)
    {
        if (!_streamEntries.TryAdd(entry.StreamId, entry))
        {
            throw new ProtocolException(
                ProtocolErrorKind.DuplicateStreamId,
                $"A stream with ID {entry.StreamId} already exists in this session");
        }
    }

    internal bool StreamEntryExists(uint streamId)
    {
        return _streamEntries.ContainsKey(streamId);
    }

    internal bool TryGetStreamEntry(uint streamId, [NotNullWhen(true)] out StreamEntry? result)
    {
        return _streamEntries.TryGetValue(streamId, out result);
    }

    internal List<StreamEntry> GetStreamEntries()
    {
        return _streamEntries.Values.ToList();
    }

    internal List<uint> GetStreamEntryIds()
    {
        return _streamEntries.Keys.ToList();
    }

    internal bool RemoveStreamEntry(uint streamId)
    {
        return _streamEntries.Remove(streamId);
    }
}
