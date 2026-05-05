namespace MWB.Networking.Layer2_Protocol.Frames;

#if ENABLE_PROTOCOL_FRAME_DIAGNOSTICS
/// <summary>
/// Optional frame-level diagnostics data.
/// Used for tracing, performance measurement, and debugging only.
/// Compiled in only when ENABLE_DIAGNOSTICS is defined.
/// </summary>
public struct FrameDiagnostics
{
    /// <summary>
    /// Timestamp recorded when the protocol session sends (emits) this frame.
    /// This represents a semantic protocol event, not transport transmission.
    /// </summary>
    public long SentTimestamp;

    /// <summary>
    /// Timestamp recorded when the protocol session receives (accepts) this frame.
    /// This represents a semantic protocol event, not network ingress or byte-level IO.
    /// </summary>
    public long ReceivedTimestamp;
}
#endif
