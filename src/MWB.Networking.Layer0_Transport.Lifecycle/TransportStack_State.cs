namespace MWB.Networking.Layer0_Transport.Lifecycle;

public sealed partial class TransportStack
{
    private enum StackState
    {
        Idle,           // Never connected, or fully reset
        Connecting,     // ConnectAsync in progress
        Connected,      // Logical connection established
        Disconnecting,  // Local or remote teardown in progress
        Terminated      // Final terminal state (EOF / disposed)
    }

    private StackState _state = StackState.Idle;

    private bool _hasEverConnected;
}
