using Microsoft.Extensions.Logging;

namespace MWB.Networking.Logging;

public interface IHasLogger
{
    ILogger Logger
    {
        get;
    }
}
