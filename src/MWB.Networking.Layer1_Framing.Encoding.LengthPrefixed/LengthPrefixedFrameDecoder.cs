using Microsoft.Extensions.Logging;
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
    private bool _completed;

    public LengthPrefixedFrameDecoder(ILogger logger, int maxFrameSize = 16 * 1024 * 1024)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFrameSize);

        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _buffer = new DecoderBuffer();
        _maxFrameSize = maxFrameSize;
    }

    public ILogger Logger
    {
        get;
    }

    // ─────────────────────────────────────────────────────────────
    // Decode
    // ─────────────────────────────────────────────────────────────
    // NOTE: intentionally non-async; returns ValueTask for efficiency
    public ValueTask DecodeFrameAsync(
        ReadOnlySequence<byte> input,
        IFrameDecoderSink output,
        CancellationToken ct)
    {
        if (_completed)
        {
            throw new InvalidOperationException(
                "DecodeFrameAsync called after decoder completion.");
        }

        // Append all incoming segments into the decoder buffer
        foreach (var segment in input)
        {
            _buffer.Append(segment.Span);
        }

        this.Logger.LogDebug(
            "[DECODER] {DecoderType} Appended {ByteCount} bytes, buffer now has {BufferedBytes}",
            nameof(LengthPrefixedFrameDecoder),
            input.Length,
            _buffer.Count);

        // Attempt to decode as many complete frames as possible
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            // Step 1: parse length prefix if not already known
            if (_expectedPayloadLength is null)
            {
                if (_buffer.Count < 4)
                {
                    this.Logger.LogDebug(
                        "[DECODER] {DecoderType} Waiting for length prefix (buffer has {BufferedBytes})\",",
                        nameof(LengthPrefixedFrameDecoder),
                        _buffer.Count);

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
                this.Logger.LogDebug(
                    "[DECODER] {DecoderType} Waiting for payload: need {Expected}, have {Buffered}",
                    nameof(LengthPrefixedFrameDecoder),
                    _expectedPayloadLength.Value,
                    _buffer.Count);

                // need more data
                return ValueTask.CompletedTask;
            }

            // Step 3: emit decoded frame
            var payloadLength = _expectedPayloadLength.Value;
            var payloadSpan = _buffer.Span.Slice(0, payloadLength);

            // Copy out payload before mutating buffer
            var payload = new ByteSegments(payloadSpan.ToArray());

            _buffer.Consume(payloadLength);
            _expectedPayloadLength = null;

            // Emit decoded frame
            // IMPORTANT: do NOT return — loop may decode more frames
            this.Logger.LogDebug(
                "[DECODER] {DecoderType} Emitting decoded frame with payload size {PayloadSize}",
                nameof(LengthPrefixedFrameDecoder),
                payloadLength);

            var writeTask = output.OnFrameDecodedAsync(payload, ct);
            this.Logger.LogDebug(
                "[DECODER] {DecoderType} Sink returned IsCompletedSuccessfully = {Completed}",
                nameof(LengthPrefixedFrameDecoder),
                writeTask.IsCompletedSuccessfully);

            // If the sink goes async, pause decoding
            if (!writeTask.IsCompletedSuccessfully)
            {
                return writeTask;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Completion
    // ─────────────────────────────────────────────────────────────
    public ValueTask CompleteAsync(
        IFrameDecoderSink output,
        CancellationToken ct)
    {
        if (_completed)
        {
            return ValueTask.CompletedTask;
        }

        _completed = true;

        ct.ThrowIfCancellationRequested();

        // Any buffered data at EOF is a protocol violation
        if (_expectedPayloadLength is not null || _buffer.Count > 0)
        {
            throw new InvalidDataException(
                "End-of-stream encountered with incomplete length-prefixed frame.");
        }

        // Nothing to flush — framing guarantees whole-frame delivery
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        _buffer.Dispose();
    }
}
