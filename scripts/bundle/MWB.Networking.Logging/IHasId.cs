namespace MWB.Networking.Logging;

public interface IHasId
{
    Guid Id
    {
        get;
    }

    string ShortId
        => this.Id.ToString("N")[..6];
}
