namespace MWB.Networking.Layer2_Protocol.Session.Api;

internal interface IProtocolSessionDiagnostics
{
    ProtocolSnapshot GetSnapshot();
}
