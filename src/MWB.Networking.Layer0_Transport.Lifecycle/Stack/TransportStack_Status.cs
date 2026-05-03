namespace MWB.Networking.Layer0_Transport.Lifecycle.Stack;

public sealed partial class TransportStack
{
    // -----------------------------
    // Public state
    // -----------------------------

    /// <summary>
    /// Gets the current connection status.
    /// </summary>
    private ObservableConnectionStatus? Status
    {
        get;
        set;
    }

    public TransportConnectionState? State
        => this.Status?.State;

    /// <summary>
    /// True if the stack is currently connected.
    /// </summary>
    public bool IsConnected
        => this.State == TransportConnectionState.Connected;
}
