namespace MWB.Networking.Layer0_Transport.Driver.Abstractions;

public interface ITransportStackEvents
{
    /// <summary>
    /// Indicates the underlying transport has closed normally.
    /// </summary>
    void OnTransportClosed();

    /// <summary>
    /// Indicates the underlying transport has faulted.
    /// </summary>
    void OnTransportFaulted(Exception error);
}
