namespace MWB.Networking.Layer0_Transport.Stack.Fsm;

internal enum TransportStackInputKind
{
    // Commands
    ConnectRequested,
    DisconnectRequested,
    DisposeRequested,

    // Provider events
    ProviderConnecting,
    ProviderConnected,
    ProviderDisconnected,
    ProviderFaulted,
}
