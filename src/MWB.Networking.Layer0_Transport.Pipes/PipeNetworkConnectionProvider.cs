using Microsoft.Extensions.Logging;
using System.IO.Pipelines;

namespace MWB.Networking.Layer0_Transport.Pipes;

public sealed class PipeNetworkConnectionProvider
    : INetworkConnectionProvider, IDisposable
{
    private readonly LogicalConnectionHandle _handle;

    public PipeNetworkConnectionProvider(
        ILogger logger,
        PipeReader reader,
        PipeWriter writer)
    {
        _handle = LogicalConnectionFactory.Create(logger);

        var connection = new PipeNetworkConnection(reader, writer);
        _handle.Control.Attach(connection);
    }

    public Task<LogicalConnectionHandle> OpenConnectionAsync(
        CancellationToken ct)
        => Task.FromResult(_handle);

    public void Dispose()
        => _handle.Connection.Dispose();
}
