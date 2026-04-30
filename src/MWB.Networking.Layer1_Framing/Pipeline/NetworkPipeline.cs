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

        var currentBuffer = new CodecBuffer();

        try
        {
            // Stage 0: semantic frame -> framed bytes
            _networkFrameCodec.Encode(frame, currentBuffer.Writer);
            currentBuffer.Writer.Complete();

            // Stage 1..N: framing codecs (forward order)
            var reader = currentBuffer.Reader;
            foreach (var codec in _frameCodecs)
            {
                var nextBuffer = new CodecBuffer();
                codec.Encode(reader, nextBuffer.Writer);
                nextBuffer.Writer.Complete();

                // old buffer is no longer needed
                currentBuffer.Dispose();

                // promote next buffer to be current
                currentBuffer = nextBuffer;
                reader = nextBuffer.Reader;
            }

            // Stage N+1: framing -> transport bytes
            using var transportBuffer = new CodecBuffer();
            _transportCodec.Encode(reader, transportBuffer.Writer);
            transportBuffer.Writer.Complete();

            // Emit segment-preserving transport payload
            return transportBuffer.ToByteSegments();
        }
        finally
        {
            // now safe to dispose final intermediate buffer
            currentBuffer?.Dispose();
        }
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
            return FrameDecodeResult.NeedsMoreData; // <- important: caller must retry later
        }

        // Seed a pipeline buffer with the framed bytes
        using var buffer = new CodecBuffer();
        buffer.Writer.Write(framedBytes);
        buffer.Writer.Complete();

        var reader = buffer.Reader;

        // ------------------------------------------------------
        // Step 2: Frame codecs (reverse order)
        // ------------------------------------------------------

        // manually walking the array backwards avoids allocation by Enumerable.Reverse()
        for (var i = _frameCodecs.Count - 1; i >= 0; i--)
        {
            var codec = _frameCodecs[i];
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
