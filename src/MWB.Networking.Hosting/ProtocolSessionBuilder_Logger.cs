using Microsoft.Extensions.Logging;

namespace MWB.Networking.Hosting;

/// <summary>
/// Application-facing builder for protocol sessions.
///
/// This type owns network pipeline wiring and delegates
/// final session creation to the protocol layer.
/// </summary>
public sealed partial class ProtocolSessionBuilder
{
    private ILogger? _logger;

    /// <summary>
    /// Sets the logger used by the protocol runtime.
    /// </summary>
    public ProtocolSessionBuilder WithLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this.EnsureNotBuilt();

        _logger = logger;
        return this;
    }
}
