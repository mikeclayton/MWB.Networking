namespace MWB.Networking.Layer2_Protocol.Streams.Lifecycle;

internal enum OutgoingStreamState
{
    /// <summary>
    /// Open
    /// </summary>
    Open,

    /// <summary>
    /// Closed cleanly by *local* peer
    /// </summary>
    Closed,

    /// <summary>
    /// Aborted with an error by *local* peer
    /// </summary>
    Aborted
}
