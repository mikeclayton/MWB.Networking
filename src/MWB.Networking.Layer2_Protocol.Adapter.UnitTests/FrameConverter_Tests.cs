using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer2_Protocol.Session.Frames;

namespace MWB.Networking.Layer2_Protocol.Adapter.UnitTests;

/// <summary>
/// Tests for <see cref="FrameConverter"/>: the internal static class that performs
/// mechanical field-by-field conversion between <see cref="NetworkFrame"/> and
/// <see cref="ProtocolFrame"/>.
///
/// Tests exercise both directions:
///   <see cref="FrameConverter.ToProtocolFrame"/>  (inbound path)
///   <see cref="FrameConverter.ToNetworkFrame"/>   (outbound path)
///
/// Contract under test:
/// - Every <see cref="NetworkFrameKind"/> maps to the numerically-equal
///   <see cref="ProtocolFrameKind"/> and vice-versa.
/// - An unknown kind throws <see cref="InvalidOperationException"/> in both directions.
/// - All seven optional fields (EventType, RequestId, RequestType, ResponseType,
///   StreamId, StreamType, Payload) are transferred without modification.
/// - Null fields remain null; non-null fields retain their exact value.
/// </summary>
[TestClass]
public sealed class FrameConverter_Tests
{
    [TestCleanup]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // -----------------------------------------------------------------------
    // Null guard — ToProtocolFrame
    // -----------------------------------------------------------------------

