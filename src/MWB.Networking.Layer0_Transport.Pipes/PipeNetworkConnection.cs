using System.Buffers.Binary;
using System.IO.Pipelines;

namespace MWB.Networking.Layer0_Transport.Pipes;

public sealed class PipeNetworkConnection : INetworkConnection
{
    public PipeNetworkConnection(PipeReader reader, PipeWriter writer)
    {
        this.Reader = reader ?? throw new ArgumentNullException(nameof(reader));
        this.Writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    private PipeReader Reader
    {
        get;
    }

    private PipeWriter Writer
    {
        get;
    }

    public Task WaitUntilConnectedAsync(CancellationToken ct)
        => Task.CompletedTask;

    public async Task WriteBlockAsync(
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
        var span = this.Writer.GetSpan(4);
        BinaryPrimitives.WriteInt32BigEndian(span, totalLength);
        this.Writer.Advance(4);

        // write segments
        foreach (var segment in segments)
        {
            if (!segment.IsEmpty)
            {
                segment.Span.CopyTo(this.Writer.GetSpan(segment.Length));
                this.Writer.Advance(segment.Length);
            }
        }

        await this.Writer.FlushAsync(ct);
    }

    public async Task<byte[]> ReadBlockAsync(CancellationToken ct)
    {
        // Read length prefix
        var lengthBytes = await ReadExactAsync(4, ct);
        int length = BinaryPrimitives.ReadInt32BigEndian(lengthBytes);

        if (length < 0)
        {
            throw new IOException("Invalid block length.");
        }

        // Read payload
        var block = await this.ReadExactAsync(length, ct);
        return block;
    }


    private async Task<byte[]> ReadExactAsync(int length, CancellationToken ct)
    {
        var result = new byte[length];
        var offset = 0;

        while (offset < length)
        {
            var readResult = await this.Reader.ReadAsync(ct);
            var buffer = readResult.Buffer;

            if (buffer.Length == 0 && readResult.IsCompleted)
            {
                throw new IOException("Unexpected end of stream.");
            }

            // Take only as much as we still need
            var toConsume = Math.Min(buffer.Length, length - offset);
            var slice = buffer.Slice(0, toConsume);

            // ✅ Copy segment-by-segment
            foreach (ReadOnlyMemory<byte> segment in slice)
            {
                segment.Span.CopyTo(result.AsSpan(offset));
                offset += segment.Length;
            }

            // Tell the PipeReader how much we consumed
            this.Reader.AdvanceTo(slice.End);
        }

        return result;
    }

}
