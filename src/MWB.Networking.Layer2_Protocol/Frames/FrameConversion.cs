using MWB.Networking.Layer1_Framing.Codec.Frames;

namespace MWB.Networking.Layer2_Protocol.Frames;

internal static class FrameConverter
{
    private static ProtocolFrameKind ToProtocolFrameKind(NetworkFrameKind kind)
    {
        var protocolFrameKind = kind switch {
            NetworkFrameKind.Event => ProtocolFrameKind.Event,
            NetworkFrameKind.Request => ProtocolFrameKind.Request,
            NetworkFrameKind.Response => ProtocolFrameKind.Response,
            NetworkFrameKind.Error => ProtocolFrameKind.Error,
            NetworkFrameKind.StreamOpen => ProtocolFrameKind.StreamOpen,
            NetworkFrameKind.StreamData => ProtocolFrameKind.StreamData,
            NetworkFrameKind.StreamClose => ProtocolFrameKind.StreamClose,
            NetworkFrameKind.StreamAbort => ProtocolFrameKind.StreamAbort,
            _ => throw new InvalidOperationException()
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
            ProtocolFrameKind.Error => NetworkFrameKind.Error,
            ProtocolFrameKind.StreamOpen => NetworkFrameKind.StreamOpen,
            ProtocolFrameKind.StreamData => NetworkFrameKind.StreamData,
            ProtocolFrameKind.StreamClose => NetworkFrameKind.StreamClose,
            ProtocolFrameKind.StreamAbort => NetworkFrameKind.StreamAbort,
            _ => throw new InvalidOperationException()
        };
        return protocolFrameKind;
    }

    internal static ProtocolFrame ToProtocolFrame(NetworkFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        return ProtocolFrame.CreateRaw(
            kind: FrameConverter.ToProtocolFrameKind(frame.Kind),
            eventType: frame.EventType,
            requestId: frame.RequestId,
            requestType: frame.RequestType,
            responseType: frame.ResponseType,
            streamId: frame.StreamId,
            streamType: frame.StreamType,
            payload: frame.Payload);
    }

    internal static NetworkFrame ToNetworkFrame(ProtocolFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        return NetworkFrame.CreateRaw(
            kind: FrameConverter.ToNetworkFrameKind(frame.Kind),
            eventType: frame.EventType,
            requestId: frame.RequestId,
            requestType: frame.RequestType,
            responseType: frame.ResponseType,
            streamId: frame.StreamId,
            streamType: frame.StreamType,
            payload: frame.Payload);
    }
}
