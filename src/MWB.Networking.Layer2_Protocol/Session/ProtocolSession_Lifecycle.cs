using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer2_Protocol.Session;

internal sealed partial class ProtocolSession : IProtocolSessionLifecycle
{
    public Task Ready
    {
        get
        {
            if (this.ProtocolDriver is null)
            {
                throw new InvalidOperationException("Protocol driver not attached.");
            }

            return this.ProtocolDriver.Ready;
        }
    }

    public Task StartAsync(CancellationToken ct)
    {
        if (this.ProtocolDriver is null)
        {
            throw new InvalidOperationException("Protocol driver not attached.");
        }

        return this.ProtocolDriver.RunAsync(ct);
    }
}
