using System.Buffers.Binary;
using System.Net;

namespace MWB.Networking.Layer0_Transport;

public static class LengthPrefixedBlockHelpers
{
    public static async Task WriteBlockAsync(
        Stream stream,
        ReadOnlyMemory<byte>[] segments,
        CancellationToken ct)
    {
        // compute total length
        var totalLength = 0;
        foreach (var segment in segments)
        {
            totalLength += segment.Length;
        }

        // write length prefix
        var lengthPrefix = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, totalLength);
        await stream.WriteAsync(lengthPrefix, ct).ConfigureAwait(false);

        // write segments
        foreach (var segment in segments)
        {
            if (!segment.IsEmpty)
            {
                await stream.WriteAsync(segment, ct).ConfigureAwait(false);
            }
        }

        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    public static async Task<byte[]> ReadBlockAsync(
        Stream stream, int maxFrameSize,
        CancellationToken ct)
    {
        // Read exactly one length-prefixed transport unit.
        // NOTE: This is transport-level framing, *not* message framing.
        var lengthBytes = await LengthPrefixedBlockHelpers.ReadExactlyAsync(stream, 4, ct);
        int length = IPAddress.NetworkToHostOrder(
            BitConverter.ToInt32(lengthBytes));
        if ((length < 0) || (length > maxFrameSize))
        {
            throw new IOException("Invalid frame length.");
        }
        var buffer = await LengthPrefixedBlockHelpers.ReadExactlyAsync(stream, length, ct);
        return buffer;
    }

    internal static async Task<byte[]> ReadExactlyAsync(Stream stream, int length, CancellationToken cancellationToken = default)
    {
        var buffer = new byte[length];
        await LengthPrefixedBlockHelpers.ReadExactlyAsync(stream, buffer, cancellationToken);
        return buffer;
    }

    internal static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken = default)
    {
        var totalBytesRead = 0;
        var bytesRemaining = buffer.Length;
        while (bytesRemaining > 0)
        {
            var chunkBytesRead = await stream.ReadAsync(buffer.AsMemory(totalBytesRead, bytesRemaining), cancellationToken)
                .ConfigureAwait(false);
            if (chunkBytesRead == 0)
            {
                // stream closed before all bytes arrived
                throw new IOException($"Unexpected end of stream. Needed {bytesRemaining} more bytes.");
            }
            totalBytesRead += chunkBytesRead;
            bytesRemaining -= chunkBytesRead;
        }
    }

    internal static async Task ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var totalBytesRead = 0;
        var bytesRemaining = buffer.Length;
        while (bytesRemaining > 0)
        {
            var chunkBytesRead = await stream.ReadAsync(buffer.Slice(totalBytesRead, bytesRemaining), cancellationToken)
                .ConfigureAwait(false);
            if (chunkBytesRead == 0)
            {
                // stream closed before all bytes arrived
                throw new EndOfStreamException($"Unexpected end of stream. Needed {bytesRemaining} more bytes.");
            }
            totalBytesRead += chunkBytesRead;
            bytesRemaining -= chunkBytesRead;
        }
    }
}
