using MWB.Networking.Layer0_Transport.Lifecycle.Abstractions;
using MWB.Networking.Layer0_Transport.Lifecycle.Exceptions;
using MWB.Networking.Layer0_Transport.Lifecycle.Internal;
using MWB.Networking.Layer0_Transport.Lifecycle.Stack;

namespace MWB.Networking.Layer0_Transport.Lifecycle;

/// <summary>
/// Orchestrates the lifecycle of a network transport connection.
/// Owns connection creation, teardown, and state,
/// and exposes a logical byte-oriented connection surface.
/// </summary>
public sealed partial class TransportStack
{
    // -----------------------------
    // Lifecycle operations
    // -----------------------------

    private void CleanupForTerminalEvent()
    {
        ObservableConnectionStatus? status;

        lock (_sync)
        {
            _logicalConnection = null;
            status = this.ConnectionStatus;
            this.ConnectionStatus = null;
            _hasTerminated = true;
        }

        if (status is null)
        {
            return;
        }

        UnregisterConnectionStatusEvents(status);
    }

    /// <summary>
    /// Initiates a new transport connection attempt using the configured provider.
    /// This method starts the connection lifecycle but does not wait for the
    /// Connected state; callers that require a ready transport should call
    /// AwaitConnectedAsync().
    /// </summary>
    public async Task ConnectAsync(
        CancellationToken cancellationToken = default)
    {
        ObservableConnectionStatus status;
        INetworkConnection physicalConnection;
        LogicalConnection logical;

        lock (_sync)
        {
            this.ThrowIfDisposed();
            if (_logicalConnection is not null || this.ConnectionStatus is not null)
            {
                throw new InvalidOperationException(
                    "Transport is already connecting or connected.");
            }
            status = new ObservableConnectionStatus();
            this.ConnectionStatus = status;
            // Wire lifecycle events *inside* the lock so that the stack
            // is fully prepared to observe transitions immediately.
            this.RegisterConnectionStatusEvents(this.ConnectionStatus);
        }

        try
        {
            // Initiate the connection attempt. The provider may
            // synchronously or asynchronously raise lifecycle events
            // (Connecting / Connected / Faulted / Disconnected).
            physicalConnection =
                await _connectionProvider
                    .OpenConnectionAsync(status, cancellationToken)
                    .ConfigureAwait(false);

            logical = new LogicalConnection(physicalConnection, status);

            lock (_sync)
            {
                // Guard against two racing terminal conditions:
                // 1. The stack was disposed while the connection was being established.
                // 2. DisconnectAsync was called concurrently — it will have nulled
                //    ConnectionStatus before we got here, so status != this.ConnectionStatus.
                //    Publishing the logical connection in that case would leave the stack in
                //    a zombie state (live _logicalConnection, null ConnectionStatus).
                if (_disposed || this.ConnectionStatus != status)
                {
                    logical.Dispose();
                    if (_disposed)
                        throw new ObjectDisposedException(nameof(TransportStack));

                    // Concurrent disconnect won the race.
                    // The Disconnected lifecycle events were already fired by DisconnectCoreAsync;
                    // the physical connection has been cleaned up above. Return silently.
                    return;
                }

                _logicalConnection = logical;
            }

        }
        catch (Exception ex)
        {
            // IMPORTANT:
            // If OpenConnectionAsync throws before a terminal lifecycle event
            // is raised, observers may have already seen Connecting().
            // We must explicitly close the lifecycle stream with a terminal
            // Faulted state before tearing everything down.
            //
            // This preserves the invariant that every lifecycle stream
            // ends in a terminal state.
            //
            // The registered OnFaulted handler (TransportStack_Status) takes
            // care of all cleanup: CleanupConnection, CleanupConnectionOnDisconnect,
            // and UnregisterConnectionStatusEvents. We must not repeat any of
            // that work here.
            //
            // NOTE — concurrent-disconnect safety:
            // If DisconnectCoreAsync raced ahead and already called
            // status.OnDisconnected(), _hasTerminated is already true.
            // ObservableConnectionStatus.Terminal() will silently absorb this
            // OnFaulted call as a no-op. We intentionally do NOT guard with an
            // explicit HasTerminated check here: that would introduce a TOCTOU
            // race (the flag could flip between the read and the call), and
            // Terminal() is already the authoritative idempotency guard.
            status.OnFaulted(
                new TransportFaultedEventArgs(
                    "Connection attempt failed before establishment.",
                    ex));

            throw;
        }
    }

