using System.Net;
using System.Net.Sockets;

namespace MWB.Networking.Layer0_Transport.Tcp.UnitTests.Helpers;

internal sealed class TestTcpServer : IDisposable
{
    public TestTcpServer(IPAddress address)
    {
        this.Listener = new TcpListener(address, 0);
    }

    private TcpListener Listener
    {
        get;
    }

    public int Port
    {
        get;
        private set;
    }

    private TaskCompletionSource<NetworkStream> ClientConnected
    {
        get;
    } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public NetworkStream ClientStream
    {
        get
        {
            return this.ClientConnected.Task.IsCompleted
                ? this.ClientConnected.Task.Result
                : throw new InvalidOperationException("Client not connected yet.");
        }
    }

    public void Start()
    {
        this.Listener.Start();
        this.Port = ((IPEndPoint)this.Listener.LocalEndpoint).Port;
        _ = AcceptAsync();
    }

    public Task<NetworkStream> WaitForClientAsync()
    {
        return this.ClientConnected.Task;
    }

    private async Task AcceptAsync()
    {
        try
        {
            var client = await this.Listener.AcceptTcpClientAsync();
            this.ClientConnected.TrySetResult(client.GetStream());
        }
        catch (ObjectDisposedException)
        {
            // Listener stopped – ignore
        }
    }

    public void Dispose()
    {
        if (this.ClientConnected.Task.IsCompleted)
        {
            this.ClientConnected.Task.Result.Dispose();
        }
        this.Listener.Stop();
    }
}
