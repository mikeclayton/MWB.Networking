using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Codec.Buffer;
using System.Buffers.Binary;

namespace MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed;

public sealed class LengthPrefixedFrameEncoder
{
    public LengthPrefixedFrameEncoder(ILogger logger)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ILogger Logger
    {
        get;
    }

    /// <summary>
    /// Encodes a single, complete input value into one or more output segments.
    /// This method is synchronous and must not block or await.
    /// </summary>
    /// <remarks>
    /// Must *not* be async
    /// </remarks>

    public void Encode(
        ICodecBufferReader inputReader,
        ICodecBufferWriter outputWriter)
    {
        // ------------------------------------------------------------
        // 1. Observe payload segments and compute total length
        // ------------------------------------------------------------
        var bytesRemaining = inputReader.Length - inputReader.Position;

        if (bytesRemaining < 0 || bytesRemaining > int.MaxValue)
        {
            throw new InvalidOperationException(
                $"Invalid payload length: {bytesRemaining}");
        }

        var payloadLength = (int)bytesRemaining;

        // ------------------------------------------------------------
        // 2. Write 4-byte big-endian length prefix
        // ------------------------------------------------------------
        Span<byte> prefix = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(prefix, payloadLength);
        outputWriter.Write(prefix);

        // ------------------------------------------------------------
        // 3. Write payload (zero-copy)
        // ------------------------------------------------------------

        var expected = payloadLength;
        var copied = 0L;
        while (inputReader.TryRead(out var memory))
        {
            // make sure we don't overrun the expected length
            // (should never happen if the input reader is well-behaved)
            copied = checked(copied + memory.Length);
            if (copied > expected)
            {
                throw new InvalidOperationException(
                    $"Encoder invariant violated: expected {expected} bytes but consumed at least {copied}.");
            }

            outputWriter.Write(memory.Span);
            inputReader.Advance(memory.Length);
        }

        // final check for payload length underrun
        // (should never happen if the input reader is well-behaved)
        if (copied != payloadLength)
        {
            throw new InvalidOperationException(
                $"Encoder invariant violated: expected {expected} bytes but consumed {copied}.");
        }
    }
}
