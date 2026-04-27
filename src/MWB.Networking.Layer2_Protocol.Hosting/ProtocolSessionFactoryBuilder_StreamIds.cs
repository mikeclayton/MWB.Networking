using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;

namespace MWB.Networking.Layer2_Protocol.Hosting;

/// <summary>
/// Application-facing builder for protocol sessions.
///
/// This type owns network pipeline wiring and delegates
/// final session creation to the protocol layer.
/// </summary>
public sealed partial class ProtocolSessionFactoryBuilder
{
    private OddEvenStreamIdParity? _streamIdParity;

    public ProtocolSessionFactoryBuilder UseStreamIdParity(
        OddEvenStreamIdParity parity)
    {
        _streamIdParity = parity;
        return this;
    }

    // Optional convenience
    public ProtocolSessionFactoryBuilder UseOddStreamIds()
        => this.UseStreamIdParity(OddEvenStreamIdParity.Odd);

    public ProtocolSessionFactoryBuilder UseEvenStreamIds()
        => this.UseStreamIdParity(OddEvenStreamIdParity.Even);
}
