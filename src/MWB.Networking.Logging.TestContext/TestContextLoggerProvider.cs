using Microsoft.Extensions.Logging;

using VsTestContext = Microsoft.VisualStudio.TestTools.UnitTesting.TestContext;

namespace MWB.Networking.Logging.TestContext;

public sealed class TestContextLoggerProvider :
    ILoggerProvider,
    ISupportExternalScope
{
    private readonly VsTestContext _testContext;
    private IExternalScopeProvider? _scopeProvider;

    public TestContextLoggerProvider(VsTestContext testContext)
    {
        _testContext = testContext;
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public ILogger CreateLogger(string categoryName)
        => new TestContextLogger(_testContext, categoryName, _scopeProvider);

    public void Dispose()
    {
    }
}
