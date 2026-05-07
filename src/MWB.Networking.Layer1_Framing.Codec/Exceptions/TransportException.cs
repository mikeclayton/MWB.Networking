namespace MWB.Networking.Layer1_Framing.Codec.Exceptions;

/// <summary>
/// Represents a fatal violation of the transport-layer protocol contract.
///
/// A <see cref="TransportException"/> indicates that the underlying byte stream
/// has become invalid or unusable, and that no further decoding or recovery is
/// possible on the current connection.
/// </summary>
/// <remarks>
/// Transport exceptions always apply at the scope of an entire transport stream
/// (for example, a TCP connection). Once thrown, the connection must be
/// considered permanently invalid and should be closed.
///
/// This base type exists to allow applications to catch and handle all
/// transport-scope failures uniformly, while still permitting more specific
/// transport exception types (such as decode failures, negotiation failures,
/// or handshake errors) to be introduced in the future.
/// </remarks>
public abstract class TransportException : Exception
{
    protected TransportException(string message)
        : base(message)
    {
    }

    protected TransportException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
