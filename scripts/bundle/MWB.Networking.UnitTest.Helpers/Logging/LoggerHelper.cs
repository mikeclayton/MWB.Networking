using Microsoft.Extensions.Logging;

namespace MWB.Networking.UnitTest.Helpers.Logging;

public static class LoggingHelper
{
    public static ILogger CreateTestContextLogger(TestContext testContext)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
         {
             builder
                 .SetMinimumLevel(LogLevel.Trace)
                 .AddProvider(new TestContextLoggerProvider(testContext));
         });

        var logger = loggerFactory.CreateLogger("ProtocolDriver");

        return logger;
    }
}
