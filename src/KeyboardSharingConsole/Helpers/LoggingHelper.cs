using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using MWB.Networking.Logging.Formatters;

namespace KeyboardSharingConsole.Helpers;

internal static class LoggingHelper
{
    public static ILogger CreateLogger()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddConsoleFormatter<MethodConsoleFormatter, ConsoleFormatterOptions>()
                .AddConsole(options =>
                {
                    options.FormatterName = nameof(MethodConsoleFormatter);
                });
        });

        var logger = loggerFactory.CreateLogger("Network");

        return logger;
    }
}
