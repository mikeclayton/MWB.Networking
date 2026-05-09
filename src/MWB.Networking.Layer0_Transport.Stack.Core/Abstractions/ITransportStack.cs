namespace MWB.Networking.Layer0_Transport.Stack.Core.Abstractions;

public interface ITransportStack :
    ITransportByteSource,
    ITransportByteSink,
    ITransportEvents;
