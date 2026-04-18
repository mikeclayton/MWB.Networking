using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace MWB.Networking.Logging.Formatters;

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
        var prefix = MethodPrefixFormatter.FormatFromScope(logEntry.LogLevel, scopeProvider);

        textWriter.Write($"{prefix} {logEntry.Formatter(logEntry.State, logEntry.Exception)}");
        textWriter.WriteLine();
    }
}

