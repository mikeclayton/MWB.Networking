using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;
using MWB.Networking.Layer1_Framing.Encoding.Helpers;
using System.Buffers;
using System.Buffers.Binary;

namespace MWB.Networking.Layer1_Framing.Encoding.LengthPrefixed;

public sealed class LengthPrefixedFrameDecoder : IFrameDecoder, IDisposable
{
    private readonly DecoderBuffer _buffer;
    private readonly int _maxFrameSize;

    private int? _expectedPayloadLength;

    public LengthPrefixedFrameDecoder(int maxFrameSize = 16 * 1024 * 1024)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFrameSize);

        _buffer = new DecoderBuffer();
        _maxFrameSize = maxFrameSize;
    }

    // should *not* be async
    public ValueTask DecodeFrameAsync(
        ReadOnlySequence<byte> input,
        IFrameDecoderSink output,
        CancellationToken ct)
    {
        // Append all incoming segments into the decoder buffer
        foreach (var segment in input)
        {
            _buffer.Append(segment.Span);
        }

        // Attempt to decode as many complete frames as possible
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            // Step 1: parse length prefix if not already known
            if (_expectedPayloadLength == null)
            {
                if (_buffer.Count < 4)
                {
                    // need more data
                    return ValueTask.CompletedTask;
                }

                _expectedPayloadLength =
                    BinaryPrimitives.ReadInt32BigEndian(
                        _buffer.Span.Slice(0, 4));

                if (_expectedPayloadLength < 0 ||
                    _expectedPayloadLength > _maxFrameSize)
                {
                    throw new InvalidDataException(
                        $"Invalid frame length: {_expectedPayloadLength}");
                }

                _buffer.Consume(4);
            }

            // Step 2: wait for full payload
            if (_buffer.Count < _expectedPayloadLength.Value)
            {
                // need more data
                return ValueTask.CompletedTask;
            }


            // Step 3: emit payload as a single decoded frame
            var payloadSpan =
                _buffer.Span.Slice(0, _expectedPayloadLength.Value);

            // Copy out payload before mutating buffer
            var payload = new ByteSegments(payloadSpan.ToArray());

            // Consume payload *before* emitting
            _buffer.Consume(_expectedPayloadLength.Value);
            _expectedPayloadLength = null;

            // Emit decoded frame
            // IMPORTANT: do NOT return — loop may decode more frames
            var task = output.OnFrameDecodedAsync(payload, ct);

            // If the sink goes async, stop decoding for now
            if (!task.IsCompletedSuccessfully)
            {
                return task;
            }

            // otherwise, continue loop and try to decode another frame
        }
    }

    public void Dispose()
    {
        _buffer.Dispose();
    }
}
