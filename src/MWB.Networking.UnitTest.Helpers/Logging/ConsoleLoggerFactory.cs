using Microsoft.Extensions.Logging;
using MWB.Networking.Logging.Formatters;

namespace MWB.Networking.UnitTest.Helpers.Logging;

public static class ConsoleLoggerFactory
{
    public static (ILogger, ILoggerFactory) CreateLogger()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddDebug()
                .AddConsole(options =>
                {
                    options.FormatterName = nameof(MethodConsoleFormatter);
                });
        });

        var logger = loggerFactory.CreateLogger("Network");

        return (logger, loggerFactory);
    }
}
