namespace MWB.Networking.Layer1_Framing.Codec.Buffer;

public sealed class CodecBufferWriter : ICodecBufferWriter
{
    private readonly CodecBuffer _outputBuffer;

    public CodecBufferWriter(CodecBuffer outputBuffer)
    {
        _outputBuffer = outputBuffer ?? throw new ArgumentNullException(nameof(outputBuffer));
    }

    public void Write(ReadOnlySpan<byte> data)
    {
        if (_outputBuffer.IsWriteCompleted)
            throw new InvalidOperationException("Writer already completed.");

        // Explicit copy, explicit segment
        // (ReadOnlySpan<byte> is stack-based, so we
        // need to copy it and not store a reference
        var copy = data.ToArray();
        _outputBuffer.Enqueue(copy);
    }

    public void Write(ReadOnlyMemory<byte> data)
    {
        if (_outputBuffer.IsWriteCompleted)
        {
            throw new InvalidOperationException("Writer already completed.");
        }

        // Explicit copy, explicit segment
        // (ReadOnlyMemory<byte> is heap-based, but we don't own the lifetime so we
        // need to copy it and not store a reference because the owner might dispose
        // it (e.g. pooled memory)
        var copy = data.ToArray();
        _outputBuffer.Enqueue(copy);
    }

    public void Write(byte[] data)
    {
        if (_outputBuffer.IsWriteCompleted)
        {
            throw new InvalidOperationException("Writer already completed.");
        }

        // Explicit copy, explicit segment
        var copy = data.ToArray();
        _outputBuffer.Enqueue(copy);
    }

    public void Complete()
    {
        _outputBuffer.WriteComplete();
    }
}
