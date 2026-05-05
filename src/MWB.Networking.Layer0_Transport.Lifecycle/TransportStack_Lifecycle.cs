using MWB.Networking.Layer0_Transport.Lifecycle.Exceptions;
using MWB.Networking.Layer0_Transport.Lifecycle.Fsm;
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

    private void TearDownConnection()
    {
        LogicalConnection? logical;
        ObservableConnectionStatus? status;

        lock (_sync)
        {
            logical = _logicalConnection;
            status = this.ConnectionStatus;

            _logicalConnection = null;
            // ensures ConnectionState == null
            this.ConnectionStatus = null;
        }

        logical?.Dispose();

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
        int attemptId;

        // ------------------------------------------------------------
        // Phase 1: request transition
        // ------------------------------------------------------------
        TransportStackTransition transition;
        lock (_sync)
        {
            this.ThrowIfDisposed();

            transition = _machine.Process(
                TransportStackInputKind.ConnectRequested);

            if (transition.NextState is not TransportStackState.Connecting)
            {
                throw new InvalidOperationException(
                    $"Connect not allowed in state {_machine.State}.");
            }

            // Start a new connection attempt
            attemptId = ++_connectionAttemptId;
        }

        this.Apply(transition);


        // ------------------------------------------------------------
        // Phase 2: initiate provider connection attempt
        // ------------------------------------------------------------
        status = new ObservableConnectionStatus();
        this.RegisterConnectionStatusEvents(status);

        try
        {
            var physical =
                await _connectionProvider
                    .OpenConnectionAsync(status, cancellationToken)
                    .ConfigureAwait(false);

            var logical = new LogicalConnection(physical, status);

            lock (_sync)
            {
                // Dispose may have raced
                if (_disposed || (_connectionAttemptId != attemptId))
                {
                    logical.Dispose();
                    ObjectDisposedException.ThrowIf(_disposed, this);
                    // concurrent disconnect won the race — abandon silently
                    return;
                }

                // ✅ Publish logical connection for *this* attempt
                // Connected state is still driven by provider events
                _logicalConnection = logical;
                this.ConnectionStatus = status;
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation is NON-terminal
            lock (_sync)
            {
                this.Apply(
                    _machine.Process(
                        TransportStackInputKind.ProviderDisconnected));
            }

            this.UnregisterConnectionStatusEvents(status);
            throw;
        }
        catch (Exception ex)
        {
            // REAL FAILURE → lifecycle fault
            lock (_sync)
            {
                this.Apply(
                    _machine.Process(
                        TransportStackInputKind.ProviderFaulted,
                        new TransportFaultedEventArgs(
                            "Connection attempt failed.", ex)));
            }

            this.UnregisterConnectionStatusEvents(status);
            throw;
        }
    }

    /// <summary>
    /// Asynchronously waits until the transport reaches the Connected state.
    /// Completes with an exception if the connection faults or disconnects
    /// before becoming connected.
    /// </summary>
    public Task AwaitConnectedAsync(CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<bool>? tcs = null;
        int attemptId;

        TaskCompletionSource<bool> GetTcs()
        {
            return tcs ??= new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }

        void Cleanup()
        {
            this.ConnectionStateChanged -= OnStateChanged;
            this.Faulted -= OnFaulted;
        }

        bool AbortIfSuperseded(
            int attemptId,
            Action fail)
        {
            lock (_sync)
            {
                if (attemptId == _connectionAttemptId)
                {
                    return false;
                }
            }

            fail();
            return true;
        }

        void FailDisconnected(string message)
        {
            Cleanup();
            GetTcs().TrySetException(
                new TransportDisconnectedException(
                    message,
                    new TransportDisconnectedEventArgs(message)));
        }

        void OnStateChanged(object? _, TransportConnectionState state)
        {
            if (AbortIfSuperseded(
                attemptId,
                () => FailDisconnected(
                    "Connection attempt was superseded by a new attempt.")))
            {
                return;
            }

            switch (state)
            {
                case TransportConnectionState.Connected:
                    Cleanup();
                    GetTcs().TrySetResult(true);
                    break;

                case TransportConnectionState.Disconnected:
                    FailDisconnected(
                        "Transport disconnected while awaiting connection establishment.");
                    break;
            }
        }

        void OnFaulted(object? _, TransportFaultedEventArgs e)
        {

            if (AbortIfSuperseded(
                attemptId,
                () => FailDisconnected(
                    "Connection attempt was superseded by a new attempt.")))
            {
                return;
            }

            Cleanup();
            GetTcs().TrySetException(
                new TransportFaultException(
                    "Transport faulted while awaiting connection establishment.", e));
        }

        lock (_sync)
        {
            this.ThrowIfDisposed();

            var state = _machine.State;

            // Fast-path: already connected
            if (state == TransportStackState.Connected)
            {
                return Task.CompletedTask;
            }

            // Precondition: must be inside a connect attempt
            if (state != TransportStackState.Connecting)
            {
                throw new InvalidOperationException(
                    $"Cannot await connection while in state {state}.");
            }

            // Capture attempt identity
            attemptId = _connectionAttemptId;

            // Subscribe before re-check to close race window
            this.ConnectionStateChanged += OnStateChanged;
            this.Faulted += OnFaulted;

            // If the attempt already ended (FSM left Connecting),
            // fail immediately so we can never hang.
            if ((_machine.State != TransportStackState.Connecting)
                || (attemptId != _connectionAttemptId))
            {
                Cleanup();
                throw new TransportDisconnectedException(
                    "Transport disconnected before becoming connected.",
                    new TransportDisconnectedEventArgs(
                        "Transport disconnected before becoming connected."));
            }
        }

        // Cancellation
        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() =>
            {
                Cleanup();
                GetTcs().TrySetCanceled(cancellationToken);
            });
        }

        return GetTcs().Task;
    }

    /// <summary>
    /// Gracefully disconnects the current connection.
    /// Signals lifecycle transitions and releases transport resources.
    /// </summary>
    public Task DisconnectAsync()
    {
        TransportStackTransition transition;

        lock (_sync)
        {
            this.ThrowIfDisposed();

            transition = _machine.Process(
                TransportStackInputKind.DisconnectRequested);
        }

        this.Apply(transition);

        // Provider-level teardown is mechanical, not semantic
        _logicalConnection?.Dispose();

        return Task.CompletedTask;
    }
}
