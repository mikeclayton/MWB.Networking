namespace MWB.Networking.Layer0_Transport;

internal class StreamHelpers
{
    internal static async Task<byte[]> ReadExactlyAsync(Stream stream, int length, CancellationToken cancellationToken = default)
    {
        var buffer = new byte[length];
        await StreamHelpers.ReadExactlyAsync(stream, buffer, cancellationToken);
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
