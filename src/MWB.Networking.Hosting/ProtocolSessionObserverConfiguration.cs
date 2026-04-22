using MWB.Networking.Layer2_Protocol.Requests.Api;
using MWB.Networking.Layer2_Protocol.Streams.Api;

namespace MWB.Networking.Hosting;

public sealed class ProtocolSessionObserverConfiguration
{
    // ------------------------------------------------------------------
    // Events
    // ------------------------------------------------------------------

    /// <summary>
    /// Invoked when a protocol event is received.
    /// Configuration-time only; wired during session build.
    /// </summary>
    public Action<uint, ReadOnlyMemory<byte>>? EventReceived
    {
        get;
        set;
    }

    // ------------------------------------------------------------------
    // Requests
    // ------------------------------------------------------------------

    /// <summary>
    /// Invoked when a response is received.
    /// </summary>
    public Action<IncomingRequest, ReadOnlyMemory<byte>>? RequestReceived
    {
        get;
        set;
    }

    // ------------------------------------------------------------------
    // Streams
    // ------------------------------------------------------------------

    /// <summary>
    /// Invoked when a stream is opened.
    /// </summary>
    public Action<IncomingStream, StreamMetadata>? StreamOpened
    {
        get;
        set;
    }

    /// <summary>
    /// Invoked when a data is received for a stream.
    /// </summary>
    public Action<IncomingStream, ReadOnlyMemory<byte>>? StreamDataReceived
    {
        get;
        set;
    }

    /// <summary>
    /// Invoked when a stream is closed.
    /// </summary>
    public Action<IncomingStream, StreamMetadata>? StreamClosed
    {
        get;
        set;
    }
}
