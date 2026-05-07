using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer2_Protocol.Adapter.UnitTests.Fakes;
using MWB.Networking.Layer2_Protocol.Session.Frames;

namespace MWB.Networking.Layer2_Protocol.Adapter.UnitTests;

/// <summary>
/// Tests for the inbound path: <c>INetworkFrameSource.FrameReceived</c> →
/// <c>IProtocolSessionInput.OnFrameReceived</c>.
///
/// Each inbound <see cref="NetworkFrame"/> must be converted to an equivalent
/// <see cref="ProtocolFrame"/> and delivered to the session synchronously and
/// exactly once. All frame fields must be preserved identically. Frames must be
/// delivered in arrival order.
/// </summary>
[TestClass]
public sealed class SessionAdapter_Inbound
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

    // -----------------------------------------------------------------------
    // Null frame guard
    // -----------------------------------------------------------------------

    [TestMethod]
    public void InboundNullFrame_ThrowsArgumentNullException()
    {
        var (_, network, adapter) = Build();
        using (adapter)
        {
            Assert.Throws<ArgumentNullException>(() =>
                network.RaiseFrameReceived(null!));
        }
    }

    // -----------------------------------------------------------------------
    // Delivery guarantee: exactly once, synchronously
    // -----------------------------------------------------------------------

    [TestMethod]
    public void InboundFrame_IsDeliveredToSessionExactlyOnce()
    {
        var (session, network, adapter) = Build();
        using (adapter)
        {
            network.RaiseFrameReceived(NetworkFrameFactory.Event());
        }

        Assert.AreEqual(1, session.ReceivedFrames.Count);
    }

    [TestMethod]
    public void MultipleInboundFrames_DeliveredInOrder()
    {
        var (session, network, adapter) = Build();
        using (adapter)
        {
            network.RaiseFrameReceived(NetworkFrameFactory.Event(eventType: 1u));
            network.RaiseFrameReceived(NetworkFrameFactory.Event(eventType: 2u));
            network.RaiseFrameReceived(NetworkFrameFactory.Event(eventType: 3u));
        }

        Assert.AreEqual(3, session.ReceivedFrames.Count);
        Assert.AreEqual(1u, session.ReceivedFrames[0].EventType);
        Assert.AreEqual(2u, session.ReceivedFrames[1].EventType);
        Assert.AreEqual(3u, session.ReceivedFrames[2].EventType);
    }

    // -----------------------------------------------------------------------
    // Kind mapping — every NetworkFrameKind must map to the correct ProtocolFrameKind
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Inbound_EventFrame_MapsToProtocolEvent()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            network.RaiseFrameReceived(NetworkFrameFactory.Event());

        Assert.AreEqual(ProtocolFrameKind.Event, session.ReceivedFrames[0].Kind);
    }

    [TestMethod]
    public void Inbound_RequestFrame_MapsToProtocolRequest()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            network.RaiseFrameReceived(NetworkFrameFactory.Request(requestId: 1u));

        Assert.AreEqual(ProtocolFrameKind.Request, session.ReceivedFrames[0].Kind);
    }

    [TestMethod]
    public void Inbound_ResponseFrame_MapsToProtocolResponse()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            network.RaiseFrameReceived(NetworkFrameFactory.Response(requestId: 1u));

        Assert.AreEqual(ProtocolFrameKind.Response, session.ReceivedFrames[0].Kind);
    }

    [TestMethod]
    public void Inbound_ErrorFrame_MapsToProtocolError()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            network.RaiseFrameReceived(NetworkFrameFactory.Error(requestId: 1u));

        Assert.AreEqual(ProtocolFrameKind.Error, session.ReceivedFrames[0].Kind);
    }

    [TestMethod]
    public void Inbound_StreamOpenFrame_MapsToProtocolStreamOpen()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            network.RaiseFrameReceived(NetworkFrameFactory.StreamOpen(streamId: 2u));

        Assert.AreEqual(ProtocolFrameKind.StreamOpen, session.ReceivedFrames[0].Kind);
    }

    [TestMethod]
    public void Inbound_StreamDataFrame_MapsToProtocolStreamData()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            network.RaiseFrameReceived(NetworkFrameFactory.StreamData(streamId: 2u));

        Assert.AreEqual(ProtocolFrameKind.StreamData, session.ReceivedFrames[0].Kind);
    }

    [TestMethod]
    public void Inbound_StreamCloseFrame_MapsToProtocolStreamClose()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            network.RaiseFrameReceived(NetworkFrameFactory.StreamClose(streamId: 2u));

        Assert.AreEqual(ProtocolFrameKind.StreamClose, session.ReceivedFrames[0].Kind);
    }

    [TestMethod]
    public void Inbound_StreamAbortFrame_MapsToProtocolStreamAbort()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            network.RaiseFrameReceived(NetworkFrameFactory.StreamAbort(streamId: 2u));

        Assert.AreEqual(ProtocolFrameKind.StreamAbort, session.ReceivedFrames[0].Kind);
    }

    // -----------------------------------------------------------------------
    // Field preservation — all wire fields survive the conversion unchanged
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Inbound_EventType_IsPreserved()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            network.RaiseFrameReceived(NetworkFrameFactory.Event(eventType: 0xABCDu));

        Assert.AreEqual(0xABCDu, session.ReceivedFrames[0].EventType);
    }

    [TestMethod]
    public void Inbound_NullEventType_IsPreserved()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            network.RaiseFrameReceived(NetworkFrameFactory.Event(eventType: null));

        Assert.IsNull(session.ReceivedFrames[0].EventType);
    }

    [TestMethod]
    public void Inbound_RequestId_IsPreserved()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            network.RaiseFrameReceived(NetworkFrameFactory.Request(requestId: 42u));

        Assert.AreEqual(42u, session.ReceivedFrames[0].RequestId);
    }

    [TestMethod]
    public void Inbound_RequestType_IsPreserved()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            network.RaiseFrameReceived(NetworkFrameFactory.Request(requestId: 1u, requestType: 99u));

        Assert.AreEqual(99u, session.ReceivedFrames[0].RequestType);
    }

    [TestMethod]
    public void Inbound_ResponseType_IsPreserved()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            network.RaiseFrameReceived(NetworkFrameFactory.Response(requestId: 1u, responseType: 77u));

        Assert.AreEqual(77u, session.ReceivedFrames[0].ResponseType);
    }

    [TestMethod]
    public void Inbound_StreamId_IsPreserved()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            network.RaiseFrameReceived(NetworkFrameFactory.StreamOpen(streamId: 55u));

        Assert.AreEqual(55u, session.ReceivedFrames[0].StreamId);
    }

    [TestMethod]
    public void Inbound_StreamType_IsPreserved()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            network.RaiseFrameReceived(NetworkFrameFactory.StreamOpen(streamId: 2u, streamType: 33u));

        Assert.AreEqual(33u, session.ReceivedFrames[0].StreamType);
    }

    [TestMethod]
    public void Inbound_Payload_IsPreserved()
    {
        var (session, network, adapter) = Build();
        var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        using (adapter)
            network.RaiseFrameReceived(NetworkFrameFactory.Event(payload: payload));

        CollectionAssert.AreEqual(payload, session.ReceivedFrames[0].Payload.ToArray());
    }

    [TestMethod]
    public void Inbound_EmptyPayload_IsPreserved()
    {
        var (session, network, adapter) = Build();
        using (adapter)
            network.RaiseFrameReceived(NetworkFrameFactory.Event(payload: Array.Empty<byte>()));

        Assert.AreEqual(0, session.ReceivedFrames[0].Payload.Length);
    }

    // -----------------------------------------------------------------------
    // All null optional fields survive as null
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Inbound_AllOptionalFieldsNull_ArePreservedAsNull()
    {
        // A raw Event frame with every optional field left null.
        var (session, network, adapter) = Build();
        using (adapter)
        {
            network.RaiseFrameReceived(NetworkFrame.CreateRaw(
                kind: NetworkFrameKind.Event,
                eventType: null,
                requestId: null,
                requestType: null,
                responseType: null,
                streamId: null,
                streamType: null,
                payload: default));
        }

        var f = session.ReceivedFrames[0];
        Assert.IsNull(f.EventType);
        Assert.IsNull(f.RequestId);
        Assert.IsNull(f.RequestType);
        Assert.IsNull(f.ResponseType);
        Assert.IsNull(f.StreamId);
        Assert.IsNull(f.StreamType);
    }
}
