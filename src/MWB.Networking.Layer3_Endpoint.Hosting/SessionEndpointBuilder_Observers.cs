using MWB.Networking.Layer2_Protocol.Events.Api;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer3_Endpoint.Hosting;

public sealed partial class SessionEndpointBuilder
{
    // ------------------------------------------------------------------
    // Events
    // ------------------------------------------------------------------

    private readonly List<Action<IncomingEvent, ReadOnlyMemory<byte>>> _eventReceived = [];

    /// <summary>
    /// Invoked when a protocol event is received.
    /// Configuration-time only; wired during session build.
    /// </summary>
    public SessionEndpointBuilder OnEventReceived(
        Action<IncomingEvent, ReadOnlyMemory<byte>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _eventReceived.Add(handler);
        return this;
    }

    // ------------------------------------------------------------------
    // Requests
    // ------------------------------------------------------------------

    private readonly List<Action<IncomingRequest, ReadOnlyMemory<byte>>> _requestReceived = [];

    /// <summary>
    /// Invoked when a response is received.
    /// </summary>
    public SessionEndpointBuilder OnRequestReceived(
        Action<IncomingRequest, ReadOnlyMemory<byte>>? handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _requestReceived.Add(handler);
        return this;
    }

    // ------------------------------------------------------------------
    // Streams
    // ------------------------------------------------------------------

    private readonly List<Action<IncomingStream, StreamMetadata>> _streamOpened = [];
    private readonly List<Action<IncomingStream, ReadOnlyMemory<byte>>> _streamDataReceived = [];
    private readonly List<Action<IncomingStream, StreamMetadata>> _streamClosed = [];

    /// <summary>
    /// Invoked when a stream is opened.
    /// </summary>
    public SessionEndpointBuilder OnStreamOpened(
        Action<IncomingStream, StreamMetadata>? handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _streamOpened.Add(handler);
        return this;
    }

    /// <summary>
    /// Invoked when a data is received for a stream.
    /// </summary>
    public SessionEndpointBuilder OnStreamData(
        Action<IncomingStream, ReadOnlyMemory<byte>>? handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _streamDataReceived.Add(handler);
        return this;
    }

    /// <summary>
    /// Invoked when a stream is closed.
    /// </summary>
    public SessionEndpointBuilder OnStreamClosed(
        Action<IncomingStream, StreamMetadata>? handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _streamClosed.Add(handler);
        return this;
    }

    // ------------------------------------------------------------------
    // Build
    // ------------------------------------------------------------------

    internal SessionEndpointObservers BuildObservers()
        => new(
            _eventReceived.ToList(),
            _requestReceived.ToList(),
            _streamOpened.ToList(),
            _streamDataReceived.ToList(),
            _streamClosed.ToList());
}
