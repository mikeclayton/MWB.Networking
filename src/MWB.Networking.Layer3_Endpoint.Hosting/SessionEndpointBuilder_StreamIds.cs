using MWB.Networking.Layer2_Protocol.Lifecycle.Infrastructure;

namespace MWB.Networking.Layer3_Endpoint.Hosting;

/// <summary>
/// Application-facing builder for protocol sessions.
///
/// This type owns network pipeline wiring and delegates
/// final session creation to the protocol layer.
/// </summary>
public sealed partial class SessionEndpointBuilder
{
    private OddEvenStreamIdParity? _streamIdParity;

    public SessionEndpointBuilder UseStreamIdParity(
        OddEvenStreamIdParity parity)
    {
        _streamIdParity = parity;
        return this;
    }

    // Optional convenience
    public SessionEndpointBuilder UseOddStreamIds()
        => this.UseStreamIdParity(OddEvenStreamIdParity.Odd);

    public SessionEndpointBuilder UseEvenStreamIds()
        => this.UseStreamIdParity(OddEvenStreamIdParity.Even);
}
