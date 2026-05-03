using Microsoft.Extensions.Logging;
using MWB.Networking.Layer1_Framing.Codec.Abstractions;
using MWB.Networking.Layer1_Framing.Codec.Buffer;
using System.Buffers;

namespace MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.Transport;

public sealed class LengthPrefixedTransportCodec : ITransportCodec
{
    public LengthPrefixedTransportCodec(
        ILogger logger,
        int maxFrameSize = 16 * 1024 * 1024)
    {
        this.Encoder = new LengthPrefixedTransportEncoder(logger);
        this.Decoder = new LengthPrefixedTransportDecoder(logger, maxFrameSize);
    }

    public LengthPrefixedTransportEncoder Encoder
    {
        get;
    }

    public LengthPrefixedTransportDecoder Decoder
    {
        get;
    }

    public bool TryDecode(ref ReadOnlySequence<byte> inputBytes, out ReadOnlyMemory<byte> outputBytes)
    {
        return this.Decoder.TryDecode(ref inputBytes, out outputBytes);
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
