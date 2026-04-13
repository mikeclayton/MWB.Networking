namespace MWB.Networking.Layer2_Protocol;

public sealed partial class ProtocolSession : IProtocolSession
{
    // ------------------------------------------------------------------
    // Snapshots
    // ------------------------------------------------------------------

    ProtocolSnapshot IProtocolSession.Snapshot()
    {
        return new ProtocolSnapshot(
            OpenRequests: this.RequestContexts.Keys.ToArray(),
            OpenStreams: this.StreamEntries.Keys.ToArray());
    }
}
