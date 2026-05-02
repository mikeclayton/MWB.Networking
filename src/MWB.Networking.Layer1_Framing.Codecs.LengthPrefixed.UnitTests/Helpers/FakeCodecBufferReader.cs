using MWB.Networking.Layer1_Framing.Codec.Buffer;

namespace MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.UnitTests.Helpers;

/// <summary>
/// A test-only implementation of <see cref="ICodecBufferReader"/> that
/// allows the declared <see cref="Length"/> to be independent of the
/// actual bytes available.
///
/// This lets encoder invariant-violation tests drive the encoder into error
/// paths that real, well-behaved readers never reach.
/// </summary>
internal sealed class FakeCodecBufferReader : ICodecBufferReader
{
    private readonly List<ReadOnlyMemory<byte>> _segments;
    private int _segmentIndex;
    private long _position;

    /// <param name="claimedLength">
    /// The value returned by <see cref="Length"/>. May differ from the actual
    /// number of bytes in <paramref name="segments"/> to simulate misbehaving
    /// readers.
    /// </param>
    /// <param name="segments">
    /// The byte arrays returned one-at-a-time by <see cref="TryRead"/>.
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
    /// Advances to the next segment. Ignores the byte count (the encoder
    /// always passes exactly the length of the segment it just read).
    /// </summary>
    public void Advance(int count)
    {
        _position += count;
        _segmentIndex++;
    }
}
