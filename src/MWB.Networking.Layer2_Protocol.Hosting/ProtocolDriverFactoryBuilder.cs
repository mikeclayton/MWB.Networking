namespace MWB.Networking.Layer2_Protocol.Hosting;

public sealed partial class ProtocolDriverFactoryBuilder
{
    public IProtocolDriverFactory Build()
    {
        if (_logger is null)
        {
            throw new InvalidOperationException(
                "Logger not configured.");
        }

        return new ProtocolDriverFactory(
            _logger);
    }
}
