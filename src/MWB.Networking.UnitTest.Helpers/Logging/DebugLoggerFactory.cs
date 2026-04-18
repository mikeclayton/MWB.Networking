using Microsoft.Extensions.Logging;
using MWB.Networking.Logging.Loggers;

namespace MWB.Networking.UnitTest.Helpers.Logging;

public static class DebugLoggerFactory
{
    public static (ILogger, ILoggerFactory?) CreateLogger()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddProvider(new DebugLoggerProvider())
                .AddDebug();
        });

        var logger = loggerFactory.CreateLogger("Network");

        return (logger, loggerFactory);
    }
}