    [TestMethod]
    public void ToProtocolFrame_NullFrame_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FrameConverter.ToProtocolFrame(null!));
    }

    // -----------------------------------------------------------------------
    // Null guard — ToNetworkFrame
    // -----------------------------------------------------------------------

    [TestMethod]
    public void ToNetworkFrame_NullFrame_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FrameConverter.ToNetworkFrame(null!));
    }

    // -----------------------------------------------------------------------
    // Kind mapping: NetworkFrameKind → ProtocolFrameKind
    // -----------------------------------------------------------------------

    [TestMethod] public void ToProtocolFrame_Event_MapsToEvent()       => AssertToProtocol(NetworkFrameKind.Event,       ProtocolFrameKind.Event);
    [TestMethod] public void ToProtocolFrame_Request_MapsToRequest()   => AssertToProtocol(NetworkFrameKind.Request,     ProtocolFrameKind.Request);
    [TestMethod] public void ToProtocolFrame_Response_MapsToResponse() => AssertToProtocol(NetworkFrameKind.Response,    ProtocolFrameKind.Response);
    [TestMethod] public void ToProtocolFrame_Error_MapsToError()       => AssertToProtocol(NetworkFrameKind.Error,       ProtocolFrameKind.Error);
    [TestMethod] public void ToProtocolFrame_StreamOpen_Maps()         => AssertToProtocol(NetworkFrameKind.StreamOpen,  ProtocolFrameKind.StreamOpen);
    [TestMethod] public void ToProtocolFrame_StreamData_Maps()         => AssertToProtocol(NetworkFrameKind.StreamData,  ProtocolFrameKind.StreamData);
    [TestMethod] public void ToProtocolFrame_StreamClose_Maps()        => AssertToProtocol(NetworkFrameKind.StreamClose, ProtocolFrameKind.StreamClose);
    [TestMethod] public void ToProtocolFrame_StreamAbort_Maps()        => AssertToProtocol(NetworkFrameKind.StreamAbort, ProtocolFrameKind.StreamAbort);

    private static void AssertToProtocol(NetworkFrameKind networkKind, ProtocolFrameKind expectedProtocolKind)
    {
        var input = NetworkFrame.CreateRaw(
            kind: networkKind,
            eventType: null, requestId: null, requestType: null,
            responseType: null, streamId: null, streamType: null,
            payload: default);

        Assert.AreEqual(expectedProtocolKind, FrameConverter.ToProtocolFrame(input).Kind);
    }

    [TestMethod]
    public void ToProtocolFrame_UnknownKind_ThrowsInvalidOperationException()
    {
        var input = NetworkFrame.CreateRaw(
            kind: (NetworkFrameKind)0xFF,
            eventType: null, requestId: null, requestType: null,
            responseType: null, streamId: null, streamType: null,
            payload: default);

        Assert.Throws<InvalidOperationException>(() =>
            FrameConverter.ToProtocolFrame(input));
    }

    // -----------------------------------------------------------------------
    // Kind mapping: ProtocolFrameKind → NetworkFrameKind
    // -----------------------------------------------------------------------

    [TestMethod] public void ToNetworkFrame_Event_MapsToEvent()       => AssertToNetwork(ProtocolFrameKind.Event,       NetworkFrameKind.Event);
    [TestMethod] public void ToNetworkFrame_Request_MapsToRequest()   => AssertToNetwork(ProtocolFrameKind.Request,     NetworkFrameKind.Request);
    [TestMethod] public void ToNetworkFrame_Response_MapsToResponse() => AssertToNetwork(ProtocolFrameKind.Response,    NetworkFrameKind.Response);
    [TestMethod] public void ToNetworkFrame_Error_MapsToError()       => AssertToNetwork(ProtocolFrameKind.Error,       NetworkFrameKind.Error);
    [TestMethod] public void ToNetworkFrame_StreamOpen_Maps()         => AssertToNetwork(ProtocolFrameKind.StreamOpen,  NetworkFrameKind.StreamOpen);
    [TestMethod] public void ToNetworkFrame_StreamData_Maps()         => AssertToNetwork(ProtocolFrameKind.StreamData,  NetworkFrameKind.StreamData);
    [TestMethod] public void ToNetworkFrame_StreamClose_Maps()        => AssertToNetwork(ProtocolFrameKind.StreamClose, NetworkFrameKind.StreamClose);
    [TestMethod] public void ToNetworkFrame_StreamAbort_Maps()        => AssertToNetwork(ProtocolFrameKind.StreamAbort, NetworkFrameKind.StreamAbort);

    private static void AssertToNetwork(ProtocolFrameKind protocolKind, NetworkFrameKind expectedNetworkKind)
    {
        var input = ProtocolFrame.CreateRaw(
            kind: protocolKind,
            eventType: null, requestId: null, requestType: null,
            responseType: null, streamId: null, streamType: null,
            payload: default);

        Assert.AreEqual(expectedNetworkKind, FrameConverter.ToNetworkFrame(input).Kind);
    }

    [TestMethod]
    public void ToNetworkFrame_UnknownKind_ThrowsInvalidOperationException()
    {
        var input = ProtocolFrame.CreateRaw(
            kind: (ProtocolFrameKind)0xFF,
            eventType: null, requestId: null, requestType: null,
            responseType: null, streamId: null, streamType: null,
            payload: default);

        Assert.Throws<InvalidOperationException>(() =>
            FrameConverter.ToNetworkFrame(input));
    }

    // -----------------------------------------------------------------------
    // Kind values are numerically equal across both enums
    // -----------------------------------------------------------------------

    [TestMethod] public void KindValues_Event_NumericallyEqual()       => AssertKindValuesEqual(NetworkFrameKind.Event,       ProtocolFrameKind.Event);
    [TestMethod] public void KindValues_Request_NumericallyEqual()     => AssertKindValuesEqual(NetworkFrameKind.Request,     ProtocolFrameKind.Request);
    [TestMethod] public void KindValues_Response_NumericallyEqual()    => AssertKindValuesEqual(NetworkFrameKind.Response,    ProtocolFrameKind.Response);
    [TestMethod] public void KindValues_Error_NumericallyEqual()       => AssertKindValuesEqual(NetworkFrameKind.Error,       ProtocolFrameKind.Error);
    [TestMethod] public void KindValues_StreamOpen_NumericallyEqual()  => AssertKindValuesEqual(NetworkFrameKind.StreamOpen,  ProtocolFrameKind.StreamOpen);
    [TestMethod] public void KindValues_StreamData_NumericallyEqual()  => AssertKindValuesEqual(NetworkFrameKind.StreamData,  ProtocolFrameKind.StreamData);
    [TestMethod] public void KindValues_StreamClose_NumericallyEqual() => AssertKindValuesEqual(NetworkFrameKind.StreamClose, ProtocolFrameKind.StreamClose);
    [TestMethod] public void KindValues_StreamAbort_NumericallyEqual() => AssertKindValuesEqual(NetworkFrameKind.StreamAbort, ProtocolFrameKind.StreamAbort);

    private static void AssertKindValuesEqual(NetworkFrameKind networkKind, ProtocolFrameKind protocolKind)
    {
        // The enums are declared as : byte with matching values. This test pins
        // the contract so any accidental misalignment produces a failing test.
        Assert.AreEqual((byte)networkKind, (byte)protocolKind,
            $"NetworkFrameKind.{networkKind} and ProtocolFrameKind.{protocolKind} must have the same underlying value.");
    }

    // -----------------------------------------------------------------------
    // Field preservation — ToProtocolFrame
    // -----------------------------------------------------------------------

    [TestMethod]
    public void ToProtocolFrame_EventType_IsPreserved()
    {
        var input = NetworkFrame.CreateRaw(
            NetworkFrameKind.Event, eventType: 123u,
            null, null, null, null, null, default);

        Assert.AreEqual(123u, FrameConverter.ToProtocolFrame(input).EventType);
    }

    [TestMethod]
    public void ToProtocolFrame_NullEventType_IsPreserved()
    {
        var input = NetworkFrame.CreateRaw(
            NetworkFrameKind.Event, eventType: null,
            null, null, null, null, null, default);

        Assert.IsNull(FrameConverter.ToProtocolFrame(input).EventType);
    }

    [TestMethod]
    public void ToProtocolFrame_RequestId_IsPreserved()
    {
        var input = NetworkFrame.CreateRaw(
            NetworkFrameKind.Request, null, requestId: 7u,
            null, null, null, null, default);

        Assert.AreEqual(7u, FrameConverter.ToProtocolFrame(input).RequestId);
    }

    [TestMethod]
    public void ToProtocolFrame_RequestType_IsPreserved()
    {
        var input = NetworkFrame.CreateRaw(
            NetworkFrameKind.Request, null, 1u, requestType: 88u,
            null, null, null, default);

        Assert.AreEqual(88u, FrameConverter.ToProtocolFrame(input).RequestType);
    }

    [TestMethod]
    public void ToProtocolFrame_ResponseType_IsPreserved()
    {
        var input = NetworkFrame.CreateRaw(
            NetworkFrameKind.Response, null, 1u, null, responseType: 44u,
            null, null, default);

        Assert.AreEqual(44u, FrameConverter.ToProtocolFrame(input).ResponseType);
    }

    [TestMethod]
    public void ToProtocolFrame_StreamId_IsPreserved()
    {
        var input = NetworkFrame.CreateRaw(
            NetworkFrameKind.StreamOpen, null, null, null, null,
            streamId: 9u, null, default);

        Assert.AreEqual(9u, FrameConverter.ToProtocolFrame(input).StreamId);
    }

    [TestMethod]
    public void ToProtocolFrame_StreamType_IsPreserved()
    {
        var input = NetworkFrame.CreateRaw(
            NetworkFrameKind.StreamOpen, null, null, null, null,
            streamId: 2u, streamType: 5u, default);

        Assert.AreEqual(5u, FrameConverter.ToProtocolFrame(input).StreamType);
    }

    [TestMethod]
    public void ToProtocolFrame_Payload_IsPreserved()
    {
        var payload = new byte[] { 0x01, 0x02, 0x03 };
        var input = NetworkFrame.CreateRaw(
            NetworkFrameKind.Event, null, null, null, null, null, null,
            new ReadOnlyMemory<byte>(payload));

        CollectionAssert.AreEqual(payload,
            FrameConverter.ToProtocolFrame(input).Payload.ToArray());
    }

    // -----------------------------------------------------------------------
    // Field preservation — ToNetworkFrame
    // -----------------------------------------------------------------------

    [TestMethod]
    public void ToNetworkFrame_EventType_IsPreserved()
    {
        var input = ProtocolFrame.CreateRaw(
            ProtocolFrameKind.Event, eventType: 123u,
            null, null, null, null, null, default);

        Assert.AreEqual(123u, FrameConverter.ToNetworkFrame(input).EventType);
    }

    [TestMethod]
    public void ToNetworkFrame_NullEventType_IsPreserved()
    {
        var input = ProtocolFrame.CreateRaw(
            ProtocolFrameKind.Event, eventType: null,
            null, null, null, null, null, default);

        Assert.IsNull(FrameConverter.ToNetworkFrame(input).EventType);
    }

    [TestMethod]
    public void ToNetworkFrame_RequestId_IsPreserved()
    {
        var input = ProtocolFrame.CreateRaw(
            ProtocolFrameKind.Request, null, requestId: 7u,
            null, null, null, null, default);

        Assert.AreEqual(7u, FrameConverter.ToNetworkFrame(input).RequestId);
    }

    [TestMethod]
    public void ToNetworkFrame_RequestType_IsPreserved()
    {
        var input = ProtocolFrame.CreateRaw(
            ProtocolFrameKind.Request, null, 1u, requestType: 88u,
            null, null, null, default);

        Assert.AreEqual(88u, FrameConverter.ToNetworkFrame(input).RequestType);
    }

    [TestMethod]
    public void ToNetworkFrame_ResponseType_IsPreserved()
    {
        var input = ProtocolFrame.CreateRaw(
            ProtocolFrameKind.Response, null, 1u, null, responseType: 44u,
            null, null, default);

        Assert.AreEqual(44u, FrameConverter.ToNetworkFrame(input).ResponseType);
    }

    [TestMethod]
    public void ToNetworkFrame_StreamId_IsPreserved()
    {
        var input = ProtocolFrame.CreateRaw(
            ProtocolFrameKind.StreamOpen, null, null, null, null,
            streamId: 9u, null, default);

        Assert.AreEqual(9u, FrameConverter.ToNetworkFrame(input).StreamId);
    }

    [TestMethod]
    public void ToNetworkFrame_StreamType_IsPreserved()
    {
        var input = ProtocolFrame.CreateRaw(
            ProtocolFrameKind.StreamOpen, null, null, null, null,
            streamId: 2u, streamType: 5u, default);

        Assert.AreEqual(5u, FrameConverter.ToNetworkFrame(input).StreamType);
    }

    [TestMethod]
    public void ToNetworkFrame_Payload_IsPreserved()
    {
        var payload = new byte[] { 0x01, 0x02, 0x03 };
        var input = ProtocolFrame.CreateRaw(
            ProtocolFrameKind.Event, null, null, null, null, null, null,
            new ReadOnlyMemory<byte>(payload));

        CollectionAssert.AreEqual(payload,
            FrameConverter.ToNetworkFrame(input).Payload.ToArray());
    }

    // -----------------------------------------------------------------------
    // Round-trip: Network → Protocol → Network field identity
    // -----------------------------------------------------------------------

    [TestMethod]
    public void RoundTrip_NetworkToProtocolToNetwork_AllFieldsIdentical()
    {
        var payload = new byte[] { 0xAA, 0xBB };
        var original = NetworkFrame.CreateRaw(
            kind: NetworkFrameKind.Request,
            eventType: null,
            requestId: 99u,
            requestType: 7u,
            responseType: null,
            streamId: null,
            streamType: null,
            payload: new ReadOnlyMemory<byte>(payload));

        var protocol = FrameConverter.ToProtocolFrame(original);
        var roundTripped = FrameConverter.ToNetworkFrame(protocol);

        Assert.AreEqual(original.Kind,         roundTripped.Kind);
        Assert.AreEqual(original.EventType,    roundTripped.EventType);
        Assert.AreEqual(original.RequestId,    roundTripped.RequestId);
        Assert.AreEqual(original.RequestType,  roundTripped.RequestType);
        Assert.AreEqual(original.ResponseType, roundTripped.ResponseType);
        Assert.AreEqual(original.StreamId,     roundTripped.StreamId);
        Assert.AreEqual(original.StreamType,   roundTripped.StreamType);
        CollectionAssert.AreEqual(
            original.Payload.ToArray(),
            roundTripped.Payload.ToArray());
    }

    // -----------------------------------------------------------------------
    // Round-trip: Protocol → Network → Protocol field identity
    // -----------------------------------------------------------------------

    [TestMethod]
    public void RoundTrip_ProtocolToNetworkToProtocol_AllFieldsIdentical()
    {
        var payload = new byte[] { 0x11, 0x22, 0x33 };
        var original = ProtocolFrame.CreateRaw(
            kind: ProtocolFrameKind.StreamOpen,
            eventType: null,
            requestId: null,
            requestType: null,
            responseType: null,
            streamId: 42u,
            streamType: 3u,
            payload: new ReadOnlyMemory<byte>(payload));

        var network = FrameConverter.ToNetworkFrame(original);
        var roundTripped = FrameConverter.ToProtocolFrame(network);

        Assert.AreEqual(original.Kind,         roundTripped.Kind);
        Assert.AreEqual(original.EventType,    roundTripped.EventType);
        Assert.AreEqual(original.RequestId,    roundTripped.RequestId);
        Assert.AreEqual(original.RequestType,  roundTripped.RequestType);
        Assert.AreEqual(original.ResponseType, roundTripped.ResponseType);
        Assert.AreEqual(original.StreamId,     roundTripped.StreamId);
        Assert.AreEqual(original.StreamType,   roundTripped.StreamType);
        CollectionAssert.AreEqual(
            original.Payload.ToArray(),
            roundTripped.Payload.ToArray());
    }
}
