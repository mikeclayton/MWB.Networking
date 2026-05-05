namespace MWB.Networking.Layer0_Transport.Lifecycle.Stack;

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
