using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;

namespace MWB.Networking.Layer3_Hosting.Configuration;

/// <summary>
/// Application-facing builder for protocol sessions.
///
/// This type owns network pipeline wiring and delegates
/// final session creation to the protocol layer.
/// </summary>
public sealed partial class SessionHostBuilder
{
    private OddEvenStreamIdParity? _streamIdParity;

    public SessionHostBuilder UseStreamIdParity(
        OddEvenStreamIdParity parity)
    {
        _streamIdParity = parity;
        return this;
    }

    // Optional convenience
    public SessionHostBuilder UseOddStreamIds()
        => this.UseStreamIdParity(OddEvenStreamIdParity.Odd);

    public SessionHostBuilder UseEvenStreamIds()
        => this.UseStreamIdParity(OddEvenStreamIdParity.Even);
}
