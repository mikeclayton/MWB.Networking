using MWB.Networking.Layer0_Transport.Stack.Abstractions;

namespace MWB.Networking.Layer3_Endpoint.Hosting;

public sealed partial class SessionEndpointBuilder
{
    private INetworkConnectionProvider? _connectionProvider;

    /// <summary>
    /// Sets the connection provider used to establish the underlying
    /// network connection when the endpoint starts.
    ///
    /// Codec pipeline configuration is handled separately via
    /// <see cref="ConfigurePipelineWith"/>.
    /// </summary>
    public SessionEndpointBuilder UseConnectionProvider(
        INetworkConnectionProvider connectionProvider)
    {
        _connectionProvider =
            connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        return this;
    }
}
