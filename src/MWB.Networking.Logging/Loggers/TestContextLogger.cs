using Microsoft.Extensions.Logging;
using MWB.Networking.Logging.Scopes;

namespace MWB.Networking.UnitTest.Helpers.Logging;

internal sealed class TestContextLogger : ILogger
{
    private readonly TestContext _testContext;
    private readonly string _category;

    public TestContextLogger(TestContext testContext, string category)
    {
        _testContext = testContext;
        _category = category;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _testContext.WriteLine(
            $"[{logLevel}] {_category}: {formatter(state, exception)}");

        if (exception != null)
        {
            _testContext.WriteLine(exception.ToString());
        }
    }

}
