using MWB.Networking.Layer1_Framing.Frames;
using MWB.Networking.Layer2_Protocol.Frames;

namespace _ProtocolFrame;

[TestClass]
public sealed class ProtocolFrameTests
{
    [TestCleanup]
    public void Cleanup()
    {
        // force any unobserved exceptions from finalizers to surface during
        // test runs rather than being silently ignored - this makes it easier
        // to determine *which* test caused the issue (and fix it!).
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// Verifies a strict 1:1 alignment between <see cref="NetworkFrameKind"/> and
    /// <see cref="ProtocolFrameKind"/>, ensuring that both enums define the same
    /// set of frame kinds with identical underlying values.
    /// </summary>
    /// <remarks>
    /// These enums represent the same conceptual frame types at different layers:
    /// <see cref="NetworkFrameKind"/> at the wire/framing layer, and
    /// <see cref="ProtocolFrameKind"/> at the protocol/semantic layer.
    /// This test exists to prevent accidental drift between the two representations.
    /// </remarks>
    [TestMethod]
    public void Network_and_Protocol_FrameKinds_Must_Be_Name_and_Value_Equivalent()
    {
        var network = Enum.GetValues(typeof(NetworkFrameKind))
            .Cast<NetworkFrameKind>()
            .ToDictionary(
                k => k.ToString(),
                k => Convert.ToInt32(k));

        var protocol = Enum.GetValues(typeof(ProtocolFrameKind))
            .Cast<ProtocolFrameKind>()
            .ToDictionary(
                k => k.ToString(),
                k => Convert.ToInt32(k));

        // Same set of names
        CollectionAssert.AreEquivalent(
            network.Keys.ToArray(),
            protocol.Keys.ToArray(),
            "NetworkFrameKind and ProtocolFrameKind must define the same names.");

        // Same numeric value per name
        foreach (var name in network.Keys)
        {
            Assert.AreEqual(
                network[name],
                protocol[name],
                $"FrameKind '{name}' has mismatched numeric values.");
        }
    }
}
