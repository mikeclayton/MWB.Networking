using MWB.Networking.Layer2_Protocol.Internal;

namespace MWB.Networking.Layer2_Protocol.Requests;

public sealed class RequestContext
{
    private enum State
    {
        Open,
        Completed,
        Cancelled,
        Failed
    }

    private State _state = State.Open;
    private readonly uint _requestId;

    public RequestContext(uint requestId)
    {
        _requestId = requestId;
    }

    internal void ProcessFrame(
        ProtocolFrame frame,
        Action<ProtocolFrame> emit,
        Action<uint> onTerminal)
    {
        switch (frame.Kind)
        {
            case ProtocolFrameKind.Response:
                Ensure(State.Open);
                emit(frame);
                break;

            case ProtocolFrameKind.Complete:
                Ensure(State.Open);
                _state = State.Completed;
                emit(frame);
                onTerminal(_requestId);
                break;

            case ProtocolFrameKind.Cancel:
                _state = State.Cancelled;
                emit(frame);
                onTerminal(_requestId);
                break;

            case ProtocolFrameKind.Error:
                _state = State.Failed;
                emit(frame);
                onTerminal(_requestId);
                break;

            default:
                throw new ProtocolException(
                    ProtocolErrorKind.InvalidFrameSequence,
                    $"Invalid frame for request: {frame.Kind}");
        }
    }

    private void Ensure(State expected)
    {
        if (_state != expected)
            throw new ProtocolException(
                ProtocolErrorKind.InvalidFrameSequence,
                $"Request {_requestId} is {_state}");
    }
}
