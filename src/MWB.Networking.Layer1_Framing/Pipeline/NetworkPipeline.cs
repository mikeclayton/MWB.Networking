using Microsoft.Extensions.Logging;
using MWB.Networking.Layer0_Transport;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;
using MWB.Networking.Layer1_Framing.Frames;
using System.Buffers;

namespace MWB.Networking.Layer1_Framing.Pipeline;

/// <summary>
/// Passive Layer‑1 pipeline.
/// Encapsulates byte <-> frame transformation capabilities,
/// but does not own execution policy or control flow.
/// </summary>
public sealed class NetworkPipeline : IDisposable
{
    public NetworkPipeline(
        ILogger logger,
        INetworkConnection connection,
        NetworkFrameWriter frameWriter,
        NetworkFrameReader frameReader,
        IFrameDecoder rootDecoder)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        this.FrameWriter = frameWriter ?? throw new ArgumentNullException(nameof(frameWriter));
        this.FrameReader = frameReader ?? throw new ArgumentNullException(nameof(frameReader));
        this.RootDecoder = rootDecoder ?? throw new ArgumentNullException(nameof(rootDecoder));
    }

    public ILogger Logger
    {
        get;
    }

    // ------------------------------------------------------------
    // Internal wiring (Layer 1 topology)
    // ------------------------------------------------------------

    /// <summary>
    /// Gets the underlying network transport used by this pipeline.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="INetworkConnection"/> represents the
    /// byte-level transport. Ownership and disposal of the connection
    /// are determined by the component that created the pipeline.
    /// </remarks>
    internal INetworkConnection Connection
    {
        get;
    }

    /// <summary>
    /// Gets the entry point of the outbound encoding pipeline.
    /// </summary>
    /// <remarks>
    /// Frames written here enter the Layer 1 encoding pipeline, where they
    /// are transformed into bytes and forwarded to the underlying transport.
    /// This marks the boundary between higher-layer frame semantics and
    /// byte-level network transmission.
    internal NetworkFrameWriter FrameWriter
    {
        get;
    }

    /// <summary>
    /// Gets the exit point of the inbound decoding pipeline for decoded frames.
    /// </summary>
    /// <remarks>
    /// Frames delivered here have passed through the entire Layer 1
    /// decoding pipeline and are ready for consumption by higher layers.
    /// </remarks>
    internal NetworkFrameReader FrameReader
    {
        get;
    }

    /// <summary>
    /// Gets the entry point of the inbound decoding pipeline.
    /// </summary>
    /// All inbound bytes read from the underlying transport must enter
    /// the Layer 1 decoding pipeline through this decoder. The decoding
    /// pipeline transforms raw bytes into logical frames, which are then
    /// delivered through the pipeline to its exit point for consumption
    /// by higher layers.
    internal IFrameDecoder RootDecoder
    {
        get;
    }

    // ------------------------------------------------------------
    // Public execution capabilities (called by ProtocolDriver)
    // ------------------------------------------------------------

    /// <summary>
    /// Reads raw bytes from the underlying transport.
    /// Returns the number of bytes read; 0 indicates EOF.
    /// </summary>
    public ValueTask<int> ReadBytesAsync(
        Memory<byte> buffer,
        CancellationToken ct = default)
    {
        return this.Connection.ReadAsync(buffer, ct);
    }

    /// <summary>
    /// Feeds a sequence of bytes into the decoding pipeline.
    /// Any fully decoded frames are delivered to the internal FrameReader.
    /// </summary>
    public ValueTask DecodeFrameAsync(
        ReadOnlySequence<byte> bytes,
        CancellationToken ct = default)
    {
        return this.RootDecoder.DecodeFrameAsync(bytes, this.FrameReader, ct);
    }

    /// <summary>
    /// Signals end‑of‑input to the decoding pipeline.
    /// Causes any buffered partial frames to be flushed or discarded
    /// according to decoder semantics.
    /// </summary>
    public ValueTask CompleteDecodingAsync(
        CancellationToken ct = default)
    {
        return this.RootDecoder.CompleteAsync(this.FrameReader, ct);
    }

    /// <summary>
    /// Reads the next decoded NetworkFrame.
    /// Blocks until a frame is available or cancellation is requested.
    /// </summary>
    public Task<NetworkFrame> ReadFrameAsync(
        CancellationToken ct = default)
    {
        return this.FrameReader.ReadFrameAsync(ct);
    }

    /// <summary>
    /// Writes a single NetworkFrame into the outbound encoding pipeline.
    /// Completion indicates the frame has been handed off to the transport sink.
    /// </summary>
    public ValueTask WriteFrameAsync(
        NetworkFrame frame,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(frame);
        return this.FrameWriter.WriteAsync(frame, ct);
    }


    public void Dispose()
    {
        // Stop accepting new frames (if needed)
        this.Connection.Dispose();
    }
}
