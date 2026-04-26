using MWB.Networking.Layer2_Protocol.Session.Api;
using MWB.Networking.Logging;

namespace MWB.Networking.Layer2_Protocol.Session;

internal sealed partial class ProtocolSession : IProtocolSessionLifecycle
{
    public Task WhenReady
    {
        get
        {
            if (this.ProtocolDriver is null)
            {
                throw new InvalidOperationException("Protocol driver not attached.");
            }

            return this.ProtocolDriver.WhenStarted;
        }
    }

    public Task StartAsync(CancellationToken ct)
    {
        using var loggerScope = this.Logger.BeginMethodLoggingScope(this);

        if (this.ProtocolDriver is null)
        {
            throw new InvalidOperationException("Protocol driver not attached.");
        }

        return this.ProtocolDriver.RunAsync(ct);
    }
}
