namespace MWB.Networking.Layer3_Endpoint.Hosting;

/// <summary>
/// Application-facing builder for protocol sessions.
///
/// This type owns network pipeline wiring and delegates
/// final session creation to the protocol layer.
/// </summary>
public sealed partial class SessionEndpointBuilder
{
    //private readonly ProtocolDriverOptions _driverOptions = new();

    //public ProtocolSessionBuilder ConfigureDriverOptions(
    //    Action<ProtocolDriverOptions> configure)
    //{
    //    ArgumentNullException.ThrowIfNull(configure);
    //    this.EnsureNotBuilt();

    //    configure(_driverOptions);

    //    return this;
    //}
}
