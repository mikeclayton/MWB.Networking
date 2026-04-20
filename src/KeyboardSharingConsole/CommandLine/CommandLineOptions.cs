namespace KeyboardSharingConsole.CommandLine;

internal sealed class CommandLineOptions
{
    public CommandLineOptions(string localPeerName, string remotePeerName, int listenPort, int connectPort)
    {
        this.LocalPeerName = localPeerName ?? throw new ArgumentNullException(localPeerName);
        this.RemotePeerName = remotePeerName ?? throw new ArgumentNullException(remotePeerName);
        this.ListenPort = listenPort;
        this.ConnectPort = connectPort;
    }

    public string LocalPeerName
    {
        get;
    }

    public string RemotePeerName
    {
        get;
    }

    public int ListenPort
    {
        get;
    }

    public int ConnectPort
    {
        get;
    }
}
