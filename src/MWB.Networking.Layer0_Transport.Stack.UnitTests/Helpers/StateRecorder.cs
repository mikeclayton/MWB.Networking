using MWB.Networking.Layer0_Transport.Stack.Core.Lifecycle;

namespace MWB.Networking.Layer0_Transport.Stack.UnitTests.Helpers;

/// <summary>
/// Captures every ConnectionStateChanged event raised by a
/// <see cref="TransportStack"/> in the order they are received.
/// </summary>
internal sealed class StateRecorder : IDisposable
{
    private readonly TransportStack _stack;
    private readonly List<TransportConnectionState> _states = new();
    private readonly object _sync = new();

    public StateRecorder(TransportStack stack)
    {
        _stack = stack;
        _stack.ConnectionStateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? _, TransportConnectionState s)
    {
        lock (_sync)
        {
            _states.Add(s);
        }
    }

    public IReadOnlyList<TransportConnectionState> States
    {
        get { lock (_sync) { return _states.ToArray(); } }
    }

    public void Dispose()
        => _stack.ConnectionStateChanged -= OnStateChanged;
}
