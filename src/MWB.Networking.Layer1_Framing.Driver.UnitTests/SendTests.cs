using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer1_Framing.Driver.UnitTests.Helpers;

namespace MWB.Networking.Layer1_Framing.Driver.UnitTests;

// ============================================================
//  Send tests
//
//  Verify the outbound path: NetworkFrame → encoded bytes →
//  ITransportByteSink.Write.
//
//  Tests cover:
//  - Argument validation (null frame).
//  - Guard against sending after shutdown (Dispose, clean close,
//    transport fault).
//  - Correct encoding: bytes written to the transport must
//    exactly match what the pipeline encodes for the same frame.
//  - All frame kinds round-trip through Send without loss.
// ============================================================

[TestClass]
public sealed class SendTests
{
    public TestContext TestContext { get; set; } = null!;

    // ------------------------------------------------------------------
    // Argument validation
    // ------------------------------------------------------------------

    /// <summary>
    /// Passing a null frame to Send must throw ArgumentNullException
    /// immediately, regardless of driver state.
    /// </summary>
    [TestMethod]
    public void Send_NullFrame_ThrowsArgumentNullException()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        Assert.ThrowsExactly<ArgumentNullException>(() => driver.Send(null!));
    }

    // ------------------------------------------------------------------
    // Shutdown guards
    // ------------------------------------------------------------------

    /// <summary>
    /// Send after Dispose() must throw InvalidOperationException.
    /// </summary>
    [TestMethod]
    public void Send_AfterDispose_ThrowsInvalidOperationException()
    {
        var transport = new FakeTransportStack();
        var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        driver.Dispose();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => driver.Send(NetworkFrames.Request(requestId: 1)));
    }

    /// <summary>
    /// Send after a clean close (Read returned 0) must throw
    /// InvalidOperationException — the connection is gone.
    /// </summary>
    [TestMethod]
    public async Task Send_AfterCleanClose_ThrowsInvalidOperationException()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var driverClosed = new TaskCompletionSource();
        driver.Closed += () => driverClosed.TrySetResult();

        driver.Start();
        transport.EnqueueEof();

        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => driver.Send(NetworkFrames.Request(requestId: 1)),
            "Send after clean close must throw.");
    }

    /// <summary>
    /// Send after a transport fault must throw InvalidOperationException.
    /// </summary>
    [TestMethod]
    public async Task Send_AfterTransportFault_ThrowsInvalidOperationException()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var driverFaulted = new TaskCompletionSource<Exception>();
        driver.Faulted += ex => driverFaulted.TrySetResult(ex);

        driver.Start();
        transport.EnqueueException(new IOException("simulated read error"));

        await driverFaulted.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => driver.Send(NetworkFrames.Request(requestId: 1)),
            "Send after transport fault must throw.");
    }

    // ------------------------------------------------------------------
    // Correct encoding
    // ------------------------------------------------------------------

    /// <summary>
    /// Send must write exactly the bytes that the pipeline encodes for the
    /// same frame. The concatenation of all Write() segments must equal the
    /// output of an independent pipeline.Encode() call.
    /// </summary>
    [TestMethod]
    public async Task Send_ValidFrame_WritesExactPipelineEncodingToTransport()
    {
        var pipeline = TestPipeline.CreateLengthPrefixed();
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, pipeline);

        var driverClosed = new TaskCompletionSource();
        driver.Closed += () => driverClosed.TrySetResult();

        driver.Start();

        var frame = NetworkFrames.Request(requestId: 42, payload: new byte[] { 1, 2, 3 });
        driver.Send(frame);

        // Compute the expected wire encoding via an independent pipeline instance
        var expectedBytes = TestPipeline.EncodeToBytes(TestPipeline.CreateLengthPrefixed(), frame);
        var actualBytes = transport.AllWrittenBytes();

        CollectionAssert.AreEqual(expectedBytes, actualBytes,
            "Bytes written to the transport must exactly match the pipeline encoding.");

        transport.EnqueueEof();
        await driverClosed.Task
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
    }

    /// <summary>
    /// Send must work correctly before Start() is called, since the driver
    /// does not restrict outbound sends to the running state.
    /// </summary>
    [TestMethod]
    public void Send_BeforeStart_WritesEncodedBytesToTransport()
    {
        var pipeline = TestPipeline.CreateLengthPrefixed();
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, pipeline);

        var frame = NetworkFrames.Event(eventType: 7);
        driver.Send(frame);

        var expectedBytes = TestPipeline.EncodeToBytes(TestPipeline.CreateLengthPrefixed(), frame);
        CollectionAssert.AreEqual(expectedBytes, transport.AllWrittenBytes(),
            "Send before Start() should still produce correct output.");
    }

    // ------------------------------------------------------------------
    // Frame kinds
    // ------------------------------------------------------------------

    /// <summary>
    /// An Event frame must be encoded and written without loss of fields.
    /// </summary>
    [TestMethod]
    public void Send_EventFrame_EncodesCorrectly()
    {
        AssertSendProducesExpectedEncoding(NetworkFrames.Event(eventType: 100));
    }

    /// <summary>
    /// A Request frame must be encoded and written without loss of fields.
    /// </summary>
    [TestMethod]
    public void Send_RequestFrame_EncodesCorrectly()
    {
        AssertSendProducesExpectedEncoding(
            NetworkFrames.Request(requestId: 99, requestType: 3,
                payload: new byte[] { 0xAA, 0xBB }));
    }

    /// <summary>
    /// A Response frame must be encoded and written without loss of fields.
    /// </summary>
    [TestMethod]
    public void Send_ResponseFrame_EncodesCorrectly()
    {
        AssertSendProducesExpectedEncoding(
            NetworkFrames.Response(requestId: 5, responseType: 2,
                payload: new byte[] { 0xCC }));
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static void AssertSendProducesExpectedEncoding(NetworkFrame frame)
    {
        var pipeline = TestPipeline.CreateLengthPrefixed();
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, pipeline);

        driver.Send(frame);

        var expected = TestPipeline.EncodeToBytes(TestPipeline.CreateLengthPrefixed(), frame);
        CollectionAssert.AreEqual(expected, transport.AllWrittenBytes());
    }
}
