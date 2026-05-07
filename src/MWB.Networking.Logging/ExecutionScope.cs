namespace MWB.Networking.Logging;

public sealed class ExecutionScope : IDisposable
{
    private readonly Action _onEndScope;
    private readonly IDisposable? _innerScope;
    private volatile bool _disposed;

    private ExecutionScope(IDisposable? innerScope, Action onEndScope)
    {
        _innerScope = innerScope;
        _onEndScope = onEndScope;
    }

    /// <summary>
    /// Starts a new execution scope by invoking <paramref name="onStartScope"/>
    /// immediately and executing <paramref name="onEndScope"/> when the scope ends.
    /// </summary>
    public static ExecutionScope StartScope(
        Func<IDisposable?> onStartScope,
        Action onEndScope)
    {
        ArgumentNullException.ThrowIfNull(onStartScope);
        ArgumentNullException.ThrowIfNull(onEndScope);

        var innerScope = onStartScope();
        return new ExecutionScope(innerScope, onEndScope);
    }

    /// <summary>
    /// Ends the scope explicitly.
    /// Equivalent to <see cref="Dispose"/>.
    /// </summary>
    public void EndScope()
    {
        this.Dispose();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true))
        {
            // was already disposed
            return;
        }

        _onEndScope();
        _innerScope?.Dispose();
    }
}
