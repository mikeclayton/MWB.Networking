using Microsoft.Extensions.Logging;

namespace MWB.Networking.Logging.Formatters;

public static class MethodPrefixFormatter
{
    public static string FormatFromScope(
        LogLevel logLevel, 
        IExternalScopeProvider? scopeProvider
    )
    {
        var className = default(string);
        var displayName = default(string);
        var longId = default(string);
        var shortId = default(string);
        var methodName = default(string);

        scopeProvider?.ForEachScope(
            (scope, _) =>
            {
                if (scope is IDictionary<string, string> dictionary)
                {
                    _ = dictionary.TryGetValue("ClassName", out className);
                    _ = dictionary.TryGetValue("DisplayName", out displayName);
                    _ = dictionary.TryGetValue("LongId", out longId);
                    _ = dictionary.TryGetValue("ShortId", out shortId);
                    _ = dictionary.TryGetValue("MethodName", out methodName);
                }
            },
            state: (object?)null
        );

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss.fff");
        var levelName = logLevel switch
        {
            LogLevel.Information => "INFO",
            LogLevel.Debug => "DBUG",
            _ => "????"
        };

        displayName ??= shortId;

        var prefix = $"[{timestamp}] {levelName} [{className}:{displayName}:{methodName}]";

        return prefix;
    }
}
