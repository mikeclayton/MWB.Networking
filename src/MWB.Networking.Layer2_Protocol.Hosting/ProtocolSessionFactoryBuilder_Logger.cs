using Microsoft.Extensions.Logging;

namespace MWB.Networking.Layer2_Protocol.Hosting;

public sealed partial class ProtocolSessionFactoryBuilder
{
    private ILogger? _logger;

    /// <summary>
    /// Sets the logger used by the protocol session.
    /// </summary>
    public ProtocolSessionFactoryBuilder UseLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        return this;
    }
}
