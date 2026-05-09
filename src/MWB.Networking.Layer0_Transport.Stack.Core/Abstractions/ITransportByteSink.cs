namespace MWB.Networking.Layer0_Transport.Stack.Core.Abstractions;

public interface ITransportByteSink
{
    /// <summary>
    /// Writes bytes to the transport.
    ///
    /// MUST block or throw if the transport cannot
    /// currently accept the data.
    /// </summary>
    void Write(ReadOnlySpan<byte> bytes);
}
