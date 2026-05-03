namespace MWB.Networking.Layer1_Framing.Codec.Buffer;

/// <summary>
/// Read-only, forward-only view over a <see cref="PipelineBuffer"/>.
///
/// This reader is non-blocking and does not own execution or waiting semantics.
/// It exposes only observable availability and explicit consumption.
/// </summary>
public sealed class CodecBufferReader : ICodecBufferReader
{
    private readonly CodecBuffer _inputBuffer;

    private long _position = 0;
    private ReadOnlyMemory<byte>? _current;
    private int _offset;

    /// <summary>
    /// Creates a reader bound to the specified buffer.
    /// </summary>
    internal CodecBufferReader(CodecBuffer inputBuffer)
    {
        _inputBuffer = inputBuffer ?? throw new ArgumentNullException(nameof(inputBuffer));
    }

    public long Position
        => _position;

    public long Length
        => _inputBuffer.Length + (_current?.Length ?? 0) - _offset;

    /// <summary>
    /// Returns a readable view of the current front segment (if any) in the codec buffer.
    /// Returns <c>false</c> if no segment is currently available.
    /// </summary>
    /// <remarks>
    /// The returned memory represents the unread portion of the current buffer segment.
    /// Repeated calls to <see cref="TryRead"/> will return the same result until
    /// <see cref="Advance"/> is called.
    ///
    /// A <c>false</c> return does not indicate end-of-stream; callers must consult
    /// <see cref="IsCompleted"/> to determine whether no further data will arrive.
    /// </remarks>
    public bool TryRead(out ReadOnlyMemory<byte> memory)
    {
        // No current segment, or current segment fully consumed
        if (_current is null || _offset >= _current.Value.Length)
        {
            if (!_inputBuffer.TryDequeue(out var next))
            {
                memory = default;
                return false;
            }

            _current = next;
            _offset = 0;
        }

        var current = _current
            ?? throw new InvalidOperationException("Invariant violated: _current must be non-null here.");

        memory = current[_offset..];
        return true;
    }

    /// <summary>
    /// Advances the read cursor after consuming bytes from the last read memory.
    /// </summary>
    /// <param name="count">The number of bytes consumed.</param>
    public void Advance(int count)
    {
        if (_current is null)
        {
            throw new InvalidOperationException("No active segment to advance.");
        }

        if ((uint)count > (uint)(_current.Value.Length - _offset))
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        _position += count;
        _offset += count;

        if (_offset == _current.Value.Length)
        {
            _current = null;
            _offset = 0;
        }
    }

    /// <summary>
    /// Gets whether the buffer has completed and no unread segments remain.
    /// </summary>
    public bool IsCompleted
        => _inputBuffer.IsReadCompleted;
}
