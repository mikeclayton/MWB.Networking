using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Requests.Lifecycle;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;
using MWB.Networking.Logging;
using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol.Streams;

public sealed partial class StreamManager : IHasLogger
{
    internal StreamManager(ILogger logger, ProtocolSession session, OddEvenStreamIdProvider streamIdProvider)
    {
        this.Logger = logger ?? throw new ArgumentOutOfRangeException(nameof(logger));
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.StreamIdProvider = streamIdProvider ?? throw new ArgumentNullException(nameof(streamIdProvider));
    }

    public ILogger Logger
    {
        get;
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
    // Cached request contexts
    // ------------------------------------------------------------------

    private readonly Dictionary<uint, StreamEntry> _streamEntries = [];

    private void AddStreamEntry(StreamEntry entry)
    {
        _streamEntries.Add(entry.StreamId, entry);
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
    // ------------------------------------------------------------------
    // Stream handling
    // ------------------------------------------------------------------

    internal IEnumerable<uint> GetStreamIds()
    {
        return this.GetStreamEntryIds();
    }

    private void RemoveStream(uint streamId)
    {
        // no-op if if doesn't exist
        if (!this.RemoveStreamEntry(streamId))
        {
            // already gone, fine
            // this.Logger.Warn($"{nameof(RemoveStream)} called for non-existent stream {streamId}");
        }
    }

    internal void TearDownStream(uint streamId)
    {
        if (!this.TryGetStreamEntry(streamId, out var entry))
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
        var snapshot = this.GetStreamEntries();

        foreach (var entry in snapshot)
        {
            if (entry.Context.OwningRequest?.RequestId == requestId)
            {
                entry.Context.Close();
                this.TearDownStream(entry.StreamId);
            }
        }
    }

}
