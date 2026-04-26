namespace MWB.Networking.Layer3_Hosting.Configuration;

/// <summary>
/// Application-facing builder for protocol sessions.
///
/// This type owns network pipeline wiring and delegates
/// final session creation to the protocol layer.
/// </summary>
public sealed partial class SessionHostBuilder
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
