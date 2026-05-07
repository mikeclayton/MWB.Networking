using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Adapter.UnitTests.Fakes;
using MWB.Networking.Layer2_Protocol.Session.Frames;

namespace MWB.Networking.Layer2_Protocol.Adapter.UnitTests;

/// <summary>
/// Tests for <see cref="SessionAdapter"/> construction, event wiring, and disposal:
///
/// - Constructor null-guards reject missing dependencies.
/// - On construction the adapter subscribes to both the session's outbound event and
///   the network source's inbound event.
/// - On disposal both subscriptions are removed; no further frames are forwarded.
/// - A second call to Dispose is safe (idempotent).
/// </summary>
[TestClass]
public sealed class SessionAdapter_Wiring
{
    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // -----------------------------------------------------------------------
    // Constructor null-guards
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var session = new FakeProtocolSession();
        var network = new FakeNetworkIO();

        Assert.Throws<ArgumentNullException>(() =>
            new SessionAdapter(null!, session, network));
    }

    [TestMethod]
    public void Constructor_NullSession_ThrowsArgumentNullException()
    {
        var network = new FakeNetworkIO();

        Assert.Throws<ArgumentNullException>(() =>
            new SessionAdapter(NullLogger.Instance, null!, network));
    }

    [TestMethod]
    public void Constructor_NullNetwork_ThrowsArgumentNullException()
    {
        var session = new FakeProtocolSession();

        Assert.Throws<ArgumentNullException>(() =>
            new SessionAdapter(NullLogger.Instance, session, null!));
    }

    // -----------------------------------------------------------------------
    // Subscription established at construction
    // -----------------------------------------------------------------------

    [TestMethod]
    public void AfterConstruction_InboundNetworkFrame_IsForwardedToSession()
    {
        // Verifies that the adapter subscribes to INetworkFrameSource.FrameReceived
        // during construction, without needing to call any additional Start() method.
        var session = new FakeProtocolSession();
        var network = new FakeNetworkIO();

        using var _ = new SessionAdapter(NullLogger.Instance, session, network);

        var frame = NetworkFrameFactory.Event();
        network.RaiseFrameReceived(frame);

        Assert.AreEqual(1, session.ReceivedFrames.Count);
    }

    [TestMethod]
    public void AfterConstruction_OutboundProtocolFrame_IsForwardedToNetwork()
    {
        // Verifies that the adapter subscribes to IProtocolSessionOutput.OutboundFrameReady
        // during construction.
        var session = new FakeProtocolSession();
        var network = new FakeNetworkIO();

        using var _ = new SessionAdapter(NullLogger.Instance, session, network);

        var frame = ProtocolFrame.CreateRaw(
            ProtocolFrameKind.Event,
            eventType: null, requestId: null, requestType: null,
            responseType: null, streamId: null, streamType: null,
            payload: default);

        session.RaiseOutboundFrameReady(frame);

        Assert.AreEqual(1, network.SentFrames.Count);
    }

    // -----------------------------------------------------------------------
    // Subscriptions removed on Dispose
    // -----------------------------------------------------------------------

    [TestMethod]
    public void AfterDispose_InboundNetworkFrame_IsNotForwardedToSession()
    {
        var session = new FakeProtocolSession();
        var network = new FakeNetworkIO();

        var adapter = new SessionAdapter(NullLogger.Instance, session, network);
        adapter.Dispose();

        network.RaiseFrameReceived(NetworkFrameFactory.Event());

        Assert.AreEqual(0, session.ReceivedFrames.Count);
    }

    [TestMethod]
    public void AfterDispose_OutboundProtocolFrame_IsNotForwardedToNetwork()
    {
        var session = new FakeProtocolSession();
        var network = new FakeNetworkIO();

        var adapter = new SessionAdapter(NullLogger.Instance, session, network);
        adapter.Dispose();

        var frame = ProtocolFrame.CreateRaw(
            ProtocolFrameKind.Event,
            eventType: null, requestId: null, requestType: null,
            responseType: null, streamId: null, streamType: null,
            payload: default);

        session.RaiseOutboundFrameReady(frame);

        Assert.AreEqual(0, network.SentFrames.Count);
    }

    // -----------------------------------------------------------------------
    // Double-Dispose safety
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var session = new FakeProtocolSession();
        var network = new FakeNetworkIO();

        var adapter = new SessionAdapter(NullLogger.Instance, session, network);
        adapter.Dispose();

        // Must not throw.
        adapter.Dispose();
    }
}
