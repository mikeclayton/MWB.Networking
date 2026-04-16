### Authentication

Add an authentication handshake at connection.

Maybe make this pluggable, but for Mouse Without Borders we
need a way for each party to prove they hold the shared secret without
revealing the secret.

### Example implementation

This is an implementation from a previous iteration. Might not be wuitable
for the current codebase, but should give a flavour of how to do it:

```
using MouseWithoutBorders.Networking.Helpers;
using System.Net.Security;
using System.Security.Cryptography;
using System.Text;

namespace MouseWithoutBorders.Networking.Cryptography.TlsChannel;

/// <summary>
/// Provides helper methods for performing an additional shared‑secret
/// validation handshake over an already‑established TLS connection.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TlsHandshakeHelper"/> implements a lightweight, symmetric
/// challenge‑response protocol layered on top of TLS to prove that both
/// peers possess the same shared secret.
/// </para>
/// <para>
/// This handshake is intended to supplement TLS transport security by
/// adding an application‑level authentication step, ensuring that the
/// remote endpoint is not only TLS‑authenticated, but also authorised
/// to participate in the peer‑to‑peer protocol.
/// </para>
/// <para>
/// The protocol uses HMAC‑SHA256 with random nonces to prevent replay
/// attacks and to ensure mutual proof of knowledge of the shared secret
/// without transmitting the secret itself.
/// </para>
/// </remarks>
internal static class TlsHandshakeHelper
{
    /// <summary>
    /// Performs the client‑side portion of the shared‑secret validation
    /// handshake over an established TLS stream.
    /// </summary>
    /// <param name="ssl">
    /// The authenticated <see cref="SslStream"/> used to exchange handshake
    /// messages. The TLS handshake must already be complete.
    /// </param>
    /// <param name="sharedSecret">
    /// The pre‑shared secret used to derive the HMAC key for validation.
    /// </param>
    /// <param name="token">
    /// A cancellation token used to abort the handshake.
    /// </param>
    /// <remarks>
    /// <para>
    /// The client initiates the handshake by generating a random nonce
    /// (<c>nonceA</c>) and sending it to the server. The server must prove
    /// possession of the shared secret by returning a valid HMAC of that
    /// nonce, along with its own challenge nonce (<c>nonceB</c>).
    /// </para>
    /// <para>
    /// The client then performs the reciprocal proof by computing and
    /// returning an HMAC of <c>nonceB</c>.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the server fails to produce a valid HMAC, indicating that
    /// it does not possess the shared secret.
    /// </exception>
    internal static async Task PerformClientHandshakeAsync(SslStream ssl, string sharedSecret, CancellationToken token = default)
    {
        // derive HMAC key
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(sharedSecret));
        using var hmac = new HMACSHA256(key);

        // generate nonceA
        var nonceA = RandomNumberGenerator.GetBytes(32);

        // send nonceA
        await ssl.WriteAsync(nonceA, token);
        await ssl.FlushAsync(token);

        // read 32-byte HMAC(nonceA) + 32-byte nonceB
        var buffer = new byte[64];
        await StreamHelper.ReadExactlyAsync(ssl, buffer, token);

        // validate HMAC of nonceA
        var hmacA = buffer[..32];
        var expectedA = hmac.ComputeHash(nonceA);
        if (!expectedA.AsSpan().SequenceEqual(hmacA))
        {
            throw new InvalidOperationException("shared-secret validation failed (server did not prove secret).");
        }

        // send HMAC(nonceB)
        var nonceB = buffer[32..];
        var hmacB = hmac.ComputeHash(nonceB);
        await ssl.WriteAsync(hmacB, token);
        await ssl.FlushAsync(token);
    }

    /// <summary>
    /// Performs the server‑side portion of the shared‑secret validation
    /// handshake over an established TLS stream.
    /// </summary>
    /// <param name="ssl">
    /// The authenticated <see cref="SslStream"/> used to exchange handshake
    /// messages. The TLS handshake must already be complete.
    /// </param>
    /// <param name="sharedSecret">
    /// The pre‑shared secret used to derive the HMAC key for validation.
    /// </param>
    /// <param name="token">
    /// A cancellation token used to abort the handshake.
    /// </param>
    /// <remarks>
    /// <para>
    /// The server receives an initial nonce from the client and responds
    /// with an HMAC proving knowledge of the shared secret, along with its
    /// own randomly generated challenge nonce.
    /// </para>
    /// <para>
    /// The handshake completes only after the client successfully proves
    /// possession of the shared secret by returning a valid HMAC of the
    /// server’s nonce.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the client fails to produce a valid HMAC, indicating that
    /// it does not possess the shared secret.
    /// </exception>
    internal static async Task PerformServerHandshakeAsync(SslStream ssl, string sharedSecret, CancellationToken token = default)
    {
        // derive HMAC key
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(sharedSecret));
        using var hmac = new HMACSHA256(key);

        // receive 32-byte nonceA from client
        var nonceA = new byte[32];
        await StreamHelper.ReadExactlyAsync(ssl, nonceA, token);

        // compute HMAC(nonceA) and generate nonceB
        var hmacA = hmac.ComputeHash(nonceA);
        var nonceB = RandomNumberGenerator.GetBytes(32);

        // send hmacA + nonceB
        var response = new byte[64];
        Buffer.BlockCopy(hmacA, 0, response, 0, 32);
        Buffer.BlockCopy(nonceB, 0, response, 32, 32);
        await ssl.WriteAsync(response, token);
        await ssl.FlushAsync(token);

        // receive 32-byte HMAC(nonceB) from client
        var hmacBClient = new byte[32];
        await StreamHelper.ReadExactlyAsync(ssl, hmacBClient, token);

        // validate
        var hmacBExpected = hmac.ComputeHash(nonceB);
        if (!hmacBExpected.AsSpan().SequenceEqual(hmacBClient))
        {
            throw new InvalidOperationException("shared-secret validation failed (client did not prove secret).");
        }
    }
}
```