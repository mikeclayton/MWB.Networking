using MouseWithoutBorders.Networking.PeerTransport.Layer2_Protocol;
using MWB.Networking.Layer1_Framing;
using MWB.Networking.Layer2_Protocol;

namespace MWB.Networking.Layer3_Runtime;

internal static class FrameConverter
{
    private static ProtocolFrameKind ToProtocolFrameKind(NetworkFrameKind kind)
    {
        var protocolFrameKind = kind switch {
            NetworkFrameKind.Event => ProtocolFrameKind.Event,
            NetworkFrameKind.Request => ProtocolFrameKind.Request,
            NetworkFrameKind.Response => ProtocolFrameKind.Response,
            NetworkFrameKind.Complete => ProtocolFrameKind.Complete,
            NetworkFrameKind.Cancel => ProtocolFrameKind.Cancel,
            NetworkFrameKind.StreamOpen => ProtocolFrameKind.StreamOpen,
            NetworkFrameKind.StreamData => ProtocolFrameKind.StreamData,
            NetworkFrameKind.StreamClose => ProtocolFrameKind.StreamOpen,
            NetworkFrameKind.StreamReset => ProtocolFrameKind.StreamReset,
            NetworkFrameKind.Error => ProtocolFrameKind.Error,
            _ =>
                throw new InvalidOperationException()
        };
        return protocolFrameKind;
    }

    private static NetworkFrameKind ToNetworkFrameKind(ProtocolFrameKind kind)
    {
        var protocolFrameKind = kind switch
        {
            ProtocolFrameKind.Event => NetworkFrameKind.Event,
            ProtocolFrameKind.Request => NetworkFrameKind.Request,
            ProtocolFrameKind.Response => NetworkFrameKind.Response,
            ProtocolFrameKind.Complete => NetworkFrameKind.Complete,
            ProtocolFrameKind.Cancel => NetworkFrameKind.Cancel,
            ProtocolFrameKind.StreamOpen => NetworkFrameKind.StreamOpen,
            ProtocolFrameKind.StreamData => NetworkFrameKind.StreamData,
            ProtocolFrameKind.StreamClose => NetworkFrameKind.StreamOpen,
            ProtocolFrameKind.StreamReset => NetworkFrameKind.StreamReset,
            ProtocolFrameKind.Error => NetworkFrameKind.Error,
            _ =>
                throw new InvalidOperationException()
        };
        return protocolFrameKind;
    }

    internal static ProtocolFrame ToProtocolFrame(NetworkFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        return new ProtocolFrame(
            kind: FrameConverter.ToProtocolFrameKind(frame.Kind),
            eventType: frame.EventType,
            requestId: frame.RequestId,
            streamId: frame.StreamId,
            payload: frame.Payload);
    }

    internal static NetworkFrame ToNetworkFrame(ProtocolFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        return new NetworkFrame(
            kind: FrameConverter.ToNetworkFrameKind(frame.Kind),
            eventType: frame.EventType,
            requestId: frame.RequestId,
            streamId: frame.StreamId,
            payload: frame.Payload);
    }
}
