using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Session.Events;
using MWB.Networking.Layer2_Protocol.Session.Requests;
using MWB.Networking.Layer2_Protocol.Session.Streams;
using MWB.Networking.Logging;

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
internal sealed partial class ProtocolSession : IProtocolSessionFrameIO
{
    public ProtocolSession(
        ILogger logger,
        ProtocolSessionConfig config)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.EventManager = new EventManager(logger, this);
        this.RequestManager = new RequestManager(logger, this);
        this.StreamManager = new StreamManager(logger, this, config.OutboundStreamIdProvider);
    }

    private ILogger Logger
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
