using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer2_Protocol.Session;

internal sealed partial class ProtocolSession : IProtocolSessionLifecycle
{
    public Task Ready
    {
        get
        {
            if (this.Driver is null)
                throw new InvalidOperationException("ProtocolDriver not attached.");

            return this.Driver.Ready;
        }
    }

    public Task StartAsync(CancellationToken ct)
    {
        if (this.Driver is null)
        {
            throw new InvalidOperationException("Protocol driver not attached.");
        }

        return this.Driver.RunAsync(ct);
    }
}
