using MWB.Networking.Layer2_Protocol.Streams.Infrastructure;

namespace MWB.Networking.Layer2_Protocol.Hosting;

/// <summary>
/// Application-facing builder for protocol sessions.
///
/// This type owns network pipeline wiring and delegates
/// final session creation to the protocol layer.
/// </summary>
public sealed partial class ProtocolRuntimeFactoryBuilder
{
    private OddEvenStreamIdParity? _streamIdParity;

    public ProtocolRuntimeFactoryBuilder UseStreamIdParity(
        OddEvenStreamIdParity parity)
    {
        _streamIdParity = parity;
        return this;
    }

    // Optional convenience
    public ProtocolRuntimeFactoryBuilder UseOddStreamIds()
        => this.UseStreamIdParity(OddEvenStreamIdParity.Odd);

    public ProtocolRuntimeFactoryBuilder UseEvenStreamIds()
        => this.UseStreamIdParity(OddEvenStreamIdParity.Even);
}
