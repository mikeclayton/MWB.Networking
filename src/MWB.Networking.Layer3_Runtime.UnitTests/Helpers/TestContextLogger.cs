using Microsoft.Extensions.Logging;

namespace MWB.Networking.Layer3_Runtime.UnitTests.Helpers;

internal sealed class TestContextLogger : ILogger
{
    private readonly TestContext _testContext;
    private readonly string _category;

    public TestContextLogger(TestContext testContext, string category)
    {
        _testContext = testContext;
        _category = category;
    }

    public IDisposable? BeginScope<TState>(TState state) => NullScope.Instance;

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

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
