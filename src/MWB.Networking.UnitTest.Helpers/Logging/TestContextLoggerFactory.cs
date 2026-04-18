using Microsoft.Extensions.Logging;

namespace MWB.Networking.UnitTest.Helpers.Logging;

public static class TestContextLoggerFactory
{
    public static (ILogger, ILoggerFactory) CreateLogger(TestContext testContext)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
         {
             builder
                 .SetMinimumLevel(LogLevel.Trace)
                 .AddProvider(new TestContextLoggerProvider(testContext));
         });

        var logger = loggerFactory.CreateLogger("ProtocolDriver");

        return (logger, loggerFactory);
    }
}
