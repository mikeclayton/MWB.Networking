using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.Abstractions;
using MWB.Networking.Layer0_Transport.Pipes;
using System.IO.Pipelines;
using System.IO.Pipes;

namespace KeyboardSharingConsole.Helpers
{
    internal static class NamedPipeHelper
    {
        public static async Task<INetworkConnectionProvider> CreateNamedPipeConnectionProviderAsync(
            ILogger logger, string localPeerName, string remotePeerName, CancellationToken cancellationToken = default)
        {
            // -----------------------------
            // Create named pipe streams
            // -----------------------------

            NamedPipeServerStream outboundPipe;
            NamedPipeClientStream inboundPipe;

            var outboundPipeName = $"MWB.Networking/{localPeerName}-to-{remotePeerName}";
            var inboundPipeName = $"MWB.Networking/{remotePeerName}-to-{localPeerName}";

            Console.WriteLine("Starting outbound pipe stream");
            Console.WriteLine($"    pipe name: {outboundPipeName}");
            outboundPipe = new NamedPipeServerStream(
                pipeName: outboundPipeName,
                PipeDirection.Out,
                1,
                PipeTransmissionMode.Byte,
                System.IO.Pipes.PipeOptions.Asynchronous);

            Console.WriteLine("Starting inbound pipe stream");
            Console.WriteLine($"    pipe name: {inboundPipeName}");
            inboundPipe = new NamedPipeClientStream(
                serverName: ".",
                pipeName: inboundPipeName,
                PipeDirection.In,
                System.IO.Pipes.PipeOptions.Asynchronous);

            // Wait for the pipes to connect
            Console.WriteLine("Waiting for pipe connections…");
            await Task.WhenAll(
                outboundPipe.WaitForConnectionAsync(cancellationToken),
                inboundPipe.ConnectAsync(cancellationToken));

            Console.WriteLine("Pipes connected");

            // -----------------------------
            // Wrap streams as PipeReader / PipeWriter
            // -----------------------------

            var writer =
                PipeWriter.Create(
                    outboundPipe,
                    new StreamPipeWriterOptions(leaveOpen: false));

            var reader =
                PipeReader.Create(
                    inboundPipe,
                    new StreamPipeReaderOptions(leaveOpen: false));

            // -----------------------------
            // Create provider + session
            // -----------------------------

            var provider =
                new PipeNetworkConnectionProvider(
                    logger,
                    reader,
                    writer);

            return provider;
        }
    }
}
