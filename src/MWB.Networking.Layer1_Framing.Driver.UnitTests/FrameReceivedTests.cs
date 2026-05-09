using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer1_Framing.Driver.UnitTests.Helpers;
using MWB.Networking.Layer1_Framing.Pipeline;

namespace MWB.Networking.Layer1_Framing.Driver.UnitTests;

// ============================================================
//  FrameReceived tests
//
//  Verify the inbound decode path: raw transport bytes →
//  decoded NetworkFrame → FrameReceived event.
//
//  Tests cover:
//  - A single complete frame delivered in one Read() call.
//  - A frame that arrives split across two or more Read()s
//    (the decode-buffer accumulation path).
//  - Multiple frames in consecutive reads arrive in order.
//  - Payload bytes are preserved through the round-trip.
//  - All supported frame kinds decode correctly.
// ============================================================

[TestClass]
public sealed class FrameReceivedTests
{
    public TestContext TestContext { get; set; } = null!;

    // ------------------------------------------------------------------
    // Single complete frame
    // ------------------------------------------------------------------

    /// <summary>
    /// When a single, complete frame arrives in one Read() call,
    /// FrameReceived must fire exactly once with a correctly decoded frame.
    /// </summary>
    [TestMethod]
    public async Task FrameReceived_SingleCompleteRead_FiresOnce()
    {
        var (driver, transport, pipeline) = CreateDriver();

        var received = new List<NetworkFrame>();
        var driverClosed = new TaskCompletionSource();
        driver.FrameReceived += f => received.Add(f);
        driver.Closed += () => driverClosed.TrySetResult();

        driver.Start();

        var frame = NetworkFrames.Request(requestId: 1);
        transport.EnqueueBytes(TestPipeline.EncodeToBytes(pipeline, frame));
        transport.EnqueueEof();

        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        driver.Dispose();

        Assert.HasCount(1, received, "Exactly one frame should have been raised.");
        TestPipeline.AssertFramesEqual(frame, received[0]);
    }

    // ------------------------------------------------------------------
    // Frame split across reads (decode buffer accumulation)
    // ------------------------------------------------------------------

