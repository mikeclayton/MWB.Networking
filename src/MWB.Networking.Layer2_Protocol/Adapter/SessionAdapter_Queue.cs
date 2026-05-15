using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace MWB.Networking.Layer2_Protocol.Adapter;

public sealed partial class SessionAdapter
{
    // ------------------------------------------------------------------
    // Scheduling / serialization
    // ------------------------------------------------------------------

    private readonly Channel<Action> _queue;

    private void Enqueue(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_cts.IsCancellationRequested)
        {
            return;
        }
        if (!_queue.Writer.TryWrite(action))
        {
            throw new InvalidOperationException("SessionAdapter queue is not accepting work.");
        }
    }

    private async Task RunAsync()
    {
        try
        {
            await foreach (var action in _queue.Reader.ReadAllAsync(_cts.Token))
            {
                action();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
    }
}