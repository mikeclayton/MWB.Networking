using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.Session.Api;
using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol.Session;

internal sealed partial class ProtocolSession : IProtocolSessionRuntime
{
    private IProtocolSessionRuntime AsRuntime()
        => this;

    void IProtocolSessionRuntime.ProcessFrame(ProtocolFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        switch (frame.Kind)
        {
            // ----- One-way ----------------------------------------------
            case ProtocolFrameKind.Event:
                this.EventManager.ProcessEventFrame(frame);
                break;

            // ----- Request lifecycle ------------------------------------
            case ProtocolFrameKind.Request:
                // Inbound request from the peer
                this.RequestManager.ProcessInboundRequestFrame(frame);
                break;

            case ProtocolFrameKind.Response:
            case ProtocolFrameKind.Error:
                // Terminal response to a request we sent
                this.RequestManager.ProcessInboundResponseFrame(frame);
                break;

            // ----- Stream lifecycle -------------------------------------
            case ProtocolFrameKind.StreamOpen:
            case ProtocolFrameKind.StreamData:
            case ProtocolFrameKind.StreamClose:
                this.StreamManager.ProcessInboundStreamFrame(frame);
                break;

            default:
                throw new ProtocolException(
                    ProtocolErrorKind.UnknownFrameKind,
                    $"Unknown frame kind: {frame.Kind}");
        }
    }

    bool IProtocolSessionRuntime.TryDequeueOutboundFrame([NotNullWhen(true)] out ProtocolFrame frame)
    {
        if (this.OutboundFrames.TryDequeue(out var result))
        {
            frame = result;
            return true;
        }

        frame = default!;
        return false;
    }
}
