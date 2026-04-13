namespace MWB.Networking.Layer0_Transport.Memory;

public sealed class MemoryNetworkConnection : INetworkConnection
{
    public MemoryNetworkConnection(int initialCapacity = 1024 * 1024)
    {
        // Pre-size to reduce resize noise
        this.Stream = new MemoryStream(initialCapacity);
    }

    private MemoryStream Stream
    {
        get;
    }

    public Task WaitUntilConnectedAsync(CancellationToken ct)
        => Task.CompletedTask;

    public Task WriteBlockAsync(
        ReadOnlyMemory<byte>[] segments,
        CancellationToken ct)
    {
        // IMPORTANT:
        // This intentionally avoids async scheduling cost.
        // We want raw write throughput, not realism.

        foreach (var segment in segments)
        {
            if (!segment.IsEmpty)
            {
                this.Stream.Write(segment.Span);
            }
        }

        return Task.CompletedTask;
    }

    // Optional: expose stats for test inspection
    public long BytesWritten => this.Stream.Length;

    // Not used for this benchmark
    public Task<byte[]> ReadBlockAsync(CancellationToken ct)
        => throw new NotSupportedException();
}
