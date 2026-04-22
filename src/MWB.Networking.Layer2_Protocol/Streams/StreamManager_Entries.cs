using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;
using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol.Streams;

public sealed partial class StreamManager
{
    // ------------------------------------------------------------------
    // Cached stream entries
    // ------------------------------------------------------------------

    private readonly Dictionary<uint, StreamEntry> _streamEntries = [];

    private void AddStreamEntry(StreamEntry entry)
    {
        if (!_streamEntries.TryAdd(entry.StreamId, entry))
        {
            throw new ProtocolException(
                ProtocolErrorKind.DuplicateStreamId,
                $"A stream with ID {entry.StreamId} already exists in this session");
        }
    }

    private bool StreamEntryExists(uint streamId)
    {
        return _streamEntries.ContainsKey(streamId);
    }

    internal bool TryGetStreamEntry(uint streamId, [NotNullWhen(true)] out StreamEntry? result)
    {
        return _streamEntries.TryGetValue(streamId, out result);
    }

    private List<StreamEntry> GetStreamEntries()
    {
        return _streamEntries.Values.ToList();
    }

    private List<uint> GetStreamEntryIds()
    {
        return _streamEntries.Keys.ToList();
    }

    private bool RemoveStreamEntry(uint streamId)
    {
        return _streamEntries.Remove(streamId);
    }
}
