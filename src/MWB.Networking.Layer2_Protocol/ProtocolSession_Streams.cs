using MWB.Networking.Layer2_Protocol.Internal;
using MWB.Networking.Layer2_Protocol.Streams;

namespace MWB.Networking.Layer2_Protocol;

public sealed partial class ProtocolSession : IProtocolSession
{
    // ------------------------------------------------------------------
    // Stream handling
    // ------------------------------------------------------------------

    public event Action<IncomingStream, StreamMetadata>? StreamOpened;

    public event Action<IncomingStream, ReadOnlyMemory<byte> /* Payload */>? StreamDataReceived;

    public event Action<IncomingStream>? StreamClosed;

    private Dictionary<uint, StreamEntry> StreamEntries
    {
        get;
    } = [];

    private void RaiseStreamOpened(IncomingStream stream, StreamMetadata metadata)
        => this.StreamOpened?.Invoke(stream, metadata);

    private void RaiseStreamDataReceived(IncomingStream stream, ReadOnlyMemory<byte> payload)
        => this.StreamDataReceived?.Invoke(stream, payload);

    private void RaiseStreamClosed(IncomingStream stream)
        => this.StreamClosed?.Invoke(stream);

    private void ProcessStreamFrame(ProtocolFrame frame)
    {
        if (frame.StreamId is null)
        {
            throw ProtocolError(frame, "Stream-related frame missing StreamId");
        }

        var streamId = frame.StreamId.Value;
        _ = this.StreamEntries.TryGetValue(streamId, out var streamEntry);

        if (frame.Kind == ProtocolFrameKind.StreamOpen)
        {
            if (streamEntry is not null)
            {
                throw ProtocolError(frame, "Duplicate StreamId");
            }

            streamEntry = new StreamEntry(
                context: new(streamId),
                stream: new(this, streamId)
            );
            this.StreamEntries.Add(streamId, streamEntry);

            // Semantic notification
            this.RaiseStreamOpened(
                stream: streamEntry.Stream,
                metadata: new(frame.Payload));

            return;
        }

        if (streamEntry is null)
        {
            throw ProtocolError(frame, "Unknown StreamId");
        }

        switch (frame.Kind)
        {
            case ProtocolFrameKind.StreamData:
                streamEntry.Context.EnsureOpen();
                this.RaiseStreamDataReceived(streamEntry.Stream, frame.Payload);
                break;

            case ProtocolFrameKind.StreamClose:
                streamEntry.Context.EnsureOpen();
                this.RaiseStreamClosed(streamEntry.Stream);
                streamEntry.Context.MarkClosed();
                this.RemoveStream(streamId);
                break;

            default:
                throw ProtocolError(frame, "Invalid stream frame kind");
        }
    }

    private void RemoveStream(uint streamId)
    {
        this.StreamEntries.Remove(streamId);
    }
}
