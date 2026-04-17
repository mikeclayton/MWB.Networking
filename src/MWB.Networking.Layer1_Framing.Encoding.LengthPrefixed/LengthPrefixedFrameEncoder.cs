using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;
using System.Buffers.Binary;

namespace MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed;

public sealed class LengthPrefixedFrameEncoder : IFrameEncoder
{
    // should *not* be async
    public ValueTask EncodeFrameAsync(
        ByteSegments input,
        IFrameEncoderSink output,
        CancellationToken ct)
    {
        // 1. compute payload length
        var length = 0;
        foreach (var segment in input.Segments)
        {
            length += segment.Length;
        }

        // 2. allocate prefix (4 bytes, big-endian)
        var prefix = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(prefix, length);

        // 3. emit [prefix] + original segments (no copying)
        var outSegments = new ReadOnlyMemory<byte>[input.Segments.Length + 1];
        outSegments[0] = prefix;

        // given:
        //
        // input.Segments:
        //   [segmentA -> bytes 0..99 ]
        //   [segmentB -> bytes 100..999 ]
        //
        // we end up with:
        //
        // outSegments:
        //    [length prefix -> new byte[4] ]
        //    [segmentA -> same bytes 0..99 ]
        //    [segmentB -> same bytes 100..999 ]

        Array.Copy(
            input.Segments,
            0,
            outSegments,
            1,
            input.Segments.Length);

        return output.OnFrameEncodedAsync(
            new ByteSegments(outSegments),
            ct);
    }
}
