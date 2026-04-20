using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;

namespace MWB.Networking.Layer2_Protocol.UnitTests;

public partial class ProtocolSessionTests
{
    /// <summary>
    /// Tests for the request lifecycle: Request → Response* → Complete | Error | Cancel.
    /// Covers snapshot state, outbound frame emission, and protocol violation guards.
    /// </summary>
    [TestClass]
    public sealed partial class Streams_Lifecycle
    {
        public TestContext TestContext
        {
            get;
            set;
        }

        // ---------------------------------------------------------------
        // Streams - Lifecycle
        // ---------------------------------------------------------------

        [TestMethod]
        public void MultipleStreamData_EmittedInOrder()
        {
            var session = ProtocolSessionHelper.CreateNullSession();
            var runtime = session.Runtime;

            // Open a session-scoped stream
            var stream = session.Commands.OpenSessionStream(ProtocolFrames.EmptyPayload);

            // Emit multiple data frames
            stream.SendData(new byte[] { 10 });
            stream.SendData(new byte[] { 20 });
            stream.SendData(new byte[] { 30 });

            var outbound = runtime.DrainOutboundFrames();

            Assert.HasCount(4, outbound);

            // First frame is StreamOpen
            Assert.AreEqual(ProtocolFrameKind.StreamOpen, outbound[0].Kind);

            // StreamData frames in order
            Assert.AreEqual((byte)10, outbound[1].Payload.Span[0]);
            Assert.AreEqual((byte)20, outbound[2].Payload.Span[0]);
            Assert.AreEqual((byte)30, outbound[3].Payload.Span[0]);
        }
    }
}
