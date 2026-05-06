namespace MWB.Networking.Layer2_Protocol.Session.Streams.Lifecycle;

internal enum IncomingStreamState
{
    /// <summary>
    /// Open
    /// </summary>
    Open,

    /// <summary>
    /// Closed cleanly by *remote* peer
    /// </summary>
    Closed,

    /// <summary>
    /// Aborted with an error by *local* peer
    /// </summary>
    Aborted
}
