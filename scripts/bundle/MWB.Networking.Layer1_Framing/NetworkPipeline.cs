using MWB.Networking.Layer0_Transport;
using MWB.Networking.Layer1_Framing.Encoding.Abstractions;

namespace MWB.Networking.Layer1_Framing;


/// <summary>
/// Represents a fully constructed Layer 1 network pipeline.
///
/// A <see cref="NetworkPipeline"/> is the completed result of
/// <see cref="NetworkPipelineBuilder"/>. It encapsulates the
/// framing boundary between raw transport bytes and logical
/// <see cref="NetworkFrame"/> instances, but contains no protocol
/// semantics or lifecycle logic.
/// </summary>
/// <remarks>
/// <para>
/// This type is a first-class artifact of <b>Layer 1 (Framing)</b>.
/// It combines:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///     The underlying <see cref="INetworkConnection"/> (transport)
///     </description>
///   </item>
///   <item>
///     <description>
///     A <see cref="NetworkFrameWriter"/> for emitting frames
///     </description>
///   </item>
///   <item>
///     <description>
///     A <see cref="NetworkFrameReader"/> for receiving decoded frames
///     </description>
///   </item>
///   <item>
///     <description>
///     The root <see cref="IFrameDecoder"/> used to transform inbound
///     bytes into frames
///     </description>
///   </item>
/// </list>
/// <para>
/// <see cref="NetworkPipeline"/> is immutable and contains no behavior
/// beyond exposing its components. Ownership and lifecycle management
/// of the underlying transport are defined by the layer that constructs
/// the pipeline (for example, an application or a protocol session).
/// </para>
/// <para>
/// Higher layers (such as protocol sessions) consume a
/// <see cref="NetworkPipeline"/> as an input, but do not modify it.
/// </para>
/// </remarks>
public sealed class NetworkPipeline
{
    public NetworkPipeline(
        INetworkConnection connection,
        NetworkFrameWriter frameWriter,
        NetworkFrameReader frameReader,
        IFrameDecoder rootDecoder)
    {
        this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        this.FrameWriter = frameWriter ?? throw new ArgumentNullException(nameof(frameWriter));
        this.FrameReader = frameReader ?? throw new ArgumentNullException(nameof(frameReader));
        this.RootDecoder = rootDecoder ?? throw new ArgumentNullException(nameof(rootDecoder));
    }

    /// <summary>
    /// Gets the underlying network transport used by this pipeline.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="INetworkConnection"/> represents the
    /// byte-level transport. Ownership and disposal of the connection
    /// are determined by the component that created the pipeline.
    /// </remarks>
    public INetworkConnection Connection
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
    public NetworkFrameWriter FrameWriter
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
    public NetworkFrameReader FrameReader
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
    public IFrameDecoder RootDecoder
    {
        get;
    }
}
