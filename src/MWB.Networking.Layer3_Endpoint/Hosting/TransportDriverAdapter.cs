using MWB.Networking.Layer1_Framing.Driver;

namespace MWB.Networking.Layer3_Endpoint.Hosting;

/// <summary>
/// Adapts the internal <see cref="TransportDriver"/> to the public
/// <see cref="IProtocolDriver"/> interface expected by <see cref="SessionEndpoint"/>.
///
/// This adapter exists so that <see cref="SessionEndpoint"/> in
/// <c>Layer3_Endpoint</c> can hold an <see cref="IProtocolDriver"/> reference
/// without taking a compile-time dependency on the internal
/// <see cref="TransportDriver"/> type.
/// </summary>
internal sealed class TransportDriverAdapter : ITransportDriver
{
    private readonly TransportDriver _driver;

    internal TransportDriverAdapter(TransportDriver driver)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    public void Start() => _driver.Start();

    public void Dispose() => _driver.Dispose();
}
