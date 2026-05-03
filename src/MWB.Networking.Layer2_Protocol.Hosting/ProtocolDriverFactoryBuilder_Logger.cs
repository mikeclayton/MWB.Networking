using Microsoft.Extensions.Logging;

namespace MWB.Networking.Layer2_Protocol.Hosting;

public sealed partial class ProtocolDriverFactoryBuilder
{
    private ILogger? _logger;

    /// <summary>
    /// Sets the logger used by the protocol logger.
    /// </summary>
    public ProtocolDriverFactoryBuilder UseLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        return this;
    }
}
