namespace MWB.Networking.Layer1_Framing.Codec.Buffer;

/// <summary>
/// Read-only, forward-only access to a stream of bytes produced by the network pipeline.
///
/// Implementations are non-blocking and expose availability separately from completion.
/// </summary>
public interface ICodecBufferReader
{
    long Position
    {
        get;
    }

    long Length
    {
        get;
    }

    /// <summary>
    /// Attempts to obtain currently available readable bytes.
    /// Returns false if no data is available at this time.
    /// </summary>
    /// <remarks>
    /// A false return does not indicate end-of-stream. Callers must consult
    /// <see cref="IsCompleted"/> to determine whether no further data will arrive.
    /// </remarks>
    bool TryRead(out ReadOnlyMemory<byte> memory);

    /// <summary>
    /// Advances the read cursor by the specified number of bytes.
    /// </summary>
    /// <param name="count">The number of bytes consumed.</param>
    void Advance(int count);

    /// <summary>
    /// Gets whether the underlying byte source has completed and no further data will arrive.
    /// </summary>
    bool IsCompleted
    {
        get;
    }
}
