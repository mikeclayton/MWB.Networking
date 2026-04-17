using Microsoft.Extensions.Logging;

namespace MWB.Networking.Layer3_Runtime.UnitTests.Helpers;

internal sealed class TestContextLoggerProvider : ILoggerProvider
{
    private readonly TestContext _testContext;

    public TestContextLoggerProvider(TestContext testContext)
    {
        _testContext = testContext;
    }

    public ILogger CreateLogger(string categoryName)
        => new TestContextLogger(_testContext, categoryName);

    public void Dispose() { }
}
