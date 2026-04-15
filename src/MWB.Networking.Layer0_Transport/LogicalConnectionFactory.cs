using Microsoft.Extensions.Logging;

namespace MWB.Networking.Layer0_Transport;

public static class LogicalConnectionFactory
{
    public static LogicalConnectionHandle Create(ILogger logger)
    {
        return new LogicalConnectionHandle(
            new LogicalConnection(logger));
    }
}
