namespace MWB.Networking.Layer1_Framing.Codec.Exceptions;

/// <summary>
/// Represents a fatal transport-layer decoding failure.
///
/// A <see cref="TransportDecodeException"/> is thrown when a transport codec
/// encounters a sequence of bytes that provably violates the transport framing
/// rules, such that no future input could allow the stream to be decoded
/// correctly.
/// </summary>
/// <remarks>
/// This exception is used to signal unrecoverable transport framing errors,
/// such as invalid length prefixes, impossible delimiter sequences, or other
/// violations that destroy the ability to locate frame boundaries.
///
/// This exception must only be thrown when the transport codec can conclusively
/// determine that the byte stream is invalid. It must not be used for cases
/// where additional bytes may allow decoding to proceed.
///
/// For example, a negative value in a LengthPrefixed frame:
///
///   [ length: -2 ][ payload: ... ]
///
/// or an invalid fixed-length aes-encrypted metadata header frame
///
///   [ invalid fixed-length aes-encrypted metadata header frame ] [ aes-encrypted payload frame ... ]
///
/// These would be grounds for throwing this exception, while an incomplete frame
/// that is waiting for more data from the transport would not be.
///
/// When a <see cref="TransportDecodeException"/> is thrown, the current
/// connection must be terminated immediately.
/// </remarks>
public sealed class TransportDecodeException : TransportException
{
    public TransportDecodeException(string message)
        : base(message)
    {
    }
}
