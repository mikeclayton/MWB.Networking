using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MWB.Networking.Logging.Loggers;

public static class DebugLoggerFactory
{
    public static (ILogger, ILoggerFactory) Create(
        [CallerMemberName] string category = "")
    {
        var factory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new DebugLoggerProvider());
        });

        var logger = factory.CreateLogger(category);

        return (logger, factory);
    }

    public static (ILogger<T>, ILoggerFactory) Create<T>()
    {
        var factory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new DebugLoggerProvider());
        });

        return (factory.CreateLogger<T>(), factory);
    }
}
