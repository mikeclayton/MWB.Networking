using Microsoft.Extensions.Logging;
using MWB.Networking.Logging.Formatters;
using MWB.Networking.Logging.Scopes;
using System.Text;

namespace MWB.Networking.Logging.Debug;

public sealed class DebugLogger : ILogger
{
    private readonly string _category;
    private readonly IExternalScopeProvider? _scopeProvider;

    public DebugLogger(string category, IExternalScopeProvider? scopeProvider)
    {
        _category = category;
        _scopeProvider = scopeProvider;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => NullScope.Instance;

    public bool IsEnabled(LogLevel level) => true;

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
            if(!string.IsNullOrEmpty(prefix))
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

        System.Diagnostics.Debug.WriteLine(sb.ToString());
    }
}
