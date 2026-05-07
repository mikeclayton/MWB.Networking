using System.Buffers;

namespace MWB.Networking.Layer1_Framing.Codec.Buffer;

/// <summary>
/// Write-only, append-only access to a byte sink produced by the network pipeline.
///
/// Implementations are non-blocking and do not own execution,
/// publication, or transmission semantics.
/// </summary>
public interface ICodecBufferWriter
{
    /// <summary>
    /// Writes a complete payload segment to the pipeline.
    /// The data is copied and published as a single segment.
    /// </summary>
    void Write(ReadOnlySpan<byte> data);

    /// <summary>
    /// Writes a complete payload segment to the pipeline.
    /// The data is copied and published as a single segment.
    /// </summary>
    void Write(ReadOnlyMemory<byte> data);

    /// <summary>
    /// Writes a complete payload segment to the pipeline.
    /// The data is copied and published as a single segment.
    /// </summary>
    void Write(byte[] data);

    ///// <summary>
    ///// Writes a payload segment and adopts ownership of the memory.
    ///// The caller must not read, write, or dispose the memory after this call.
    ///// </summary>
    //void WriteAndAdopt(IMemoryOwner<byte> owner, int length);
}
