using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer1_Framing.Codec;
using MWB.Networking.Layer1_Framing.Codec.Abstractions;
using MWB.Networking.Layer1_Framing.Codec.Buffer;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using System.Buffers;

namespace MWB.Networking.Layer1_Framing.Pipeline;

/// <summary>
/// Composes a network framing pipeline from codecs.
/// Owns ordering, buffering, and direction, but not execution or transport.
/// </summary>
public sealed class NetworkPipeline
{
    private readonly INetworkFrameCodec _networkFrameCodec;
    private readonly IReadOnlyList<IFrameCodec> _frameCodecs;
    private readonly ITransportCodec _transportCodec;

    public NetworkPipeline(
        INetworkFrameCodec networkFrameCodec,
        IReadOnlyList<IFrameCodec> frameCodecs,
        ITransportCodec transportCodec)
    {
        _networkFrameCodec = networkFrameCodec
            ?? throw new ArgumentNullException(nameof(networkFrameCodec));
        _frameCodecs = frameCodecs
            ?? throw new ArgumentNullException(nameof(frameCodecs));
        _transportCodec = transportCodec
            ?? throw new ArgumentNullException(nameof(transportCodec));
    }

    // --------------------------------------------------------------------
    // Encode: NetworkFrame -> ByteSegments
    // --------------------------------------------------------------------

    /// <summary>
    /// Encodes a single network frame into transport-ready byte segments.
    /// </summary>
    public ByteSegments Encode(NetworkFrame frame)
    {
        using var buffer = new CodecBuffer();

        var writer = buffer.Writer;
        var reader = buffer.Reader;

        // Step 1: semantic frame -> framed bytes
        _networkFrameCodec.Encode(frame, writer);

        // Step 2: framing codecs (forward order)
        foreach (var codec in _frameCodecs)
        {
            codec.Encode(reader, writer);
        }

        // Step 3: framing -> transport bytes
        _transportCodec.Encode(reader, writer);

        // Emit segment-preserving transport payload
        return buffer.ToByteSegments();
    }

    // --------------------------------------------------------------------
    // Decode: transport bytes -> NetworkFrame
    // --------------------------------------------------------------------

    /// <summary>
    /// Attempts to decode a single network frame from transport bytes.
    /// Advances the input sequence only on success.
    /// </summary>
    public FrameDecodeResult Decode(
       ref ReadOnlySequence<byte> transportBytes,
       out NetworkFrame? frame)
    {
        frame = null;

        // Work on a local copy so transport consumption is atomic
        var current = transportBytes;

        // ------------------------------------------------------
        // Step 1: Transport boundary → framed bytes
        // ------------------------------------------------------
        if (!_transportCodec.TryDecode(
                ref current,
                out ReadOnlyMemory<byte> framedBytes))
        {
            // Not enough transport bytes yet
            return FrameDecodeResult.Success; // <- important: caller must retry later
        }

        // Seed a pipeline buffer with the framed bytes
        using var buffer = new CodecBuffer();
        buffer.Writer.Write(framedBytes);
        buffer.Writer.Complete();

        var reader = buffer.Reader;

        // ------------------------------------------------------
        // Step 2: Frame codecs (reverse order)
        // ------------------------------------------------------
        foreach (var codec in Enumerable.Reverse(_frameCodecs))
        {
            using var nextBuffer = new CodecBuffer();

            var frameResult = codec.Decode(reader, nextBuffer.Writer);
            if (frameResult != FrameDecodeResult.Success)
            {
                return frameResult; // InvalidFrameEncoding
            }

            nextBuffer.Writer.Complete();
            reader = nextBuffer.Reader;
        }

        // ------------------------------------------------------
        // Step 3: Semantic frame decode
        // ------------------------------------------------------

        var networkFrameResult = _networkFrameCodec.Decode(reader, out frame);
        if (networkFrameResult != FrameDecodeResult.Success)
        {
            // Structural failure: bytes cannot materialise a NetworkFrame
            return networkFrameResult;
        }

        // ------------------------------------------------------
        // Commit transport consumption
        // ------------------------------------------------------
        transportBytes = current;
        return FrameDecodeResult.Success;
    }
}
