using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport.Stack.Core.Connection;
using MWB.Networking.Layer0_Transport.Stack.Core.Lifecycle;
using MWB.Networking.Layer0_Transport.Tcp.Arbitration;
using MWB.Networking.Logging;
using System.Net.Sockets;

namespace MWB.Networking.Layer0_Transport.Tcp;

/// <summary>
/// Long-lived provider capable of creating multiple independent
/// TCP connection attempts over time.
/// </summary>
public sealed class TcpNetworkConnectionProvider
    : INetworkConnectionProvider
{
    private readonly ILogger _logger;
    private readonly TcpNetworkConnectionConfig _config;
    private readonly ITcpConnectionArbitrator _arbitrator;

    public TcpNetworkConnectionProvider(
        ILogger logger,
        TcpNetworkConnectionConfig config,
        ConnectionDirection preferredArbitrationDirection)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _arbitrator =
            new PreferredDirectionArbitrator(
                preferredArbitrationDirection);
    }

    /// <summary>
    /// Creates a single TCP connection attempt.
    /// May be called multiple times over the provider's lifetime.
    /// </summary>
    public async Task<INetworkConnection> OpenConnectionAsync(
        IConnectionStatus status,
        CancellationToken ct)
    {
        using var scope = _logger.BeginMethodLoggingScope(this);
        ArgumentNullException.ThrowIfNull(status);

        TcpListener? listener = null;
        var candidates =
            new List<(TcpClient Client, ConnectionDirection Direction)>(2);

        try
        {
            // ------------------------------------------------------------
            // Inbound candidate
            // ------------------------------------------------------------
            if (_config.LocalEndpoint is not null)
            {
                listener = new TcpListener(_config.LocalEndpoint);
                listener.Start();

                _logger.LogDebug(
                    "Listening on {Endpoint}",
                    _config.LocalEndpoint);

                var inboundClient =
                    await listener.AcceptTcpClientAsync(ct)
                        .ConfigureAwait(false);

                inboundClient.NoDelay = _config.NoDelay;

                candidates.Add(
                    (inboundClient, ConnectionDirection.Inbound));

                _logger.LogDebug("Accepted inbound TCP connection");
            }

            // ------------------------------------------------------------
            // Outbound candidate
            // ------------------------------------------------------------
            if (_config.RemoteEndpoint is not null)
            {
                var outboundClient = new TcpClient
                {
                    NoDelay = _config.NoDelay
                };

                _logger.LogDebug(
                    "Connecting to {Endpoint}",
                    _config.RemoteEndpoint);

                await outboundClient.ConnectAsync(
                    _config.RemoteEndpoint.Address,
                    _config.RemoteEndpoint.Port,
                    ct).ConfigureAwait(false);

                candidates.Add(
                    (outboundClient, ConnectionDirection.Outbound));

                _logger.LogDebug("Outbound TCP connection established");
            }

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    "No local or remote endpoint configured.");
            }

            // ------------------------------------------------------------
            // Arbitration
            // ------------------------------------------------------------
            var winner = candidates[0];

            for (int i = 1; i < candidates.Count; i++)
            {
                var challenger = candidates[i];

                if (_arbitrator.ShouldReplace(
                        winner.Direction,
                        challenger.Direction))
                {
                    _logger.LogDebug(
                        "Replacing {Old} with {New}",
                        winner.Direction,
                        challenger.Direction);

                    winner.Client.Dispose();
                    winner = challenger;
                }
                else
                {
                    challenger.Client.Dispose();
                }
            }

            // ------------------------------------------------------------
            // Wrap as INetworkConnection
            // ------------------------------------------------------------
            var connection =
                new TcpNetworkConnection(
                    winner.Client,
                    _config.MaxFrameSize);

            // ------------------------------------------------------------
            // Bind lifecycle and start
            // ------------------------------------------------------------
            connection.BindStatus(status);
            connection.OnStarted();

            return connection;
        }
        catch
        {
            foreach (var (client, _) in candidates)
            {
                client.Dispose();
            }
            throw;
        }
        finally
        {
            listener?.Stop();
        }
    }

    public void Dispose()
    {
        // Intentionally empty.
        // The provider is stateless between calls.
    }
}