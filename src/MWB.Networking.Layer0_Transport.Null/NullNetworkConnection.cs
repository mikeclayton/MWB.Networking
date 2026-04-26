using MWB.Networking.Layer0_Transport.Encoding;

namespace MWB.Networking.Layer0_Transport.NullConnection;

public sealed class NullNetworkConnection : INetworkConnection
{
    public ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken ct)
    {
        // Immediately signal EOF: no inbound data
        return new ValueTask<int>(0);
    }

    public ValueTask WriteAsync(
        ByteSegments segments,
        CancellationToken ct)
    {
        // Discard all outbound bytes
        return ValueTask.CompletedTask;
    }

    public ValueTask CompleteAsync()
    {
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
    }
}
