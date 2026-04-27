namespace MWB.Networking.Layer1_Framing.Pipeline;

public interface INetworkPipelineFactory
{
    Task<NetworkPipeline> CreatePipelineAsync(CancellationToken ct);
}
