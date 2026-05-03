using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Events;
using MWB.Networking.Layer2_Protocol.Requests;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Logging;
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
public sealed partial class ProtocolSession : IHasLogger
{
    internal ProtocolSession(
        ILogger logger,
        ProtocolSessionConfig config)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.EventManager = new EventManager(logger, this);
        this.RequestManager = new RequestManager(logger, this);
        this.StreamManager = new StreamManager(logger, this, config.OutboundStreamIdProvider);
    }

    public ILogger Logger
    {
        get;
    }

    internal ProtocolSessionHandle AsHandle()
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
