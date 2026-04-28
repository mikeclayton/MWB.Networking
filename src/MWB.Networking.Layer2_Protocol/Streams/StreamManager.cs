using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Lifecycle.Infrastructure;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Streams.Lifecycle;
using MWB.Networking.Logging;
using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol.Streams;

public sealed class StreamManager : IHasLogger
{
    internal StreamManager(ILogger logger, ProtocolSession session, OddEvenStreamIdProvider streamIdProvider)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(streamIdProvider);
        this.Logger = logger;
        this.Inbound = new StreamManagerInbound(session, this, this.StreamEntries);
        this.Outbound = new StreamManagerOutbound(session, this, this.StreamEntries, streamIdProvider);
    }

    public ILogger Logger
    {
        get;
    }

    private StreamEntries StreamEntries
    {
        get;
    } = new();

    internal StreamManagerInbound Inbound
    {
        get;
    }

    internal StreamManagerOutbound Outbound
    {
        get;
    }

    // ------------------------------------------------------------------
    // Stream handling
    // ------------------------------------------------------------------

    internal IEnumerable<uint> GetStreamIds()
    {
        return this.StreamEntries.GetStreamEntryIds();
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

    internal void TearDownRequestStreams(uint requestId)
    {
        // Iterate over a snapshot to avoid modifying during enumeration
        var snapshot = this.StreamEntries.GetStreamEntries();
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
