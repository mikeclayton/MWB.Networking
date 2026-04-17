using System.Buffers;

namespace MWB.Networking.Layer1_Framing.Encoding.Helpers;

/// <summary>
/// A utility class - a decoder-owned byte accumulation buffer used to
/// support incremental, stream-oriented decoding of frame data.
///
/// This type provides only mechanical buffering operations (append,
/// inspect, consume). It deliberately contains no framing, encoding,
/// or protocol semantics. All interpretation of buffered bytes
/// remains the responsibility of the decoder that owns this instance.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DecoderBuffer"/> exists to bridge the semantic gap between
/// stream-oriented transports (which deliver arbitrary byte chunks)
/// and frame-oriented decoders (which must reconstruct complete frames).
/// </para>
/// <para>
/// This buffer is intended to be:
/// <list type="bullet">
///   <item><description>Owned by a single decoder instance</description></item>
///   <item><description>Used only on the inbound (decode) path</description></item>
///   <item><description>Cleared or disposed when the decoder is finished</description></item>
/// </list>
/// </para>
/// <para>
/// It must not be treated as a general-purpose byte container, nor should
/// framing logic, prefix parsing, or blocking read semantics be added here.
/// </para>
/// </remarks>
public sealed class DecoderBuffer : IDisposable
{
    private byte[] _buffer;
    private int _count;
    private bool _disposed;

    /// <summary>
    /// Creates a new <see cref="DecoderBuffer"/> with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">
    /// The initial buffer capacity. This value is not a maximum; the buffer
    /// will grow as required to accommodate appended data.
    /// </param>
    public DecoderBuffer(int initialCapacity = 4096)
    {
        if (initialCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));

        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        _count = 0;
    }

    /// <summary>
    /// Gets the number of bytes currently buffered.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Gets a contiguous, read-only view over the currently buffered bytes.
    /// </summary>
    /// <remarks>
    /// The returned span represents all accumulated bytes that have not yet
    /// been consumed. Decoders may inspect this span to determine whether
    /// enough data is available to make decoding progress.
    /// </remarks>
    public ReadOnlySpan<byte> Span => _buffer.AsSpan(0, _count);

    /// <summary>
    /// Appends bytes to the end of the buffer.
    /// </summary>
    /// <param name="data">
    /// The byte data to append.
    /// </param>
    /// <remarks>
    /// This method performs no interpretation of the appended bytes.
    /// It simply ensures capacity and copies the data into the buffer.
    /// </remarks>
    public void Append(ReadOnlySpan<byte> data)
    {
        EnsureCapacity(_count + data.Length);
        data.CopyTo(_buffer.AsSpan(_count));
        _count += data.Length;
    }

    /// <summary>
    /// Consumes (removes) a prefix of the buffered data.
    /// </summary>
    /// <param name="count">
    /// The number of bytes to remove from the start of the buffer.
    /// </param>
    /// <remarks>
    /// After consumption, any remaining bytes are shifted down to the start
    /// of the buffer. This operation encodes no knowledge of framing or
    /// message boundaries; the caller is responsible for determining how
    /// many bytes are safe to consume.
    /// </remarks>
    public void Consume(int count)
    {
        if ((uint)count > (uint)_count)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (count == _count)
        {
            _count = 0;
            return;
        }

        Buffer.BlockCopy(
            _buffer, count,
            _buffer, 0,
            _count - count);

        _count -= count;
    }

    /// <summary>
    /// Removes all buffered bytes without releasing the underlying storage.
    /// </summary>
    /// <remarks>
    /// This is equivalent to consuming the entire buffer and is typically
    /// used when resetting decoder state.
    /// </remarks>
    public void Clear()
    {
        _count = 0;
    }

    private void EnsureCapacity(int required)
    {
        if (_buffer.Length >= required)
            return;

        int newCapacity = Math.Max(required, _buffer.Length * 2);
        var newBuffer = ArrayPool<byte>.Shared.Rent(newCapacity);

        _buffer.AsSpan(0, _count).CopyTo(newBuffer);

        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }

    /// <summary>
    /// Releases the underlying buffer back to the pool.
    /// </summary>
    /// <remarks>
    /// After disposal, the <see cref="DecoderBuffer"/> must not be used again.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = Array.Empty<byte>();
        _count = 0;
    }
}
