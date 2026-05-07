namespace MWB.Networking.Layer0_Transport.Stack.Fsm;

internal enum TransportStackState
{
    Idle,           // Never connected, or fully reset
    Connecting,     // ConnectAsync in progress
    Connected,      // Logical connection established
    Disconnecting,  // Local or remote teardown in progress
    Terminated      // Final terminal state (EOF / disposed)
}
