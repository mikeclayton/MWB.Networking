using System.CommandLine;

namespace KeyboardSharingConsole.CommandLine;

internal static class CommandLineParser
{
    public static CommandLineOptions Parse(string[] args)
    {
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
        rootCommand.Options.Add(listenPortOption);
        rootCommand.Options.Add(connectPortOption);

        var parseResult = rootCommand.Parse(args);
        if (parseResult.Errors.Count > 0)
        {
            throw new InvalidOperationException();
        }

        var options = new CommandLineOptions(
            parseResult.GetValue(listenPortOption),
            parseResult.GetValue(connectPortOption)
        );

        return options;
    }
}

