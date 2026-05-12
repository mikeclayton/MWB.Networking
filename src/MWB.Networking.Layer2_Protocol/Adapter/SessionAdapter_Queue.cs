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
        var success = _queue.Writer.TryWrite(action);
        ObjectDisposedException.ThrowIf(!success, this);
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