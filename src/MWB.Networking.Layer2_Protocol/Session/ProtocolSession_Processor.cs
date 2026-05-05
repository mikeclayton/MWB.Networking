using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Session.Api;

namespace MWB.Networking.Layer2_Protocol.Session;

public sealed partial class ProtocolSession : IProtocolSessionProcessor
{
    internal IProtocolSessionProcessor AsProcessor()
        => this;

    void IProtocolSessionProcessor.ProcessFrame(ProtocolFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        switch (frame.Kind)
        {
            // ----- One-way ----------------------------------------------
            case ProtocolFrameKind.Event:
                this.EventManager.ProcessInboundEventFrame(frame);
                break;

            // ----- Request lifecycle ------------------------------------
            case ProtocolFrameKind.Request:
                // Inbound request from the peer
                this.RequestManager.Inbound.ProcessInboundRequestFrame(frame);
                break;

            case ProtocolFrameKind.Response:
            case ProtocolFrameKind.Error:
                // Terminal response to a request we sent
                this.RequestManager.Inbound.ProcessInboundResponseFrame(frame);
                break;

            // ----- Stream lifecycle -------------------------------------
            case ProtocolFrameKind.StreamOpen:
            case ProtocolFrameKind.StreamData:
            case ProtocolFrameKind.StreamClose:
            case ProtocolFrameKind.StreamAbort:
                this.StreamManager.Inbound.ProcessStreamFrame(frame);
                break;

            default:
                throw new ProtocolException(
                    ProtocolErrorKind.UnknownFrameKind,
                    $"Unknown frame kind: {frame.Kind}");
        }
    }
}
