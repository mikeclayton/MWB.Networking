using MWB.Networking.Layer0_Transport.Encoding;
using System.Net.Sockets;

namespace MWB.Networking.Layer0_Transport.Tcp;

/// <summary>
/// Represents a single physical TCP connection.
/// Instances are created and owned by <see cref="TcpNetworkConnectionProvider"/>.
/// Consumers should interact with <see cref="ILogicalConnection"/> instead.
/// </summary>
internal sealed class TcpNetworkConnection : INetworkConnection
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;

    internal TcpNetworkConnection(TcpClient client, int maxFrameSize)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _stream = client.GetStream();
    }


    /// <summary>
    /// Reads raw bytes from the network stream.
    /// Returns 0 on EOF.
    /// </summary>
    public ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken ct)
    {
        return _stream.ReadAsync(buffer, ct);
    }

    /// <summary>
    /// Writes raw byte segments to the network stream.
    /// </summary>
    public async ValueTask WriteAsync(
        ByteSegments segments,
        CancellationToken ct)
    {
        foreach (var segment in segments.Segments)
        {
            if (!segment.IsEmpty)
            {
                await _stream
                    .WriteAsync(segment, ct)
                    .ConfigureAwait(false);
            }
        }

        await _stream.FlushAsync(ct).ConfigureAwait(false);
    }

    public void Dispose()
    {
        try
        {
            _stream.Dispose();
        }
        finally
        {
            _client.Dispose();
        }
    }
}
