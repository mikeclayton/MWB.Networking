using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using VsTestContext = Microsoft.VisualStudio.TestTools.UnitTesting.TestContext;

namespace MWB.Networking.Logging.TestContext;

public static class TestContextLoggerFactory
{
    public static (ILogger, ILoggerFactory) CreateLogger(
        VsTestContext testContext,
        [CallerMemberName] string category = "")
    {
        var factory = LoggerFactory.Create(builder =>
         {
             builder
                 .SetMinimumLevel(LogLevel.Trace)
                 .AddProvider(new TestContextLoggerProvider(testContext));
         });

        var logger = factory.CreateLogger(category);

        return (logger, factory);
    }
}
