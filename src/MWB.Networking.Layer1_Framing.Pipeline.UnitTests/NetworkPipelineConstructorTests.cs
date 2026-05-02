using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer1_Framing.Codec.Abstractions;
using MWB.Networking.Layer1_Framing.Codecs.Default.Network;
using MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.Transport;
using MWB.Networking.Layer1_Framing.Codecs.Reverse.Frame;

namespace MWB.Networking.Layer1_Framing.Pipeline.UnitTests;

/// <summary>
/// Tests for <see cref="NetworkPipeline"/> construction.
/// Verifies that null arguments are rejected and that a well-formed
/// pipeline is created successfully.
/// </summary>
[TestClass]
public sealed class NetworkPipelineConstructorTests
{
    [TestMethod]
    public void Constructor_NullNetworkFrameCodec_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            _ = new NetworkPipeline(
                networkFrameCodec: null!,
                frameCodecs: Array.Empty<IFrameCodec>(),
                transportCodec: new LengthPrefixedTransportCodec(NullLogger.Instance)));
    }

    [TestMethod]
    public void Constructor_NullFrameCodecs_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            _ = new NetworkPipeline(
                networkFrameCodec: new DefaultNetworkFrameCodec(),
                frameCodecs: null!,
                transportCodec: new LengthPrefixedTransportCodec(NullLogger.Instance)));
    }

    [TestMethod]
    public void Constructor_NullTransportCodec_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            _ = new NetworkPipeline(
                networkFrameCodec: new DefaultNetworkFrameCodec(),
                frameCodecs: Array.Empty<IFrameCodec>(),
                transportCodec: null!));
    }

    [TestMethod]
    public void Constructor_ValidArguments_DoesNotThrow()
    {
        // Arrange / Act
        var pipeline = new NetworkPipeline(
            networkFrameCodec: new DefaultNetworkFrameCodec(),
            frameCodecs: new[] { new ReverseFrameCodec() },
            transportCodec: new LengthPrefixedTransportCodec(NullLogger.Instance));

        // Assert: construction must not throw and the object must be non-null.
        Assert.IsNotNull(pipeline);
    }
}
