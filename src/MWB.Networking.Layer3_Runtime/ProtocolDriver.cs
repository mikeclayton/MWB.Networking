using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer2_Protocol;

namespace MWB.Networking.Layer3_Runtime;

/// <summary>
/// Drives a ProtocolSession using a NetworkAdapter.
/// 
/// The ProtocolDriver owns execution, concurrency, cancellation and lifetime.
/// It contains no protocol semantics and no transport logic.
/// </summary>
internal sealed class ProtocolDriver
{
    public ProtocolDriver(
        NetworkAdapter adapter,
        ProtocolSession session)
    {
        this.Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        this.Session = session ?? throw new ArgumentNullException(nameof(session));
        this.SessionRuntime = this.Session;
    }

    private NetworkAdapter Adapter 
    {
        get;
    }

    private ProtocolSession Session
    {
        get;
    }

    private IProtocolSessionRuntime SessionRuntime
    {
        get;
    }

    private SemaphoreSlim SessionGate
    {
        get;
    } = new(1, 1);

    /// <summary>
    /// Runs the protocol driver until cancelled or a fatal error occurs.
    /// </summary>
    public async Task RunAsync(CancellationToken ct)
    {
        // We deliberately run two loops:
        //  - Read loop drives inbound frames into the protocol
        //  - Write loop drains outbound frames from the protocol
        //
        // They share no state except the ProtocolSession, and
        // are serialized via the session access discipline.
        var readTask = this.RunReadLoopAsync(ct);
        var writeTask = this.RunWriteLoopAsync(ct);

        // If either loop faults or completes, we stop the driver.
        await Task.WhenAny(readTask, writeTask).ConfigureAwait(false);
    }

    /// <summary>
    /// Continuously reads frames from the adapter and feeds them into the protocol session.
    /// </summary>
    private async Task RunReadLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            NetworkFrame networkFrame;
            try
            {
                networkFrame = await this.Adapter.ReadFrameAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                // Transport failure:
                // Let the runtime policy decide what to do.
                // For now, terminate the driver.
                throw;
            }

            // Serialize all access to ProtocolSession
            var protocolFrame = FrameConverter.ToProtocolFrame(networkFrame);
            await this.SessionGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                this.SessionRuntime.ProcessFrame(protocolFrame);
            }
            finally
            {
                this.SessionGate.Release();
            }
        }
    }

    /// <summary>
    /// Continuously drains outbound frames from the protocol session
    /// and writes them to the adapter.
    /// </summary>
    private async Task RunWriteLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Wait until outbound data exists
            await this.SessionRuntime
                .WaitForOutboundFrameAsync(ct)
                .ConfigureAwait(false);

            var protocolFrame = default(ProtocolFrame?);

            // Serialize access to ProtocolSession to dequeue one frame
            await this.SessionGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!this.SessionRuntime.TryDequeueOutboundFrame(out protocolFrame))
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
