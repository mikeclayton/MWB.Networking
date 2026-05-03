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

    /// <summary>
    /// Handles transition of the stack to the Disconnected state.
    ///
    /// This method is intentionally idempotent. It is the single cleanup path
    /// for provider-initiated and remote disconnects.
    ///
    /// In the voluntary DisconnectAsync path, the stack may already have
    /// nulled internal fields before the lifecycle raises Disconnected.
    /// In that case, HandleDisconnected will run redundantly and must remain
    /// safe to do so.
    /// </summary>
    private void CleanupOnDisconnected(TransportDisconnectedEventArgs e)
    {
        lock (_sync)
        {
            _logicalConnection = null;
            this.ConnectionStatus = null;
        }
    }

    private void RaiseFaultedEvent(TransportFaultedEventArgs e)
        => this.Faulted?.Invoke(this, e);

    /// <summary>
    /// Establishes a new network connection using the configured provider.
    /// </summary>
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
            // Request a physical connection attempt.
            // The provider may synchronously signal lifecycle events.
            physicalConnection =
                await _connectionProvider
                    .OpenConnectionAsync(status, cancellationToken)
                    .ConfigureAwait(false);
        }
        catch
        {
            // Failed before a logical connection existed – reset state.
            lock (_sync)
            {
                if (this.ConnectionStatus is null)
                {
                    return;
                }
                this.UnregisterConnectionStatusEvents(this.ConnectionStatus);
                this.ConnectionStatus = null;
            }
            throw;
        }

        logical = new LogicalConnection(physicalConnection, status);

        lock (_sync)
        {
            if (_disposed)
            {
                logical.Dispose();
                throw new ObjectDisposedException(nameof(TransportStack));
            }

            _logicalConnection = logical;
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

        var tcs = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        CancellationTokenRegistration ctr = default;
        var cleanedUp = false;

        void Cleanup()
        {
            if (cleanedUp)
            {
                return;
            }

            cleanedUp = true;

            statusEventSource.Connected -= OnConnected;
            statusEventSource.Faulted -= OnFaulted;
            statusEventSource.Disconnected -= OnDisconnected;

            ctr.Dispose();
        }

        void OnConnected(object? _, EventArgs __)
        {
            Cleanup();
            tcs.TrySetResult();
        }

        void OnFaulted(object? _, TransportFaultedEventArgs e)
        {
            Cleanup();
            tcs.TrySetException(new TransportFaultException(
                "Transport faulted while awaiting connection establishment.",
                e));
        }

        void OnDisconnected(object? _, TransportDisconnectedEventArgs e)
        {
            Cleanup();
            tcs.TrySetException(new TransportDisconnectedException(
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
                return Task.CompletedTask;

            case TransportConnectionState.Faulted:
                Cleanup();
                var faultMessage = "Transport faulted before connection completed.";
                return Task.FromException(
                    new TransportFaultException(
                        faultMessage,
                        new TransportFaultedEventArgs(
                            faultMessage)));

            case TransportConnectionState.Disconnected:
                Cleanup();
                var disconnectMessage = "Transport disconnected before connection completed.";
                return Task.FromException(
                    new TransportDisconnectedException(
                        disconnectMessage,
                        new TransportDisconnectedEventArgs(
                            disconnectMessage)));

            default:
                // Connecting / Disconnecting:
                // leave handlers subscribed; events will complete the TCS
                break;
        }

        return tcs.Task;
    }

    /// <summary>
    /// Gracefully disconnects the current connection.
    /// Signals lifecycle transitions and releases transport resources.
    /// </summary>
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        this.ThrowIfDisposed();

        LogicalConnection? logical;
        ObservableConnectionStatus? connectionStatus;

        lock (_sync)
        {
            logical = _logicalConnection;
            connectionStatus = this.ConnectionStatus;

            _logicalConnection = null;
            this.ConnectionStatus = null;
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
