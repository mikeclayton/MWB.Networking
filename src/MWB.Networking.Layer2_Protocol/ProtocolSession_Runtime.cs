using MWB.Networking.Layer2_Protocol.Internal;
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
                this.ProcessNewRequestFrame(frame);
                break;


            case ProtocolFrameKind.Response:
                // responses must be sent by the local side in reaction
                // to requests from the remote side, so receiving a
                // response is always a protocol violation
                throw new ProtocolException(
                    ProtocolErrorKind.InvalidFrameSequence,
                    "Inbound Response frames are not allowed.");

            case ProtocolFrameKind.Complete:
            case ProtocolFrameKind.Cancel:
            case ProtocolFrameKind.Error:
                this.ProcessRequestFrame(frame);
                break;

            // ----- Stream lifecycle -------------------------------------
            case ProtocolFrameKind.StreamOpen:
            case ProtocolFrameKind.StreamData:
            case ProtocolFrameKind.StreamClose:
                this.ProcessStreamFrame(frame);
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
