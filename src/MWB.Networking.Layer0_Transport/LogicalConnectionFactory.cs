using Microsoft.Extensions.Logging;

namespace MWB.Networking.Layer0_Transport;

/// <summary>
/// Factory for creating a logical network connection and its
/// associated control handle.
/// </summary>
public static class LogicalConnectionFactory
{
    public static LogicalConnectionHandle Create(ILogger logger)
    {
        return new LogicalConnectionHandle(
            new LogicalConnection(logger));
    }
}
