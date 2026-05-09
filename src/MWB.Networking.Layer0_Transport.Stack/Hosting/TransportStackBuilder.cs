using Microsoft.Extensions.Logging;

namespace MWB.Networking.Layer0_Transport.Stack.Hosting;

public sealed class TransportStackBuilder
{
    // -----------------------------
    // Initial step
    // -----------------------------

    public ITransportStackBuilderConnectionProviderStage UseLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        return new TransportStackBuilderStages()
            .UseLogger(logger);
    }
}
