using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer2_Protocol.Adapter.UnitTests.Fakes;
using MWB.Networking.Layer2_Protocol.Session.Frames;

namespace MWB.Networking.Layer2_Protocol.Adapter.UnitTests;

/// <summary>
/// Tests for the outbound path: <c>IProtocolSessionOutput.OutboundFrameReady</c> →
/// <c>INetworkFrameSink.Send</c>.
///
/// Each outbound <see cref="ProtocolFrame"/> must be converted to an equivalent
/// <see cref="NetworkFrame"/> and sent to the network synchronously and exactly once.
/// All frame fields must be preserved identically. Frames must reach the sink in
/// emission order.
/// </summary>
[TestClass]
public sealed class SessionAdapter_Outbound
{
    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static (FakeProtocolSession session, FakeNetworkIO network, SessionAdapter adapter)
        Build()
    {
        var session = new FakeProtocolSession();
        var network = new FakeNetworkIO();
        var adapter = new SessionAdapter(NullLogger.Instance, session, network);
        return (session, network, adapter);
    }

    private static ProtocolFrame MakeProtocol(
        ProtocolFrameKind kind,
        uint? eventType = null,
        uint? requestId = null,
        uint? requestType = null,
        uint? responseType = null,
        uint? streamId = null,
        uint? streamType = null,
        byte[]? payload = null)
        => ProtocolFrame.CreateRaw(kind, eventType, requestId, requestType,
            responseType, streamId, streamType,
            payload != null ? new ReadOnlyMemory<byte>(payload) : default);

    // -----------------------------------------------------------------------
    // Null frame guard
    // -----------------------------------------------------------------------

    [TestMethod]
    public void OutboundNullFrame_ThrowsArgumentNullException()
    {
        var (session, _, adapter) = Build();
        using (adapter)
        {
            Assert.Throws<ArgumentNullException>(() =>
                session.RaiseOutboundFrameReady(null!));
        }
    }

    // -----------------------------------------------------------------------
    // Delivery guarantee: exactly once, synchronously
    // -----------------------------------------------------------------------

    [TestMethod]
    public void OutboundFrame_IsDeliveredToNetworkExactlyOnce()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            session.RaiseOutboundFrameReady(MakeProtocol(ProtocolFrameKind.Event));