    /// <summary>
    /// Asynchronously waits until the transport reaches the Connected state.
    /// Completes with an exception if the connection faults or disconnects
    /// before becoming connected.
    /// </summary>
    public Task AwaitConnectedAsync()
    {
        ObservableConnectionStatus statusEventSource;

        lock (_sync)
        {
            this.ThrowIfDisposed();

            statusEventSource = this.ConnectionStatus
                ?? throw new InvalidOperationException(
                    "TransportStack is not connecting or connected.");
        }

        // Fast-path: already connected
        if (statusEventSource.State == TransportConnectionState.Connected)
        {
            return Task.CompletedTask;
        }

        // Lazily allocated only if we actually need to wait.
        TaskCompletionSource? tcs = null;

        TaskCompletionSource GetOrCreateTcs()
        {
            if (tcs is not null) return tcs;
            var newTcs = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            return Interlocked.CompareExchange(ref tcs, newTcs, null) ?? newTcs;
        }

        var cleanedUp = false;

        void Cleanup()
        {
            if (Interlocked.CompareExchange(ref cleanedUp, true, false))
            {
                // was already cleaned up
                return;
            }

            statusEventSource.Connected -= OnConnected;
            statusEventSource.Faulted -= OnFaulted;
            statusEventSource.Disconnected -= OnDisconnected;
        }

        void OnConnected(object? _, EventArgs __)
        {
            Cleanup();
            GetOrCreateTcs().TrySetResult();
        }

        void OnFaulted(object? _, TransportFaultedEventArgs e)
        {
            Cleanup();
            GetOrCreateTcs().TrySetException(new TransportFaultException(
                "Transport faulted while awaiting connection establishment.",
                e));
        }

        void OnDisconnected(object? _, TransportDisconnectedEventArgs e)
        {
            Cleanup();
            GetOrCreateTcs().TrySetException(new TransportDisconnectedException(
                "Transport disconnected while awaiting connection establishment.",
                e));
        }

        // Subscribe FIRST
        statusEventSource.Connected += OnConnected;
        statusEventSource.Faulted += OnFaulted;
        statusEventSource.Disconnected += OnDisconnected;

        // Re-check AFTER subscribing to close the race window
        switch (statusEventSource.State)
        {
            case TransportConnectionState.Connected:
                Cleanup();
                return Task.CompletedTask; // no TCS allocated

            case TransportConnectionState.Faulted:
                Cleanup();
                var faultMessage = "Transport faulted before connection completed.";
                return Task.FromException(
                    new TransportFaultException(
                        faultMessage,
                        new TransportFaultedEventArgs(
                            faultMessage))); // no TCS allocated

            case TransportConnectionState.Disconnected:
                if (!statusEventSource.HasTerminated)
                {
                    // initial Disconnected → still connecting
                    break;
                }

                Cleanup();
                var disconnectMessage = "Transport disconnected before connection completed.";
                return Task.FromException(
                    new TransportDisconnectedException(
                        disconnectMessage,
                        new TransportDisconnectedEventArgs(
                            disconnectMessage))); // no TCS allocated

            default:
                // Connecting / Disconnecting:
                // leave handlers subscribed; events will complete the TCS
                break;
        }

        return GetOrCreateTcs().Task;
    }

    /// <summary>
    /// Gracefully disconnects the current connection.
    /// Signals lifecycle transitions and releases transport resources.
    /// </summary>
    public Task DisconnectAsync()
    {
        this.ThrowIfDisposed();
        return this.DisconnectCoreAsync();
    }

    private Task DisconnectCoreAsync()
    {

        LogicalConnection? logical;
        ObservableConnectionStatus? connectionStatus;

        lock (_sync)
        {
            logical = _logicalConnection;
            connectionStatus = this.ConnectionStatus;

            _logicalConnection = null;
            this.ConnectionStatus = null;
            _hasTerminated = true;
        }

        // Voluntary disconnect: drive lifecycle explicitly
        connectionStatus?.OnDisconnecting();

        // Release transport resources
        logical?.Dispose();

        if (connectionStatus is not null)
        {
            connectionStatus.OnDisconnected(
                new TransportDisconnectedEventArgs(
                    "Transport disconnected by local request."));

            // Unsubscribe stack-owned handlers
            this.UnregisterConnectionStatusEvents(connectionStatus);
        }

        return Task.CompletedTask;
    }
}
