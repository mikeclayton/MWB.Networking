namespace MWB.Networking.Layer1_Framing.Pipeline.Hosting;

// Stage 2: must choose byte-stream codec
public interface INetworkPipelineBuildStage
{
    NetworkPipelineFactory BuildFactory();
}
