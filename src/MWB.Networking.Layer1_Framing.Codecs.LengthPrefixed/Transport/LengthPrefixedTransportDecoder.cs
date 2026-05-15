using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Codec.Exceptions;
using System.Buffers;
using System.Buffers.Binary;

namespace MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.Transport;

public sealed class LengthPrefixedTransportDecoder
{
    private readonly int _maxFrameSize;

    public LengthPrefixedTransportDecoder(ILogger logger, int maxFrameSize = 16 * 1024 * 1024)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFrameSize);

        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxFrameSize = maxFrameSize;
    }

    private ILogger Logger
    {
        get;
    }

    // ─────────────────────────────────────────────────────────────
    // Decode
    // ─────────────────────────────────────────────────────────────


    /// <summary>
    /// Attempts to extract a single length-prefixed frame from the
    /// transport byte stream.
    ///
    /// Returns false if more bytes are required.
    /// Throws <see cref="TransportDecodeException"/> if the stream
    /// is provably invalid and cannot ever be decoded.
    /// </summary>
    public bool TryDecode(
        ref ReadOnlySequence<byte> inputBytes,
        out ReadOnlyMemory<byte> payload)
    {
        payload = default;

        // Need at least 4 bytes for the length prefix
        if (inputBytes.Length < 4)
        {
            return false; // need more bytes
        }

        // Read the 4-byte big-endian length prefix (may span segments)
        Span<byte> prefix = stackalloc byte[4];
        inputBytes.Slice(0, 4).CopyTo(prefix);

        var payloadLength =
            BinaryPrimitives.ReadInt32BigEndian(prefix);

        // Provably invalid framing → fatal transport error
        if (payloadLength < 0 || payloadLength > _maxFrameSize)
        {
            throw new TransportDecodeException(
                $"Invalid length-prefixed frame size: {payloadLength}");
        }

        var totalFrameLength = 4L + payloadLength;

        // Do we have the full frame yet?
        if (inputBytes.Length < totalFrameLength)
        {
            return false; // need more bytes
        }

        // Slice out the payload (after the length prefix)
        ReadOnlySequence<byte> payloadSequence =
            inputBytes.Slice(4, payloadLength);

        // Ensure the payload is a single ReadOnlyMemory<byte>
        // (copy only if necessary)
        payload = payloadSequence.IsSingleSegment
            ? payloadSequence.First
            : payloadSequence.ToArray();

        // Consume the frame atomically
        inputBytes = inputBytes.Slice(totalFrameLength);

        return true;
    }
}
