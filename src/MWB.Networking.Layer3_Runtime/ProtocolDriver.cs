using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Logging;

namespace MWB.Networking.Layer3_Runtime;

/// <summary>
/// Drives a ProtocolSession using a NetworkAdapter.
///
/// The ProtocolDriver owns execution, concurrency, cancellation and lifetime.
/// It contains no protocol semantics and no transport logic.
/// </summary>
public sealed class ProtocolDriver : IHasLogger
{
    public ProtocolDriver(
        ILogger logger,
        NetworkAdapter adapter,
        ProtocolSessionHandle session)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public ILogger Logger
    {
        get;
    }

    private NetworkAdapter Adapter
    {
        get;
    }

    private ProtocolSessionHandle Session
    {
        get;
    }

    private SemaphoreSlim SessionGate
    {
        get;
    } = new(1, 1);

    private TaskCompletionSource ReadySource
    {
        get;
    } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Ready => this.ReadySource.Task;

    /// <summary>
    /// Runs the protocol driver until cancelled or a fatal error occurs.
    /// </summary>
    [LogMethod]
    public async Task RunAsync(CancellationToken ct)
    {
        using var logScope = this.Logger.BeginMethodScope(this);
        this.Logger.LogDebug("Entering method");

        // We deliberately run two loops:
        //  - Read loop drives inbound frames into the protocol
        //  - Write loop drains outbound frames from the protocol
        //
        // They share no state except the ProtocolSession, and
        // are serialized via the session access discipline.
        var readTask = this.RunReadLoopAsync(ct);
        var writeTask = this.RunWriteLoopAsync(ct);

        this.ReadySource.TrySetResult();

        // If either loop faults or completes, we stop the driver.
        await Task.WhenAny(readTask, writeTask).ConfigureAwait(false);

        this.Logger.LogDebug("Leaving method");
    }

    /// <summary>
    /// Continuously reads frames from the adapter and feeds them into the protocol session.
    /// </summary>
    [LogMethod]
    private async Task RunReadLoopAsync(CancellationToken ct)
    {
        using var scope = this.Logger.BeginMethodScope(this);
        this.Logger.LogDebug($"[DRIVER READ LOOP] entering");
        while (!ct.IsCancellationRequested)
        {
            NetworkFrame networkFrame;
            try
            {
                // Blocks until a complete NetworkFrame is read from the transport.
                networkFrame = await this.Adapter.ReadFrameAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Thrown when the local cancellation token is signaled
                // (normal shutdown or driver termination).
                this.Logger.LogDebug("[DRIVER READ LOOP] returning (cancelled)");
                return;
            }
            catch (IOException ex)
            {
                // Thrown when the remote peer cleanly closes the connection (EOF)
                // or the underlying socket is closed during a read.
                this.Logger.LogDebug(
                    ex,
                    "[DRIVER READ LOOP] connection closed – exiting read loop");
                return;
            }
            catch (ObjectDisposedException ex)
            {
                // Thrown if the connection or stream is disposed while a read is in progress
                // (e.g. shutdown racing with an active read).
                this.Logger.LogDebug(
                    ex,
                    "[DRIVER READ LOOP] connection disposed – exiting read loop");
                return;
            }
            catch (Exception ex)
            {
                // Thrown for unexpected or fatal transport/protocol errors
                // (e.g. framing corruption, invariants violated).
                // Let the runtime policy decide how to handle this.
                this.Logger.LogError(
                    ex,
                    "[DRIVER READ LOOP] fatal error – terminating driver");
                throw;
            }

            // Serialize all access to ProtocolSession
            var protocolFrame = FrameConverter.ToProtocolFrame(networkFrame);
            await this.SessionGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                this.Session.Runtime.ProcessFrame(protocolFrame);
            }
            finally
            {
                this.SessionGate.Release();
            }
        }
        this.Logger.LogDebug($"[DRIVER READ LOOP] leaving");
    }

    /// <summary>
    /// Continuously drains outbound frames from the protocol session
    /// and writes them to the adapter.
    /// </summary>
    [LogMethod]
    private async Task RunWriteLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Wait until outbound data exists
            await this.Session.Runtime
                .WaitForOutboundFrameAsync(ct)
                .ConfigureAwait(false);

            var protocolFrame = default(ProtocolFrame?);

            // Serialize access to ProtocolSession to dequeue one frame
            await this.SessionGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!this.Session.Runtime.TryDequeueOutboundFrame(out protocolFrame))
                {
                    // No outbound work available right now
                    protocolFrame = null!;
                }
            }
            finally
            {
                this.SessionGate.Release();
            }

            // If there was nothing to send, yield and continue
            if (protocolFrame is null)
            {
                await Task.Yield();
                continue;
            }

            var networkFrame = FrameConverter.ToNetworkFrame(protocolFrame);
            try
            {
                await this.Adapter.WriteFrameAsync(networkFrame, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                // Transport failure:
                // Abort; read loop will also terminate.
                throw;
            }
        }
    }
}
