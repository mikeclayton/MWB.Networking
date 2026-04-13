using System.Diagnostics.CodeAnalysis;

namespace MWB.Networking.Layer2_Protocol;

public sealed partial class ProtocolSession : IProtocolSessionRuntime
{
    void IProtocolSessionRuntime.ProcessFrame(ProtocolFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        switch (frame.Kind)
        {
            // ----- One-way ----------------------------------------------
            case ProtocolFrameKind.Event:
                this.ProcessEventFrame(frame);
                break;

            // ----- Request lifecycle ------------------------------------
            case ProtocolFrameKind.Request:
                // Inbound request from the peer
                this.ProcessInboundRequestFrame(frame);
                break;

            case ProtocolFrameKind.Response:
            case ProtocolFrameKind.Error:
                // Terminal response to a request we sent
                this.ProcessInboundResponseFrame(frame);
                break;

            // ----- Stream lifecycle -------------------------------------
            case ProtocolFrameKind.StreamOpen:
            case ProtocolFrameKind.StreamData:
            case ProtocolFrameKind.StreamClose:
                this.ProcessInboundStreamFrame(frame);
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