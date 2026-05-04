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
        }

        if (status is not null)
        {
            this.UnregisterConnectionStatusEvents(status);
        }
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

        // ------------------------------------------------------------
        // Phase 1: validate state and move to Connecting
        // ------------------------------------------------------------
        lock (_sync)
        {
            this.ThrowIfDisposed();

            if (_state != StackState.Idle)
            {
                throw new InvalidOperationException(
                $"Cannot connect while in state {_state}.");
            }

            _state = StackState.Connecting;

            status = new ObservableConnectionStatus();
            this.ConnectionStatus = status;

            // Wire lifecycle events *inside* the lock so that the stack
            // is fully prepared to observe transitions immediately.
            this.RegisterConnectionStatusEvents(this.ConnectionStatus);
        }

        try
        {
            // --------------------------------------------------------
            // Phase 2: initiate provider connection attempt
            // --------------------------------------------------------
            
            // The provider may synchronously or asynchronously raise
            // lifecycle events (Connecting / Connected / Faulted / Disconnected).
            physicalConnection =
                await _connectionProvider
                    .OpenConnectionAsync(status, cancellationToken)
                    .ConfigureAwait(false);

            logical = new LogicalConnection(physicalConnection, status);

            // --------------------------------------------------------
            // Phase 3: publish logical connection (race-proof)
            // --------------------------------------------------------
            lock (_sync)
            {
                // Dispose may have raced while awaiting provider
                if (_disposed)
                {
                    _state = StackState.Terminated;
                    logical.Dispose();
                    throw new ObjectDisposedException(nameof(TransportStack));
                }

                // A provider lifecycle callback may have already
                // transitioned us to a terminal state.
                if (_state != StackState.Connecting)
                {
                    logical.Dispose();
                    throw new InvalidOperationException(
                        "Connection attempt terminated before completion.");
                }

                _logicalConnection = logical;
                // NOTE: we do NOT set Connected here.
                // Connected is driven only by lifecycle events.
            }
        }
        catch (OperationCanceledException)
        {
            // ----------------------------------------------------
            // Cancellation is NON-terminal:
            // reset stack so it is reconnectable
            // ----------------------------------------------------

            lock (_sync)
            {
                _state = StackState.Idle;
                ConnectionStatus = null;
            }

            this.UnregisterConnectionStatusEvents(status);

            throw;
        }
        catch (Exception ex)
        {
            // ----------------------------------------------------
            // Real failure: terminal
            // ----------------------------------------------------
            status.OnFaulted(
                new TransportFaultedEventArgs(
                    "Connection attempt failed before establishment.",
                    ex));

            this.CleanupForTerminalEvent();

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
        ObservableConnectionStatus status;

        // determine whether it is meaningful and valid to wait for a connection:
        // - if the transport is already connected, we can complete immediately.
        // - if the transport is currently connecting, capture the active
        //   ConnectionStatus so we can await its lifecycle events.
        // - in any other state (idle, disconnecting, terminated), awaiting
        //   a connection would be a logic error because no successful connection
        //   can occur.
        lock (_sync)
        {
            this.ThrowIfDisposed();
            switch (_state)
            {
                case StackState.Connected:
                    return Task.CompletedTask;
                case StackState.Connecting:
                    status = this.ConnectionStatus
                        ?? throw new InvalidOperationException(
                            "ConnectionStatus unexpectedly missing while connecting.");
                    break;
                case StackState.Terminated:
                    throw new InvalidOperationException(
                        "Cannot await connection after the transport has been disconnected.");
                default:
                    throw new InvalidOperationException(
                        $"Cannot await connection while in state {_state}.");
            }
        }

        // Lazily allocated only if we actually need to wait.
        TaskCompletionSource? tcs = null;

        TaskCompletionSource GetOrCreateTcs()
        {
            if (tcs is not null)
                return tcs;

            var created = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

            return Interlocked.CompareExchange(ref tcs, created, null)
                ?? created;
        }

        var cleanedUp = false;

        void Cleanup()
        {
            if (Interlocked.Exchange(ref cleanedUp, true))
            {
                return;
            }

            status.Connected -= OnConnected;
            status.Faulted -= OnFaulted;
            status.Disconnected -= OnDisconnected;
        }

        void OnConnected(object? _, EventArgs __)
        {
            Cleanup();
            GetOrCreateTcs().TrySetResult();
        }

        void OnFaulted(object? _, TransportFaultedEventArgs e)
        {
            Cleanup();
            GetOrCreateTcs().TrySetException(
                new TransportFaultException(
                    "Transport faulted while awaiting connection establishment.",
                    e));
        }

        void OnDisconnected(object? _, TransportDisconnectedEventArgs e)
        {
            Cleanup();
            GetOrCreateTcs().TrySetException(
                new TransportDisconnectedException(
                    "Transport disconnected while awaiting connection establishment.",
                    e));
        }

        // Subscribe FIRST to close the race window
        status.Connected += OnConnected;
        status.Faulted += OnFaulted;
        status.Disconnected += OnDisconnected;

        // Re-check AFTER subscribing
        lock (_sync)
        {
            switch (_state)
            {
                case StackState.Connected:
                    Cleanup();
                    return Task.CompletedTask;

                case StackState.Terminated:
                    Cleanup();
                    return Task.FromException(
                        new TransportDisconnectedException(
                            "Transport disconnected before connection completed.",
                            new TransportDisconnectedEventArgs(
                                "Transport disconnected before connection completed.")));

                case StackState.Connecting:
                    // Still in progress — wait for events
                    break;

                default:
                    Cleanup();
                    throw new InvalidOperationException(
                        $"Invalid state {_state} while awaiting connection.");
            }
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
        bool shouldSignalDisconnecting;

        lock (_sync)
        {
            // Idempotent: if we're already terminated, nothing to do
            if (_state == StackState.Terminated)
            {
                return Task.CompletedTask;
            }

            // Capture current objects
            logical = _logicalConnection;
            connectionStatus = ConnectionStatus;

            _logicalConnection = null;
            this.ConnectionStatus = null;

            // Determine whether Disconnecting should be observed
            shouldSignalDisconnecting =
                _state == StackState.Connected ||
                _state == StackState.Connecting;

            // IMPORTANT:
            // Do NOT mutate _state here.
            // State transitions are owned by lifecycle handlers.
        }

        // ------------------------------------------------------------
        // Lifecycle signaling (outside lock)
        // ------------------------------------------------------------

        // Voluntary disconnect: emit Disconnecting if meaningful
        if (shouldSignalDisconnecting)
        {
            connectionStatus?.OnDisconnecting();
        }

        // Release transport resources
        logical?.Dispose();

        // Emit final Disconnected and cleanup lifecycle wiring
        if (connectionStatus is not null)
        {
            connectionStatus.OnDisconnected(
                new TransportDisconnectedEventArgs(
                    "Transport disconnected by local request."));

            this.UnregisterConnectionStatusEvents(connectionStatus);
        }

        // Ensure final terminal state
        lock (_sync)
        {
            // local DisconnectAsync resets the stack to Idle;
            // only faults or remote disconnects leave it Terminated.
            _state = StackState.Idle;
        }

        return Task.CompletedTask;
    }
}
