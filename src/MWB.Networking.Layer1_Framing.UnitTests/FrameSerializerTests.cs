using MWB.Networking.Layer1_Framing.Frames;
using MWB.Networking.Layer1_Framing.Serialization;

namespace MWB.Networking.Layer1_Framing.UnitTests;

[TestClass]
public sealed class FrameSerializerTests
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

    [TestMethod]
    public void FullyPopulatedFrame_RoundTripsThroughSerializer()
    {
        // Arrange
        var originalFrame = new NetworkFrame(
            kind: NetworkFrameKind.Response, // arbitrary
            eventType: 0x10,
            requestId: 0x20,
            requestType: 0x30,
            responseType: 0x40,
            streamId: 0x50,
            streamType: 0x60,
            payload: new byte[] { 0xAA, 0xBB, 0xCC });

        // Act
        var segments = NetworkFrameSerializer.SerializeFrame(originalFrame);
        var buffer = segments.Collapse();
        var roundtripFrame = NetworkFrameSerializer.DeserializeFrame(buffer);

        // Assert – structural equality only
        Assert.AreEqual(originalFrame.Kind, roundtripFrame.Kind);
        Assert.AreEqual(originalFrame.EventType, roundtripFrame.EventType);
        Assert.AreEqual(originalFrame.RequestId, roundtripFrame.RequestId);
        Assert.AreEqual(originalFrame.RequestType, roundtripFrame.RequestType);
        Assert.AreEqual(originalFrame.ResponseType, roundtripFrame.ResponseType);
        Assert.AreEqual(originalFrame.StreamId, roundtripFrame.StreamId);
        Assert.AreEqual(originalFrame.StreamType, roundtripFrame.StreamType);
        CollectionAssert.AreEqual(
            originalFrame.Payload.ToArray(),
            roundtripFrame.Payload.ToArray());
    }
}
