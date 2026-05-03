using System.Collections.Concurrent;

namespace MWB.Networking.Layer0_Transport.Memory.Buffer;

/// <summary>
/// Single-writer, single-reader buffered byte channel.
/// Owns exactly one reader and one writer.
/// </summary>
internal sealed class SegmentedBuffer : IDisposable
{
    private readonly ConcurrentQueue<byte[]> _segments = new();
    private readonly SemaphoreSlim _dataAvailable = new(0);

    private volatile bool _completed;
    private volatile bool _disposed;

    public SegmentedBuffer()
    {
        this.Writer = new SegmentedBufferWriter(this);
        this.Reader = new SegmentedBufferReader(this);
    }

    public SegmentedBufferWriter Writer
    {
        get;
    }

    public SegmentedBufferReader Reader
    {
        get;
    }

    internal void Enqueue(byte[] segment)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SegmentedBuffer));
        }

        if (_completed)
        {
            throw new InvalidOperationException(
                "Cannot enqueue after buffer completion.");
        }

        _segments.Enqueue(segment);
        _dataAvailable.Release();
    }

    internal void Complete()
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        _dataAvailable.Release();
    }


    /// <summary>
    /// Dequeues the next segment, waits for data, or returns null on EOF.
    /// </summary>
    internal async ValueTask<byte[]?> DequeueAsync(CancellationToken ct)
    {
        while (true)
        {
            // return immediately if an item is available
            if (_segments.TryDequeue(out var segment))
            {
                return segment;
            }

            // return null if we've finsihed writing
            if (_completed)
            {
                return null;
            }

            // otherwise wait for the next segment to arrive
            await _dataAvailable
                .WaitAsync(ct)
                .ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _completed = true;

        // Unblock any waiting readers
        _dataAvailable.Release();
        _dataAvailable.Dispose();
    }
}
