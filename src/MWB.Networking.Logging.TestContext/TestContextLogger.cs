using Microsoft.Extensions.Logging;
using MWB.Networking.Logging.Formatters;
using MWB.Networking.Logging.Scopes;
using System.Text;
using VsTestContext = Microsoft.VisualStudio.TestTools.UnitTesting.TestContext;

namespace MWB.Networking.Logging.TestContext;

internal sealed class TestContextLogger : ILogger
{
    private readonly VsTestContext _testContext;
    private readonly string _category;
    private readonly IExternalScopeProvider? _scopeProvider;

    public TestContextLogger(VsTestContext testContext, string category, IExternalScopeProvider? scopeProvider)
    {
        _testContext = testContext;
        _category = category;
        _scopeProvider = scopeProvider;
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
        if (!this.IsEnabled(logLevel))
        {
            return;
        }

        var sb = new StringBuilder();

        // append the prefix
        var prefix = MethodPrefixFormatter.FormatFromScope(logLevel, _scopeProvider);
        sb.Append(prefix);

        // append the message
        var message = formatter(state, exception);
        if (!string.IsNullOrEmpty(message))
        {
            if (!string.IsNullOrEmpty(prefix))
            {
                sb.Append(' ');
            }
            sb.Append(message);
        }

        // append the exception (if there is one)
        if (exception is not null)
        {
            sb.AppendLine();
            sb.Append(exception);
        }

        _testContext.WriteLine(sb.ToString());
    }
}
