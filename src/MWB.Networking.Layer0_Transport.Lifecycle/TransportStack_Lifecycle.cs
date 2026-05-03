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

    private void CleanupConnection()
    {
        lock (_sync)
        {
            _logicalConnection = null;
            // ConnectionStatus handled separately (see below)
        }
    }

    private void CleanupConnectionOnDisconnect()
    {
        ObservableConnectionStatus? status;

        lock (_sync)
        {
            status = this.ConnectionStatus;
            this.ConnectionStatus = null;
        }

        if (status is null)
        {
            return;
        }

        UnregisterConnectionStatusEvents(status);
    }

    private void RaiseFaultedEvent(TransportFaultedEventArgs e)
        => this.Faulted?.Invoke(this, e);

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

                // The stack may have been disposed while the connection was
                // being established. In that case we must not publish a live
                // logical connection.
                if (_disposed)
                {
                    logical.Dispose();
                    throw new ObjectDisposedException(nameof(TransportStack));
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

            status.OnFaulted(
                new TransportFaultedEventArgs(
                    "Connection attempt failed before establishment.",
                    ex));

            // Cleanup after terminal signalling
            UnregisterConnectionStatusEvents(status);

            lock (_sync)
            {
                this.ConnectionStatus = null;
                _logicalConnection = null;
            }

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

        var tcs = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var cleanedUp = false;

        void Cleanup()
        {
            if (Interlocked.CompareExchange(ref cleanedUp, true, false))
            {
                // already cleaned up
                return;
            }

            cleanedUp = true;

            statusEventSource.Connected -= OnConnected;
            statusEventSource.Faulted -= OnFaulted;
            statusEventSource.Disconnected -= OnDisconnected;
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
    public Task DisconnectAsync()
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
