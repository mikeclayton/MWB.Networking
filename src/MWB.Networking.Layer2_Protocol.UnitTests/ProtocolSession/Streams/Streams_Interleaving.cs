using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace _ProtocolSession;

/// <summary>
/// Tests for the request lifecycle: Request → Response* → Complete | Error | Cancel.
/// Covers snapshot state, outbound frame emission, and protocol violation guards.
/// </summary>
[TestClass]
public sealed partial class Streams_Interleaving
{
    public TestContext TestContext
    {
        get;
        set;
    }

    // ---------------------------------------------------------------
    // Streams - Interleaving
    // ---------------------------------------------------------------

    [TestMethod]
    public void InterleavedSessionStreams_FramesEmittedInOrder()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        // Open two session-scoped streams
        var stream1 = session.Commands.OpenSessionStream();
        var stream2 = session.Commands.OpenSessionStream();

        // Interleave data writes
        stream1.SendData(new byte[] { 0x01 });
        stream2.SendData(new byte[] { 0x02 });

        // Close in the same interleaved order
        stream1.Close();
        stream2.Close();

        var outbound = processor.DrainOutboundFrames();

        Assert.HasCount(6, outbound);

        // Open frames
        Assert.AreEqual(ProtocolFrameKind.StreamOpen, outbound[0].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamOpen, outbound[1].Kind);

        // Interleaved data
        Assert.AreEqual(ProtocolFrameKind.StreamData, outbound[2].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamData, outbound[3].Kind);

        // Close frames
        Assert.AreEqual(ProtocolFrameKind.StreamClose, outbound[4].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamClose, outbound[5].Kind);
    }

    [TestMethod]
    public void LocalSessionStreams_MayInterleaveOutboundFrames()
    {
        var logger = NullLogger.Instance;
        var session = ProtocolSessionHelper.CreateOddProtocolSession(logger);
        var processor = session.Processor;

        var s1 = session.Commands.OpenSessionStream();
        var s2 = session.Commands.OpenSessionStream();

        s1.SendData(new byte[] { 0x01 });
        s2.SendData(new byte[] { 0x02 });
        s1.SendData(new byte[] { 0x03 });
        s2.SendData(new byte[] { 0x04 });

        s1.Close();
        s2.Close();

        var outbound = processor.DrainOutboundFrames();

        Assert.HasCount(8, outbound);

        // Open frames
        Assert.AreEqual(ProtocolFrameKind.StreamOpen, outbound[0].Kind);
        Assert.AreEqual(ProtocolFrameKind.StreamOpen, outbound[1].Kind);

        var id1 = outbound[0].StreamId;
        var id2 = outbound[1].StreamId;

        // Interleaved data
        Assert.AreEqual(id1, outbound[2].StreamId);
        Assert.AreEqual(id2, outbound[3].StreamId);
        Assert.AreEqual(id1, outbound[4].StreamId);
        Assert.AreEqual(id2, outbound[5].StreamId);

        // Close frames
        Assert.AreEqual(ProtocolFrameKind.StreamClose, outbound[6].Kind);
        Assert.AreEqual(id1, outbound[6].StreamId);
        Assert.AreEqual(ProtocolFrameKind.StreamClose, outbound[7].Kind);
        Assert.AreEqual(id2, outbound[7].StreamId);
    }
}
