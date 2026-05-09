using Microsoft.Extensions.Logging.Abstractions;
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
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            _ = new NetworkPipeline(
                logger: null!,
                networkFrameCodec: new DefaultNetworkFrameCodec(),
                frameCodecs: [],
                transportCodec: new LengthPrefixedTransportCodec(NullLogger.Instance)));
    }

    [TestMethod]
    public void Constructor_NullNetworkFrameCodec_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(static () =>
            _ = new NetworkPipeline(
                logger: NullLogger.Instance,
                networkFrameCodec: null!,
                frameCodecs: [],
                transportCodec: new LengthPrefixedTransportCodec(NullLogger.Instance)));
    }

    [TestMethod]
    public void Constructor_NullFrameCodecs_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            _ = new NetworkPipeline(
                logger: NullLogger.Instance,
                networkFrameCodec: new DefaultNetworkFrameCodec(),
                frameCodecs: null!,
                transportCodec: new LengthPrefixedTransportCodec(NullLogger.Instance)));
    }

    [TestMethod]
    public void Constructor_NullTransportCodec_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            _ = new NetworkPipeline(
                logger: NullLogger.Instance,
                networkFrameCodec: new DefaultNetworkFrameCodec(),
                frameCodecs: [],
                transportCodec: null!));
    }

    [TestMethod]
    public void Constructor_ValidArguments_DoesNotThrow()
    {
        // Arrange / Act
        var pipeline = new NetworkPipeline(
            logger: NullLogger.Instance,
            networkFrameCodec: new DefaultNetworkFrameCodec(),
            frameCodecs: [new ReverseFrameCodec()],
            transportCodec: new LengthPrefixedTransportCodec(NullLogger.Instance));

        // Assert: construction must not throw and the object must be non-null.
        Assert.IsNotNull(pipeline);
    }
}
