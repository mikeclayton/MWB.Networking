namespace MWB.Networking.Layer3_Endpoint.Policies.Reconnect
{
    public interface IReconnectionPolicy
    {
        Task<ReconnectDecision> GetReconnectDecisionAsync(
            int attempt,
            Exception? cause,
            CancellationToken ct);
    }
}
