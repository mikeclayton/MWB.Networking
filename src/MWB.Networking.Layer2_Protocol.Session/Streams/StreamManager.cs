using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Session.Streams.Infrastructure;
using MWB.Networking.Layer2_Protocol.Session.Streams.Lifecycle;
using MWB.Networking.Logging;
using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol.Session.Streams;

internal sealed class StreamManager
{
    internal StreamManager(ILogger logger, ProtocolSession session, OddEvenStreamIdProvider streamIdProvider)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(streamIdProvider);
        this.Logger = logger;
        this.StreamIdProvider = streamIdProvider;
        this.Inbound = new StreamManagerInbound(session, this, this.StreamEntries);
        this.Outbound = new StreamManagerOutbound(session, this, this.StreamEntries, streamIdProvider);
    }

    private ILogger Logger
    {
        get;
    }

    private OddEvenStreamIdProvider StreamIdProvider
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

    internal void TearDownRequestStreams(uint requestId)
    {
        // Iterate over a snapshot to avoid modifying during enumeration
        var snapshot = this.StreamEntries.GetStreamEntries();
        foreach (var entry in snapshot)
        {
            if (entry.Context.OwningRequest?.RequestId == requestId)
            {
                this.TearDownStream(entry.StreamId);
            }
        }
    }
}
