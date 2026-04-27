using MWB.Networking.Layer1_Framing.Hosting;
using MWB.Networking.Layer1_Framing.Pipeline;

namespace MWB.Networking.PerformanceTests;

internal static class NetworkPipelineBuilderExtensions
{
    /// <summary>
    /// Convenience method that materializes a <see cref="NetworkPipeline"/> directly
    /// from a <see cref="NetworkPipelineBuilder"/>.
    ///
    /// Intended for unit tests and low‑level scenarios where going through higher‑level
    /// hosting abstractions would be unnecessary. This method simply collapses
    /// builder → factory → pipeline into a single call.
    /// </summary>
    internal static async Task<NetworkPipeline> CreatePipelineAsync(this NetworkPipelineBuilder builder, CancellationToken cancellationToken = default)
    {
        return await builder.BuildFactory().CreatePipelineAsync(cancellationToken);
    }
}
