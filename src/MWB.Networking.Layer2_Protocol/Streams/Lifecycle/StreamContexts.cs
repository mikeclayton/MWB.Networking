using MWB.Networking.Layer2_Protocol.Internal;
using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

internal sealed class StreamContexts
{
    // ------------------------------------------------------------------
    // Cached stream entries
    // ------------------------------------------------------------------

    private readonly Dictionary<uint, StreamContext> _streamContexts = [];

    internal void Add(StreamContext context)
    {
        if (!_streamContexts.TryAdd(context.StreamId, context))
        {
            throw ProtocolException.DuplicateStreamId(
                $"A stream with ID {context.StreamId} already exists in this session");
        }
    }

    internal bool Exists(uint streamId)
    {
        return _streamContexts.ContainsKey(streamId);
    }

    internal bool TryGet(uint streamId, [NotNullWhen(true)] out StreamContext? result)
    {
        return _streamContexts.TryGetValue(streamId, out result);
    }

    internal IReadOnlyCollection<uint> GetStreamIds()
    {
        return _streamContexts.Keys;
    }

    internal bool Remove(uint streamId)
    {
        return _streamContexts.Remove(streamId);
    }

    // ------------------------------------------------------------------
    // Utility methods
    // ------------------------------------------------------------------

    internal void ThrowIfExists(uint streamId)
    {
        if (_streamContexts.ContainsKey(streamId))
        {
            throw ProtocolException.InvalidSequence(
                $"Duplicate StreamId {streamId}");
        }
    }

    internal StreamContext GetOrThrow(uint streamId)
    {
        if (this.TryGet(streamId, out var result))
        {
            return result;
        }
        throw ProtocolException.InvalidSequence(
            $"Unknown StreamId {streamId}");
    }
}