    /// <summary>
    /// When the wire bytes for a single frame arrive in two separate Read()
    /// calls — simulating a TCP segment boundary — FrameReceived must fire
    /// exactly once, only after the second (completing) read.
    ///
    /// This exercises the decode-buffer accumulation logic.
    /// </summary>
    [TestMethod]
    public async Task FrameReceived_FrameSplitAcrossTwoReads_FiresOnce()
    {
        var (driver, transport, pipeline) = CreateDriver();

        var received = new List<NetworkFrame>();
        var driverClosed = new TaskCompletionSource();
        driver.FrameReceived += f => received.Add(f);
        driver.Closed += () => driverClosed.TrySetResult();

        driver.Start();

        var frame = NetworkFrames.Request(requestId: 7, payload: new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
        var wireBytes = TestPipeline.EncodeToBytes(pipeline, frame);

        // Split the wire bytes in half across two Read() calls
        var splitAt = wireBytes.Length / 2;
        transport.EnqueueBytes(wireBytes[..splitAt]);
        transport.EnqueueBytes(wireBytes[splitAt..]);
        transport.EnqueueEof();

        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        driver.Dispose();

        Assert.HasCount(1, received,
            "Exactly one frame should be raised even when data arrives in two chunks.");
        TestPipeline.AssertFramesEqual(frame, received[0]);
    }

    /// <summary>
    /// When the wire bytes arrive one byte at a time, the driver must
    /// accumulate them and fire FrameReceived exactly once.
    ///
    /// This exercises the accumulation loop under maximum fragmentation.
    /// </summary>
    [TestMethod]
    public async Task FrameReceived_FrameDeliveredOneByteAtATime_FiresOnce()
    {
        var (driver, transport, pipeline) = CreateDriver();

        var received = new List<NetworkFrame>();
        var driverClosed = new TaskCompletionSource();
        driver.FrameReceived += f => received.Add(f);
        driver.Closed += () => driverClosed.TrySetResult();

        driver.Start();

        var frame = NetworkFrames.Response(requestId: 3, responseType: 1,
            payload: new byte[] { 0xAA, 0xBB });
        var wireBytes = TestPipeline.EncodeToBytes(pipeline, frame);

        foreach (var b in wireBytes)
        {
            transport.EnqueueBytes([b]);
        }
        transport.EnqueueEof();

        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        driver.Dispose();

        Assert.HasCount(1, received,
            "Exactly one frame should be raised even when bytes arrive one at a time.");
        TestPipeline.AssertFramesEqual(frame, received[0]);
    }

    // ------------------------------------------------------------------
    // Multiple frames
    // ------------------------------------------------------------------

    /// <summary>
    /// When multiple frames are delivered in consecutive reads,
    /// FrameReceived must fire once per frame in arrival order.
    /// </summary>
    [TestMethod]
    public async Task FrameReceived_MultipleFramesInOrder_FiresInArrivalOrder()
    {
        var (driver, transport, pipeline) = CreateDriver();

        var received = new List<NetworkFrame>();
        var driverClosed = new TaskCompletionSource();
        driver.FrameReceived += f => received.Add(f);
        driver.Closed += () => driverClosed.TrySetResult();

        driver.Start();

        var frame1 = NetworkFrames.Request(requestId: 1);
        var frame2 = NetworkFrames.Request(requestId: 2);
        var frame3 = NetworkFrames.Response(requestId: 1, responseType: 0);

        transport.EnqueueBytes(TestPipeline.EncodeToBytes(pipeline, frame1));
        transport.EnqueueBytes(TestPipeline.EncodeToBytes(pipeline, frame2));
        transport.EnqueueBytes(TestPipeline.EncodeToBytes(pipeline, frame3));
        transport.EnqueueEof();

        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        driver.Dispose();

        Assert.HasCount(3, received, "Three frames should have been raised.");
        TestPipeline.AssertFramesEqual(frame1, received[0]);
        TestPipeline.AssertFramesEqual(frame2, received[1]);
        TestPipeline.AssertFramesEqual(frame3, received[2]);
    }

    /// <summary>
    /// When two frames arrive concatenated in a single Read() call,
    /// both must be decoded and raised in order.
    ///
    /// This exercises the DrainDecodeBuffer loop that continues decoding
    /// after a successful frame until no more complete frames remain.
    /// </summary>
    [TestMethod]
    public async Task FrameReceived_TwoFramesConcatenatedInOneRead_BothRaisedInOrder()
    {
        var (driver, transport, pipeline) = CreateDriver();

        var received = new List<NetworkFrame>();
        var driverClosed = new TaskCompletionSource();
        driver.FrameReceived += f => received.Add(f);
        driver.Closed += () => driverClosed.TrySetResult();

        driver.Start();

        var frame1 = NetworkFrames.Event(eventType: 10);
        var frame2 = NetworkFrames.Event(eventType: 20);

        // Deliver both frames in a single Read() call
        var combined = TestPipeline.EncodeToBytes(pipeline, frame1)
            .Concat(TestPipeline.EncodeToBytes(pipeline, frame2))
            .ToArray();

        transport.EnqueueBytes(combined);
        transport.EnqueueEof();

        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        driver.Dispose();

        Assert.HasCount(2, received,
            "Two frames concatenated in one read should produce two FrameReceived events.");
        TestPipeline.AssertFramesEqual(frame1, received[0]);
        TestPipeline.AssertFramesEqual(frame2, received[1]);
    }

    // ------------------------------------------------------------------
    // Payload fidelity
    // ------------------------------------------------------------------

    /// <summary>
    /// A frame with a non-trivial payload must arrive with its payload bytes
    /// unchanged.
    /// </summary>
    [TestMethod]
    public async Task FrameReceived_PayloadIsPreservedExactly()
    {
        var (driver, transport, pipeline) = CreateDriver();

        var received = new List<NetworkFrame>();
        var driverClosed = new TaskCompletionSource();
        driver.FrameReceived += f => received.Add(f);
        driver.Closed += () => driverClosed.TrySetResult();

        driver.Start();

        var payload = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        var frame = NetworkFrames.Request(requestId: 99, payload: payload);

        transport.EnqueueBytes(TestPipeline.EncodeToBytes(pipeline, frame));
        transport.EnqueueEof();

        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        driver.Dispose();

        Assert.HasCount(1, received);
        CollectionAssert.AreEqual(payload, received[0].Payload.ToArray(),
            "Payload bytes must be identical after the encode→transport→decode round-trip.");
    }

    /// <summary>
    /// A frame with an empty payload must arrive with an empty payload —
    /// not null or any default bytes.
    /// </summary>
    [TestMethod]
    public async Task FrameReceived_EmptyPayload_PreservedAsEmpty()
    {
        var (driver, transport, pipeline) = CreateDriver();

        var received = new List<NetworkFrame>();
        var driverClosed = new TaskCompletionSource();
        driver.FrameReceived += f => received.Add(f);
        driver.Closed += () => driverClosed.TrySetResult();

        driver.Start();

        var frame = NetworkFrames.Request(requestId: 1);    // no payload

        transport.EnqueueBytes(TestPipeline.EncodeToBytes(pipeline, frame));
        transport.EnqueueEof();

        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        driver.Dispose();

        Assert.HasCount(1, received);
        Assert.AreEqual(0, received[0].Payload.Length,
            "A frame encoded with no payload should arrive with an empty payload.");
    }

    // ------------------------------------------------------------------
    // Frame kinds
    // ------------------------------------------------------------------

    /// <summary>
    /// An Event frame must round-trip through the driver with all fields intact.
    /// </summary>
    [TestMethod]
    public async Task FrameReceived_EventFrame_FieldsPreserved()
    {
        await AssertRoundTrip(NetworkFrames.Event(eventType: 42));
    }

    /// <summary>
    /// A Request frame must round-trip through the driver with all fields intact.
    /// </summary>
    [TestMethod]
    public async Task FrameReceived_RequestFrame_FieldsPreserved()
    {
        await AssertRoundTrip(
            NetworkFrames.Request(requestId: 7, requestType: 3,
                payload: new byte[] { 1, 2 }));
    }

    /// <summary>
    /// A Response frame must round-trip through the driver with all fields intact.
    /// </summary>
    [TestMethod]
    public async Task FrameReceived_ResponseFrame_FieldsPreserved()
    {
        await AssertRoundTrip(
            NetworkFrames.Response(requestId: 7, responseType: 5,
                payload: new byte[] { 0xFF }));
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private (TransportDriver driver, FakeTransportStack transport, NetworkPipeline pipeline) CreateDriver()
    {
        var pipeline = TestPipeline.CreateLengthPrefixed();
        var transport = new FakeTransportStack();
        var driver = new TransportDriver(transport, pipeline);
        return (driver, transport, pipeline);
    }

    private async Task AssertRoundTrip(NetworkFrame frame)
    {
        var (driver, transport, pipeline) = CreateDriver();

        var received = new List<NetworkFrame>();
        var driverClosed = new TaskCompletionSource();
        driver.FrameReceived += f => received.Add(f);
        driver.Closed += () => driverClosed.TrySetResult();

        driver.Start();

        transport.EnqueueBytes(TestPipeline.EncodeToBytes(pipeline, frame));
        transport.EnqueueEof();

        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        driver.Dispose();

        Assert.HasCount(1, received);
        TestPipeline.AssertFramesEqual(frame, received[0]);
    }
}
