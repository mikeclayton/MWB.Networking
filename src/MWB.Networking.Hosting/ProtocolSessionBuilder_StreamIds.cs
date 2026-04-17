using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;

namespace MWB.Networking.Hosting;

/// <summary>
/// Application-facing builder for protocol sessions.
///
/// This type owns network pipeline wiring and delegates
/// final session creation to the protocol layer.
/// </summary>
public sealed partial class ProtocolSessionBuilder
{
    private OddEvenStreamIdParity? _streamIdParity;

    public ProtocolSessionBuilder UseStreamIdParity(
        OddEvenStreamIdParity parity)
    {
        _streamIdParity = parity;
        return this;
    }

    // Optional convenience
    public ProtocolSessionBuilder UseOddStreamIds()
        => this.UseStreamIdParity(OddEvenStreamIdParity.Odd);

    public ProtocolSessionBuilder UseEvenStreamIds()
        => this.UseStreamIdParity(OddEvenStreamIdParity.Even);
}
