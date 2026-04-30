namespace MWB.Networking.Layer1_Framing.Codec;

public enum FrameDecodeResult
{
    /// <summary>
    /// A complete frame was successfully decoded from the input.
    /// </summary>
    Success,

    /// <summary>
    /// No frame could be decoded because additional input data is required.
    /// This is not an error; decoding should be retried when more data arrives.
    /// </summary>
    NeedsMoreData,

    /// <summary>
    /// The input data is structurally invalid and cannot represent a valid frame.
    /// This is a fatal error for the current connection.
    /// </summary>
    InvalidFrameEncoding
}
