using MWB.Networking.Layer0_Transport.Encoding;
using System.Buffers;
using System.IO.Pipelines;
using System.IO.Pipes;

namespace MWB.Networking.Layer0_Transport.Pipes;

public sealed class PipeNetworkConnection : INetworkConnection, IDisposable
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

    /// <summary>
    /// Reads raw bytes from the pipe into the provided buffer.
    /// Returns 0 on EOF.
    /// </summary>
    public async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken ct = default)
    {
        while (true)
        {
            var result = await Reader.ReadAsync(ct).ConfigureAwait(false);
            var sequence = result.Buffer;

            if (sequence.Length > 0)
            {
                // Copy as much as fits into the provided buffer
                int toCopy = (int)Math.Min(sequence.Length, buffer.Length);
                sequence.Slice(0, toCopy).CopyTo(buffer.Span);

                // Advance reader by consumed bytes
                this.Reader.AdvanceTo(sequence.GetPosition(toCopy));
                return toCopy;
            }

            if (result.IsCompleted)
            {
                // EOF
                this.Reader.AdvanceTo(sequence.End);
                return 0;
            }

            // Otherwise: no data yet, keep waiting
            this.Reader.AdvanceTo(sequence.Start, sequence.End);
        }
    }

    /// <summary>
    /// Writes raw byte segments to the pipe.
    /// </summary>
    public async ValueTask WriteAsync(
        ByteSegments segments,
        CancellationToken ct)
    {
        foreach (var segment in segments.Segments)
        {
            if (!segment.IsEmpty)
            {
                segment.Span.CopyTo(
                    this.Writer.GetSpan(segment.Length));
                this.Writer.Advance(segment.Length);
            }
        }

        var result = await this.Writer.FlushAsync(ct).ConfigureAwait(false);

        if (result.IsCompleted && !ct.IsCancellationRequested)
        {
            throw new IOException("Pipe closed during write.");
        }
    }


    public void Dispose()
    {
        try
        {
            // existing dispose logic
            this.Reader?.Complete();
            this.Writer?.Complete(new OperationCanceledException());
        }
        catch (IOException)
        {
            // Peer already disconnected — normal shutdown
        }
        catch (ObjectDisposedException)
        {
            // Already disposed — normal shutdown
        }
    }
}