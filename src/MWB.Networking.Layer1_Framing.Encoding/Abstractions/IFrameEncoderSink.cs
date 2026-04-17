using MWB.Networking.Layer0_Transport.Encoding;

namespace MWB.Networking.Layer1_Framing.Encoding.Abstractions;

/// <summary>
/// Receives encoded frames for transmission downstream.
/// This is the terminal point of the frame encoding pipeline.
/// </summary>
public interface IFrameEncoderSink
{
    /// <summary>
    /// Receives a fully encoded logical frame.
    /// The provided segments represent exactly one complete frame.
    /// Partial frames must never be delivered.
    /// </summary>
    ValueTask OnFrameEncodedAsync(
        ByteSegments encodedFrame,
        CancellationToken ct);
}