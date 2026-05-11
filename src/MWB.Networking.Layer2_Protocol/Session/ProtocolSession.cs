using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Events;
using MWB.Networking.Layer2_Protocol.Hosting;
using MWB.Networking.Layer2_Protocol.Requests;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams;

namespace MWB.Networking.Layer2_Protocol.Session;

/// <summary>
/// The central authority for a protocol connection.
/// 
/// ProtocolSession is the single choke point where:
/// - inbound frames are validated
/// - protocol invariants are enforced
/// - requests and streams are coordinated
/// - application intent is translated into protocol actions
/// </summary>
internal sealed partial class ProtocolSession
{
    internal ProtocolSession(
        ILogger logger,
        IIncomingActionSink incomingActionSink,
        IOutgoingActionSink outgoingActionSink,
        ProtocolSessionOptions options)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.IncomingActionSink = incomingActionSink ?? throw new ArgumentNullException(nameof(incomingActionSink));
        this.OutgoingActionSink = outgoingActionSink ?? throw new ArgumentNullException(nameof(outgoingActionSink));
        this.EventManager = new EventManager(logger, this);
        this.RequestManager = new RequestManager(logger, this);
        this.StreamManager = new StreamManager(logger, this, options.OutboundStreamIdProvider);
    }

    private ILogger Logger
    {
        get;
    }

    internal IIncomingActionSink IncomingActionSink
    {
        get;
    }

    internal IOutgoingActionSink OutgoingActionSink
    {
        get;
    }

    public ProtocolSessionHandle AsHandle()
    {
        return new ProtocolSessionHandle(this);
    }

    internal EventManager EventManager
    {
        get;
    }

    internal RequestManager RequestManager
    {
        get;
    }

    internal StreamManager StreamManager
    {
        get;
    }
}
