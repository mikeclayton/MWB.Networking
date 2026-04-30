using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Codec.Abstractions;
using MWB.Networking.Layer1_Framing.Codec.Buffer;
using System.Buffers;

namespace MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed;

public sealed class LengthPrefixedFrameCodec : ITransportCodec
{
    public LengthPrefixedFrameCodec(
        ILogger logger,
        int maxFrameSize = 16 * 1024 * 1024)
    {
        this.Encoder = new LengthPrefixedFrameEncoder(logger);
        this.Decoder = new LengthPrefixedFrameDecoder(logger, maxFrameSize);
    }

    public LengthPrefixedFrameEncoder Encoder
    {
        get;
    }

    public LengthPrefixedFrameDecoder Decoder
    {
        get;
    }

    public bool TryDecode(ref ReadOnlySequence<byte> inputBytes, out ReadOnlyMemory<byte> outputBytes)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Encodes a single, complete input value into one or more output segments.
    /// This method is synchronous and must not block or await.
    /// </summary>
    void ITransportCodec.Encode(ICodecBufferReader inputReader, ICodecBufferWriter outputWriter)
    {
        this.Encoder.Encode(inputReader, outputWriter);
    }
}
