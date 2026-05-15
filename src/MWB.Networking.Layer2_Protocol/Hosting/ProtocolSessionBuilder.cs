using Microsoft.Extensions.Logging;
using MWB.Networking.Layer2_Protocol.Session;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;

namespace MWB.Networking.Layer2_Protocol.Hosting;

public sealed class ProtocolSessionBuilder
{
    // ------------------------------------------------------------------
    // Logger
    // ------------------------------------------------------------------

    private ILogger? _logger;

    public ProtocolSessionBuilder UseLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        return this;
    }

    // ------------------------------------------------------------------
    // Parity
    // ------------------------------------------------------------------
    
    private OddEvenStreamIdParity _parity = OddEvenStreamIdParity.Odd;

    public ProtocolSessionBuilder UseStreamIdParity(
          OddEvenStreamIdParity parity)
    {
        _parity = parity;
        return this;
    }

    // Optional convenience
    public ProtocolSessionBuilder UseOddStreamIds()
        => this.UseStreamIdParity(OddEvenStreamIdParity.Odd);

    public ProtocolSessionBuilder UseEvenStreamIds()
        => this.UseStreamIdParity(OddEvenStreamIdParity.Even);

    // ------------------------------------------------------------------
    // Build
    // ------------------------------------------------------------------

    /// <summary>
    /// Builds a <see cref="ProtocolSession"/> using the configured options and
    /// the provided runtime action sinks.
    /// </summary>
    /// <remarks>
    /// The builder defines protocol configuration only; runtime wiring (i.e. action sinks)
    /// is supplied as parameters to keep configuration and execution concerns separate.
    /// </remarks>
    internal ProtocolSession Build(
        IIncomingActionSink incomingActions,
        IOutgoingActionSink outgoingActions)
    {
        // runtime wiring
        ArgumentNullException.ThrowIfNull(incomingActions);
        ArgumentNullException.ThrowIfNull(outgoingActions);

        // session configuration
        var logger = _logger
            ?? throw new InvalidOperationException("A logger must be configured.");
        var options = new ProtocolSessionOptions(
            new OddEvenStreamIdProvider(_parity));

        var session = new ProtocolSession(logger, incomingActions, outgoingActions, options);
        return session;
    }
}
