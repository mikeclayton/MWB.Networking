namespace KeyboardSharingConsole.CommandLine;

internal sealed class CommandLineOptions
{
    public CommandLineOptions(int listenPort, int connectPort)
    {
        this.ListenPort = listenPort;
        this.ConnectPort = connectPort;
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
