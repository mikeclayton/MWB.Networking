namespace MWB.Networking.Layer2_Protocol.Driver;

/// <summary>
/// Fixed-size ring buffer for diagnostics and observability.
///
/// Optimized for fast, allocation-free writes and snapshot-based reads.
/// Oldest entries are overwritten when capacity is exceeded.
///
/// When T is a struct, entries represent value snapshots.
/// When T is a class, entries are references and must be treated as
/// immutable (or observationally safe) after being written.
/// </summary>
public sealed class RingBuffer<T>
{
    private readonly T[] _buffer;
    private readonly int _capacity;

    // Monotonically increasing write counter
    private long _writeIndex;

    // Used only to protect snapshot reads
    private readonly object _readLock = new();

    public RingBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _capacity = capacity;
        _buffer = new T[capacity];
    }

    /// <summary>
    /// Writes an item into the ring buffer, overwriting the oldest entry if full.
    ///
    /// This method is allocation-free and lock-free.
    /// Safe for high-frequency diagnostic capture.
    /// </summary>
    public void Write(in T item)
    {
        var index = Interlocked.Increment(ref _writeIndex) - 1;
        _buffer[index % _capacity] = item;
    }

    /// <summary>
    /// Returns a snapshot of the most recent items in the buffer,
    /// in chronological order (oldest to newest).
    ///
    /// This method allocates and is intended for diagnostics or tooling only.
    /// </summary>
    public T[] Snapshot()
    {
        lock (_readLock)
        {
            var written = Volatile.Read(ref _writeIndex);
            var count = (int)Math.Min(written, _capacity);

            var snapshot = new T[count];

            var start = Math.Max(0, written - _capacity);
            for (var i = 0; i < count; i++)
            {
                snapshot[i] = _buffer[(start + i) % _capacity];
            }

            return snapshot;
        }
    }

    /// <summary>
}