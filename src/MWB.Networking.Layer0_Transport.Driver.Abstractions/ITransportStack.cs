namespace MWB.Networking.Layer0_Transport.Driver.Abstractions;

public interface ITransportStack :
    ITransportByteSource,
    ITransportByteSink,
    ITransportEvents;
