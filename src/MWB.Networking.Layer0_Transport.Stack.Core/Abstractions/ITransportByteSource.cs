namespace MWB.Networking.Layer0_Transport.Stack.Core.Abstractions;

public interface ITransportByteSource
{
    /// <summary>
    /// Reads bytes from the transport into <paramref name="buffer"/>.
    ///
    /// Returns the number of bytes read.
    /// Returns 0 to indicate clean EOF.
    /// Throws on transport fault.
    /// </summary>
    int Read(Span<byte> buffer);
}
