namespace MWB.Networking.Logging.Scopes;

public sealed class NullScope : IDisposable
{
    public static readonly NullScope Instance = new();

    public void Dispose()
    {
    }
}
