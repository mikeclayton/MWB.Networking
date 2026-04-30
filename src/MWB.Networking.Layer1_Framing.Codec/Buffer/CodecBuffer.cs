using MWB.Networking.Layer0_Transport.Encoding;

namespace MWB.Networking.Layer1_Framing.Codec.Buffer;

/// <summary>
/// A segmented, zero-copy, single-writer / single-reader byte buffer.
///
/// The buffer is non-blocking and does not own execution or waiting semantics.
/// Callers must explicitly handle availability and completion.
/// </summary>
/// <remarks>
/// CodecBuffer is not thread-safe.
/// All access is externally sequenced by the pipeline.
/// Concurrent Enqueue/Dequeue is undefined behavior.
///
/// Length represents the current buffered byte count.
/// It may change until IsWriteCompleted is true.
/// </remarks>
public sealed class CodecBuffer : IDisposable
{
    /// <summary>
    /// The queue of written-but-not-read-yet byte segments.
    ///
    /// This queue is not thread-safe. Callers must ensure that enqueue and dequeue
    /// operations are externally coordinated according to the buffer’s
    /// single-writer, single-reader usage model.
    /// </summary>
    private readonly Queue<ReadOnlyMemory<byte>> _segments = new();

    private bool _writeCompleted;
    private bool _disposed;
    private long _length = 0;

    public CodecBuffer()
    {
        this.Writer = new CodecBufferWriter(this);
        this.Reader = new CodecBufferReader(this);
    }

    /// <summary>
    /// Write-only, append-only view over this buffer.
    /// </summary>
    public CodecBufferWriter Writer
    {
        get;
    }

    /// <summary>
    /// Read-only, forward-only view over this buffer.
    /// </summary>
    public CodecBufferReader Reader
    {
        get;
    }

    public long Length
        => _length;

    /// <summary>
    /// Attempts to dequeue the next completed segment.
    /// Returns false if no data is currently available.
    /// </summary>
    /// <remarks>
    /// A false return does not indicate end-of-stream -
    /// it just means there's no data currently queued,
    /// and more *may* arrive later.
    /// </remarks>
    internal bool TryDequeue(out ReadOnlyMemory<byte> segment)
    {
        if (_segments.Count > 0)
        {
            segment = _segments.Dequeue();
            _length -= segment.Length;
            return true;
        }

        segment = default;
        return false;
    }

    /// <summary>
    /// Enqueues a completed segment for reading.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The buffer has been disposed.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The buffer has been marked complete.
    /// </exception>
    internal void Enqueue(ReadOnlyMemory<byte> segment)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_writeCompleted)
        {
            throw new InvalidOperationException("Cannot enqueue after write completion.");
        }

        _segments.Enqueue(segment);
        _length += segment.Length;
    }

    /// <summary>
    /// Marks the buffer as complete; no further segments may be enqueued.
    /// </summary>
    public void WriteComplete()
    {
        _writeCompleted = true;
    }

    public bool IsWriteCompleted
        => _writeCompleted;

    private bool IsBufferEmpty
        => _segments.Count == 0;

    public bool IsReadCompleted
        => this.IsWriteCompleted && this.IsBufferEmpty;

    public ByteSegments ToByteSegments()
    {
        if (!_writeCompleted)
        {
            throw new InvalidOperationException(
                "Cannot materialize ByteSegments before completion.");
        }

        return _segments.Count == 0
            ? new ByteSegments()
            : new ByteSegments(_segments.ToArray());
    }

    public void Dispose()
    {
        _disposed = true;
        _writeCompleted = true;
        _segments.Clear();
        _length = 0;
    }
}
