using System.CommandLine;

namespace KeyboardSharingConsole.CommandLine;

internal static class CommandLineParser
{
    public static CommandLineOptions Parse(string[] args)
    {
        var localPeerNameOption = new Option<string>(name: "--local-peer-name")
        {
            Description = "Name of the local peer",
            Required = true
        };

        var remotePeerNameOption = new Option<string>(name: "--remote-peer-name")
        {
            Description = "Name of the remote peer",
            Required = true
        };

        var listenPortOption = new Option<int>(name: "--listen-port")
        {
            Description = "Local port to listen on",
            Required = true
        };

        var connectPortOption = new Option<int>(name: "--connect-port")
        {
            Description = "Peer port to connect to",
            Required = true
        };

        var rootCommand = new RootCommand("Keyboard sharing peer");
        rootCommand.Options.Add(localPeerNameOption);
        rootCommand.Options.Add(remotePeerNameOption);
        rootCommand.Options.Add(listenPortOption);
        rootCommand.Options.Add(connectPortOption);

        var parseResult = rootCommand.Parse(args);
        if (parseResult.Errors.Count > 0)
        {
            throw new InvalidOperationException();
        }

        var options = new CommandLineOptions(
            parseResult.GetValue<string>(localPeerNameOption) ?? throw new InvalidOperationException(),
            parseResult.GetValue<string>(remotePeerNameOption) ?? throw new InvalidOperationException(),
            parseResult.GetValue(listenPortOption),
            parseResult.GetValue(connectPortOption)
        );

        return options;
    }
}

