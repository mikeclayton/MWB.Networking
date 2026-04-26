using Microsoft.Extensions.Logging;

namespace MWB.Networking.Layer3_Hosting.Configuration;

/// <summary>
/// Application-facing builder for protocol sessions.
///
/// This type owns network pipeline wiring and delegates
/// final session creation to the protocol layer.
/// </summary>
public sealed partial class SessionHostBuilder
{
    private ILogger? _logger;

    /// <summary>
    /// Sets the logger used by the protocol runtime.
    /// </summary>
    public SessionHostBuilder WithLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        return this;
    }
}
