using MWB.Networking.Layer1_Framing.Codec.Buffer;

namespace MWB.Networking.Layer1_Framing.Codecs.Default.UnitTests.Helpers;

/// <summary>
/// A test-only <see cref="ICodecBufferReader"/> whose <see cref="Length"/>
/// property is decoupled from the actual bytes in its segments.
///
/// This lets decode edge-case tests exercise guards that a well-behaved real
/// reader can never trigger — for example a reported <see cref="Length"/> of
/// -1 (negative) or a value that exceeds <see cref="int.MaxValue"/>.
/// </summary>
internal sealed class FakeCodecBufferReader : ICodecBufferReader
{
    private readonly List<ReadOnlyMemory<byte>> _segments;
    private int _segmentIndex;
    private long _position;

    /// <param name="claimedLength">
    /// The value returned by <see cref="Length"/>, regardless of how many
    /// bytes are actually available in <paramref name="segments"/>.
    /// </param>
    /// <param name="segments">
    /// Byte arrays returned one-at-a-time by <see cref="TryRead"/>.
    /// </param>
    internal FakeCodecBufferReader(long claimedLength, params byte[][] segments)
    {
        Length = claimedLength;
        _segments = segments
            .Select(s => (ReadOnlyMemory<byte>)s)
            .ToList();
    }

    public long Position => _position;

    /// <summary>
    /// The declared byte count — may not equal the actual bytes in the segments.
    /// </summary>
    public long Length { get; }

    public bool IsCompleted => _segmentIndex >= _segments.Count;

    public bool TryRead(out ReadOnlyMemory<byte> memory)
    {
        if (_segmentIndex >= _segments.Count)
        {
            memory = default;
            return false;
        }

        memory = _segments[_segmentIndex];
        return true;
    }

    /// <summary>
    /// Advances to the next segment. The byte count argument is ignored; the
    /// reader always moves forward by exactly one segment per call.
    /// </summary>
    public void Advance(int count)
    {
        _position += count;
        _segmentIndex++;
    }
}
