using MWB.Networking.Layer1_Framing.Codec;
using MWB.Networking.Layer1_Framing.Codec.Buffer;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer1_Framing.Codecs.Default.Network;

namespace MWB.Networking.Layer1_Framing.Codecs.Default.UnitTests.Helpers;

/// <summary>
/// Shared utility methods for DefaultNetworkFrameCodec unit tests.
/// </summary>
internal static class CodecTestHelpers
{
    // -------------------------------------------------------------------------
    // Known flag bit positions
    // These mirror the internal NetworkFrameFlags enum so tests can verify the
    // flags byte in encoded output without depending on the internal type.
    // -------------------------------------------------------------------------

    internal const byte FlagEventType    = 0x01; // bit 0
    internal const byte FlagRequestId    = 0x02; // bit 1
    internal const byte FlagRequestType  = 0x04; // bit 2
    internal const byte FlagResponseType = 0x08; // bit 3
    internal const byte FlagStreamId     = 0x10; // bit 4
    internal const byte FlagStreamType   = 0x20; // bit 5

    // -------------------------------------------------------------------------
    // CodecBuffer helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a completed <see cref="CodecBuffer"/> containing one segment per
    /// supplied array (zero-length arrays are skipped).
    /// </summary>
    internal static CodecBuffer CreateInputBuffer(params byte[][] segments)
    {
        var buffer = new CodecBuffer();
        foreach (var segment in segments)
        {
            if (segment.Length > 0)
                buffer.Writer.Write(segment);
        }
        buffer.Writer.Complete();
        return buffer;
    }

    /// <summary>
    /// Drains all segments from a <see cref="CodecBuffer"/>'s reader and
    /// returns them concatenated as a single byte array.
    /// </summary>
    internal static byte[] ReadAllOutput(CodecBuffer buffer)
    {
        var chunks = new List<byte[]>();
        while (buffer.Reader.TryRead(out var memory))
        {
            chunks.Add(memory.ToArray());
            buffer.Reader.Advance(memory.Length);
        }
        return chunks.SelectMany(c => c).ToArray();
    }

    // -------------------------------------------------------------------------
    // Encode / Decode wrappers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Encodes <paramref name="frame"/> using a fresh codec instance and returns
    /// the raw output bytes.
    /// </summary>
    internal static byte[] Encode(NetworkFrame frame)
    {
        var codec = new DefaultNetworkFrameCodec();
        var outputBuffer = new CodecBuffer();
        codec.Encode(frame, outputBuffer.Writer);
        outputBuffer.Writer.Complete();
        return ReadAllOutput(outputBuffer);
    }

    /// <summary>
    /// Decodes a frame from the supplied byte segments using a fresh codec
    /// instance.  Segments become separate <see cref="CodecBuffer"/> entries so
    /// tests can exercise multi-segment decode paths.
    /// </summary>
    internal static (FrameDecodeResult result, NetworkFrame? frame) Decode(
        params byte[][] segments)
    {
        var buffer = CreateInputBuffer(segments);
        var codec = new DefaultNetworkFrameCodec();
        var result = codec.Decode(buffer.Reader, out var frame);
        return (result, result == FrameDecodeResult.Success ? frame : null);
    }

    // -------------------------------------------------------------------------
    // Frame assertion helper
    // -------------------------------------------------------------------------

    /// <summary>
    /// Asserts that every field of <paramref name="actual"/> matches
    /// <paramref name="expected"/>, producing field-level failure messages.
    /// </summary>
    internal static void AssertFramesEqual(
        NetworkFrame expected,
        NetworkFrame actual,
        string? message = null)
    {
        var prefix = message is null ? "" : $"{message}: ";
        Assert.AreEqual(expected.Kind,         actual.Kind,         $"{prefix}Kind mismatch");
        Assert.AreEqual(expected.EventType,    actual.EventType,    $"{prefix}EventType mismatch");
        Assert.AreEqual(expected.RequestId,    actual.RequestId,    $"{prefix}RequestId mismatch");
        Assert.AreEqual(expected.RequestType,  actual.RequestType,  $"{prefix}RequestType mismatch");
        Assert.AreEqual(expected.ResponseType, actual.ResponseType, $"{prefix}ResponseType mismatch");
        Assert.AreEqual(expected.StreamId,     actual.StreamId,     $"{prefix}StreamId mismatch");
        Assert.AreEqual(expected.StreamType,   actual.StreamType,   $"{prefix}StreamType mismatch");
        CollectionAssert.AreEqual(
            expected.Payload.ToArray(),
            actual.Payload.ToArray(),
            $"{prefix}Payload mismatch");
    }
}
