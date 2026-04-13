using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace MWB.Networking.Logging;

public sealed class MethodConsoleFormatter : ConsoleFormatter
{
    public MethodConsoleFormatter()
        : base(nameof(MethodConsoleFormatter))
    {
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var className = default(string);
        var displayName = default(string);
        var longId = default(string);
        var shortId = default(string);
        var methodName = default(string);

        scopeProvider?.ForEachScope((scope, _) =>
        {
            if (scope is IDictionary<string, string> dictionary)
            {
                _ = dictionary.TryGetValue("ClassName", out className);
                _ = dictionary.TryGetValue("DisplayName", out displayName);
                _ = dictionary.TryGetValue("LongId", out longId);
                _ = dictionary.TryGetValue("ShortId", out shortId);
                _ = dictionary.TryGetValue("MethodName", out methodName);
            }
        }, state: (object?)null);

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss.fff");
        var logLevel = logEntry.LogLevel switch
        {
            LogLevel.Information => "INFO",
            LogLevel.Debug => "DBUG",
            _ =>
                throw new InvalidOperationException()
        };
        displayName = displayName ?? shortId;

        textWriter.Write($"[{timestamp}] {logLevel} [{className}:{displayName}:{methodName}] {logEntry.Formatter(logEntry.State, logEntry.Exception)}");
        textWriter.WriteLine();
    }
}

