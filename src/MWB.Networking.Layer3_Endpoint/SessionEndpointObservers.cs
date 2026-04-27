using MWB.Networking.Layer2_Protocol.Events.Api;
using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Layer3_Endpoint;

public sealed class SessionEndpointObservers
{
    private readonly IReadOnlyList<Action<IncomingEvent, ReadOnlyMemory<byte>>> _eventReceived;
    private readonly IReadOnlyList<Action<IncomingRequest, ReadOnlyMemory<byte>>> _requestReceived;
    private readonly IReadOnlyList<Action<IncomingStream, StreamMetadata>> _streamOpened;
    private readonly IReadOnlyList<Action<IncomingStream, ReadOnlyMemory<byte>>> _streamDataReceived;
    private readonly IReadOnlyList<Action<IncomingStream, StreamMetadata>> _streamClosed;

    public SessionEndpointObservers(
        IEnumerable<Action<IncomingEvent, ReadOnlyMemory<byte>>> eventReceived,
        IEnumerable<Action<IncomingRequest, ReadOnlyMemory<byte>>> requestReceived,
        IEnumerable<Action<IncomingStream, StreamMetadata>> streamOpened,
        IEnumerable<Action<IncomingStream, ReadOnlyMemory<byte>>> streamDataReceived,
        IEnumerable<Action<IncomingStream, StreamMetadata>> streamClosed)
    {
        _eventReceived = eventReceived.ToArray();
        _requestReceived = requestReceived.ToArray();
        _streamOpened = streamOpened.ToArray();
        _streamDataReceived = streamDataReceived.ToArray();
        _streamClosed = streamClosed.ToArray();
    }

    internal void RegisterObservers(ProtocolSessionHandle session)
    {
        var observer = session.Observer;

        foreach (var handler in _eventReceived)
        {
            observer.EventReceived += handler;
        }

        foreach (var handler in _requestReceived)
        {
            observer.RequestReceived += handler;
        }

        foreach (var handler in _streamOpened)
        {
            observer.StreamOpened += handler;
        }

        foreach (var handler in _streamDataReceived)
        {
            observer.StreamDataReceived += handler;
        }

        foreach (var handler in _streamClosed)
        {
            observer.StreamClosed += handler;
        }
    }

    internal void UnregisterObservers(ProtocolSessionHandle session)
    {
        var observer = session.Observer;

        foreach (var handler in _eventReceived)
        {
            observer.EventReceived -= handler;
        }

        foreach (var handler in _requestReceived)
        {
            observer.RequestReceived -= handler;
        }

        foreach (var handler in _streamOpened)
        {
            observer.StreamOpened -= handler;
        }

        foreach (var handler in _streamDataReceived)
        {
            observer.StreamDataReceived -= handler;
        }

        foreach (var handler in _streamClosed)
        {
            observer.StreamClosed -= handler;
        }
    }
}