        Assert.AreEqual(1, network.SentFrames.Count);
    }

    [TestMethod]
    public void MultipleOutboundFrames_DeliveredInOrder()
    {
        var (session, network, adapter) = Build();
        using (adapter)
        {
            session.RaiseOutboundFrameReady(MakeProtocol(ProtocolFrameKind.Event, eventType: 1u));
            session.RaiseOutboundFrameReady(MakeProtocol(ProtocolFrameKind.Event, eventType: 2u));
            session.RaiseOutboundFrameReady(MakeProtocol(ProtocolFrameKind.Event, eventType: 3u));
        }

        Assert.AreEqual(3, network.SentFrames.Count);
        Assert.AreEqual(1u, network.SentFrames[0].EventType);
        Assert.AreEqual(2u, network.SentFrames[1].EventType);
        Assert.AreEqual(3u, network.SentFrames[2].EventType);
    }

    // -----------------------------------------------------------------------
    // Kind mapping — every ProtocolFrameKind must map to the correct NetworkFrameKind
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Outbound_EventFrame_MapsToNetworkEvent()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            session.RaiseOutboundFrameReady(MakeProtocol(ProtocolFrameKind.Event));

        Assert.AreEqual(NetworkFrameKind.Event, network.SentFrames[0].Kind);
    }

    [TestMethod]
    public void Outbound_RequestFrame_MapsToNetworkRequest()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            session.RaiseOutboundFrameReady(MakeProtocol(ProtocolFrameKind.Request, requestId: 1u));

        Assert.AreEqual(NetworkFrameKind.Request, network.SentFrames[0].Kind);
    }

    [TestMethod]
    public void Outbound_ResponseFrame_MapsToNetworkResponse()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            session.RaiseOutboundFrameReady(MakeProtocol(ProtocolFrameKind.Response, requestId: 1u));

        Assert.AreEqual(NetworkFrameKind.Response, network.SentFrames[0].Kind);
    }

    [TestMethod]
    public void Outbound_ErrorFrame_MapsToNetworkError()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            session.RaiseOutboundFrameReady(MakeProtocol(ProtocolFrameKind.Error, requestId: 1u));

        Assert.AreEqual(NetworkFrameKind.Error, network.SentFrames[0].Kind);
    }

    [TestMethod]
    public void Outbound_StreamOpenFrame_MapsToNetworkStreamOpen()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            session.RaiseOutboundFrameReady(MakeProtocol(ProtocolFrameKind.StreamOpen, streamId: 1u));

        Assert.AreEqual(NetworkFrameKind.StreamOpen, network.SentFrames[0].Kind);
    }

    [TestMethod]
    public void Outbound_StreamDataFrame_MapsToNetworkStreamData()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            session.RaiseOutboundFrameReady(MakeProtocol(ProtocolFrameKind.StreamData, streamId: 1u));

        Assert.AreEqual(NetworkFrameKind.StreamData, network.SentFrames[0].Kind);
    }

    [TestMethod]
    public void Outbound_StreamCloseFrame_MapsToNetworkStreamClose()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            session.RaiseOutboundFrameReady(MakeProtocol(ProtocolFrameKind.StreamClose, streamId: 1u));

        Assert.AreEqual(NetworkFrameKind.StreamClose, network.SentFrames[0].Kind);
    }

    [TestMethod]
    public void Outbound_StreamAbortFrame_MapsToNetworkStreamAbort()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            session.RaiseOutboundFrameReady(MakeProtocol(ProtocolFrameKind.StreamAbort, streamId: 1u));

        Assert.AreEqual(NetworkFrameKind.StreamAbort, network.SentFrames[0].Kind);
    }

    // -----------------------------------------------------------------------
    // Field preservation — all fields survive the conversion unchanged
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Outbound_EventType_IsPreserved()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            session.RaiseOutboundFrameReady(MakeProtocol(ProtocolFrameKind.Event, eventType: 0xABCDu));

        Assert.AreEqual(0xABCDu, network.SentFrames[0].EventType);
    }

    [TestMethod]
    public void Outbound_NullEventType_IsPreserved()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            session.RaiseOutboundFrameReady(MakeProtocol(ProtocolFrameKind.Event, eventType: null));

        Assert.IsNull(network.SentFrames[0].EventType);
    }

    [TestMethod]
    public void Outbound_RequestId_IsPreserved()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            session.RaiseOutboundFrameReady(MakeProtocol(ProtocolFrameKind.Request, requestId: 42u));

        Assert.AreEqual(42u, network.SentFrames[0].RequestId);
    }

    [TestMethod]
    public void Outbound_RequestType_IsPreserved()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            session.RaiseOutboundFrameReady(
                MakeProtocol(ProtocolFrameKind.Request, requestId: 1u, requestType: 99u));

        Assert.AreEqual(99u, network.SentFrames[0].RequestType);
    }

    [TestMethod]
    public void Outbound_ResponseType_IsPreserved()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            session.RaiseOutboundFrameReady(
                MakeProtocol(ProtocolFrameKind.Response, requestId: 1u, responseType: 77u));

        Assert.AreEqual(77u, network.SentFrames[0].ResponseType);
    }

    [TestMethod]
    public void Outbound_StreamId_IsPreserved()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            session.RaiseOutboundFrameReady(
                MakeProtocol(ProtocolFrameKind.StreamOpen, streamId: 55u));

        Assert.AreEqual(55u, network.SentFrames[0].StreamId);
    }

    [TestMethod]
    public void Outbound_StreamType_IsPreserved()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            session.RaiseOutboundFrameReady(
                MakeProtocol(ProtocolFrameKind.StreamOpen, streamId: 1u, streamType: 33u));

        Assert.AreEqual(33u, network.SentFrames[0].StreamType);
    }

    [TestMethod]
    public void Outbound_Payload_IsPreserved()
    {
        var (session, network, adapter) = Build();
        var payload = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE };
        using (adapter)
            session.RaiseOutboundFrameReady(MakeProtocol(ProtocolFrameKind.Event, payload: payload));

        CollectionAssert.AreEqual(payload, network.SentFrames[0].Payload.ToArray());
    }

    [TestMethod]
    public void Outbound_EmptyPayload_IsPreserved()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            session.RaiseOutboundFrameReady(
                MakeProtocol(ProtocolFrameKind.Event, payload: Array.Empty<byte>()));

        Assert.AreEqual(0, network.SentFrames[0].Payload.Length);
    }

    // -----------------------------------------------------------------------
    // All null optional fields survive as null
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Outbound_AllOptionalFieldsNull_ArePreservedAsNull()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            session.RaiseOutboundFrameReady(MakeProtocol(ProtocolFrameKind.Event));

        var f = network.SentFrames[0];
        Assert.IsNull(f.EventType);
        Assert.IsNull(f.RequestId);
        Assert.IsNull(f.RequestType);
        Assert.IsNull(f.ResponseType);
        Assert.IsNull(f.StreamId);
        Assert.IsNull(f.StreamType);
    }
}
