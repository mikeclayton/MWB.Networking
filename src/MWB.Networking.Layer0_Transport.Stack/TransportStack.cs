using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer0_Transport.Stack.Abstractions;
using MWB.Networking.Layer0_Transport.Stack.Fsm;
using MWB.Networking.Layer0_Transport.Stack.Internal;

namespace MWB.Networking.Layer0_Transport.Stack;

/// <summary>
/// Orchestrates the lifecycle of a network transport connection.
/// Owns connection creation, teardown, and state,
/// and exposes a logical byte-oriented connection surface.
/// </summary>
public sealed partial class TransportStack : IDisposable, IAsyncDisposable
{
    // -----------------------------
    // Construction
    // -----------------------------

    private readonly INetworkConnectionProvider _connectionProvider;
    private readonly bool _ownsProvider;

    private LogicalConnection? _logicalConnection;

    private int _connectionAttemptId;

    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="TransportStack"/> with the given connection provider.
    /// </summary>
    /// <param name="connectionProvider">
    /// The provider used to open physical network connections.
    /// </param>
    /// <param name="ownsProvider">
    /// <see langword="true"/> (the default) if this stack should dispose
    /// <paramref name="connectionProvider"/> when the stack itself is disposed;
    /// <see langword="false"/> if the caller retains ownership of the provider's lifetime.
    /// </param>
    public TransportStack(
        ILogger logger,
        INetworkConnectionProvider connectionProvider,
        bool ownsProvider = true)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _ownsProvider = ownsProvider;
    }

    public ILogger Logger
    {
        get;
    }

    /// <summary>
    /// Exposes the logical, ordered byte stream for this connection.
    /// Only valid while connected.
    /// </summary>
    private LogicalConnection LogicalConnection
    {
        get
        {
            lock (_sync)
            {
                if (_logicalConnection is null)
                    throw new InvalidOperationException("Transport is not connected.");

                return _logicalConnection;
            }
        }
    }

    // -----------------------------
    // Byte I/O surface
    // -----------------------------

    /// <summary>
    /// Asynchronously reads available bytes from the connection.
    /// </summary>
    public ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        LogicalConnection? conn;
        bool hasEverConnected;

        lock (_sync)
        {
            conn = _logicalConnection;
            hasEverConnected = _hasEverConnected;
        }

        // Must throw synchronously
        if (conn is null)
        {
            // distinguish "never connected" vs "EOF"
            if (hasEverConnected)
            {
                return new ValueTask<int>(0); // EOF
            }
            // Otherwise this is a logic error: read before connected
            throw new InvalidOperationException(
                "Transport is not connected.");
        }

        // Delegate async work to helper
        return TransportStack.ReadAsyncCore(conn, hasEverConnected, buffer, cancellationToken);
    }

    private static async ValueTask<int> ReadAsyncCore(
        LogicalConnection conn,
        bool hasEverConnected,
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        try
        {
            return await conn
                .ReadAsync(buffer, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // ✅ Teardown raced with read
            if (hasEverConnected)
                return 0; // EOF

            throw;
        }
    }

    /// <summary>
    /// Asynchronously writes bytes to the connection.
    /// </summary>
    public ValueTask WriteAsync(
        ByteSegments segments,
        CancellationToken cancellationToken = default)
        => this.LogicalConnection.WriteAsync(segments, cancellationToken);

    // -----------------------------
    // Disposal
    // -----------------------------

    void IDisposable.Dispose()
    {
        this.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public ValueTask DisposeAsync()
    {
        // Idempotent
        if (Interlocked.Exchange(ref _disposed, true))
        {
            return ValueTask.CompletedTask;
        }

        TransportStackTransition transition;

        lock (_sync)
        {
            transition = _machine.Process(
                TransportStackInputKind.DisposeRequested);
        }

        this.Apply(transition);

        if (_ownsProvider)
        {
            _connectionProvider.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
