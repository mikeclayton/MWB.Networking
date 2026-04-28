namespace MWB.Networking.Layer3_Endpoint.Policies.Reconnect;

public sealed class ReconnectDecision
{
    public static ReconnectDecision Now()
        => new(true, TimeSpan.Zero);

    public static ReconnectDecision After(TimeSpan delay)
        => new(true, delay);

    public static ReconnectDecision Stop
        => new(false, null);

    private ReconnectDecision(bool shouldReconnect, TimeSpan? delay)
    {
        this.ShouldReconnect = shouldReconnect;
        this.Delay = delay;
    }

    public bool ShouldReconnect
    {
        get;
    }
    
    public TimeSpan? Delay
    {
        get;
    }
}