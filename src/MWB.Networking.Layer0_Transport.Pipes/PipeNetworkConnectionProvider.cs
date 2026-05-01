using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.Abstractions;
using MWB.Networking.Layer0_Transport.Stack;
using System.IO.Pipelines;

namespace MWB.Networking.Layer0_Transport.Pipes;

public sealed class PipeNetworkConnectionProvider
    : INetworkConnectionProvider
{
    private readonly ILogger _logger;
    private readonly PipeReader _reader;
    private readonly PipeWriter _writer;
    private bool _disposed;

    public PipeNetworkConnectionProvider(
        ILogger logger,
        PipeReader reader,
        PipeWriter writer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public Task<INetworkConnection> OpenConnectionAsync(
        ObservableConnectionStatus status,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(status);

        ct.ThrowIfCancellationRequested();

        var connection =
            new PipeNetworkConnection(
                _logger,
                _reader,
                _writer,
                status);

        connection.OnStarted();

        return Task.FromResult((INetworkConnection)connection);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Provider owns the pipes, so it disposes them
        _reader.Complete();
        _writer.Complete();
    }
}