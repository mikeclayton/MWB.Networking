using Microsoft.Extensions.Logging;

namespace MWB.Networking.Logging.Debug;

public sealed class DebugLoggerProvider :
    ILoggerProvider,
    ISupportExternalScope
{
    private IExternalScopeProvider? _scopeProvider;

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new DebugLogger(categoryName, _scopeProvider);
    }

    public void Dispose()
    {
    }
}
