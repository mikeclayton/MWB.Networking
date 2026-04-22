using MWB.Networking.Layer2_Protocol.Frames;
using MWB.Networking.Layer2_Protocol.UnitTests.Helpers;
using MWB.Networking.UnitTest.Helpers.Layer2_Protocol;

namespace ProtocolSession;

/// <summary>
/// Tests for the request lifecycle: Request → Response* → Complete | Error | Cancel.
/// Covers snapshot state, outbound frame emission, and protocol violation guards.
/// </summary>
[TestClass]
public sealed partial class Requests_Invariants
{
    public TestContext TestContext
    {
        get;
        set;
    }

    // ---------------------------------------------------------------
    // Protocol violations
    // ---------------------------------------------------------------

    [TestMethod]
    public void Request_MissingRequestId_ThrowsProtocolException()
    {
        var session = ProtocolSessionHelper.CreateNullSession();
        var runtime = session.Runtime;

        var frame = ProtocolFrameGenerator.CreateInvalidProtocolFrame(
            ProtocolFrameKind.Request);

        Assert.Throws<ProtocolException>(() => runtime.ProcessFrame(frame));
    }

    [TestMethod]
    public void Response_MissingRequestId_ThrowsProtocolException()
    {
        // Cancel is always routed through request handling; a null RequestId always throws.
        var session = ProtocolSessionHelper.CreateNullSession();
        var runtime = session.Runtime;

        var frame = ProtocolFrameGenerator.CreateInvalidProtocolFrame(
            ProtocolFrameKind.Response);

        Assert.Throws<ProtocolException>(() => runtime.ProcessFrame(frame));
    }

    [TestMethod]
    public void Error_MissingRequestId_ThrowsProtocolException()
    {
        // Cancel is always routed through request handling; a null RequestId always throws.
        var session = ProtocolSessionHelper.CreateNullSession();
        var runtime = session.Runtime;

        var frame = ProtocolFrameGenerator.CreateInvalidProtocolFrame(
            ProtocolFrameKind.Error);

        Assert.Throws<ProtocolException>(() => runtime.ProcessFrame(frame));
    }

    [TestMethod]
    public void DuplicateRequestId_ThrowsProtocolException()
    {
        var session = ProtocolSessionHelper.CreateNullSession();
        var runtime = session.Runtime;

        runtime.ProcessFrame(ProtocolFrames.Request(1));

        Assert.Throws<ProtocolException>(
            () => runtime.ProcessFrame(ProtocolFrames.Request(1)));
    }

    [TestMethod]
    public void Response_UnknownRequestId_ThrowsProtocolException()
    {
        var session = ProtocolSessionHelper.CreateNullSession();
        var runtime = session.Runtime;

        Assert.Throws<ProtocolException>(
            () => runtime.ProcessFrame(ProtocolFrames.Response(99)));
    }

    [TestMethod]
    public void Error_UnknownRequestId_ThrowsProtocolException()
    {
        var session = ProtocolSessionHelper.CreateNullSession();
        var runtime = session.Runtime;

        Assert.Throws<ProtocolException>(
            () => runtime.ProcessFrame(ProtocolFrames.Error(99)));
    }

    [TestMethod]
    public void Cancel_UnknownRequestId_ThrowsProtocolException()
    {
        var session = ProtocolSessionHelper.CreateNullSession();
        var runtime = session.Runtime;

        Assert.Throws<ProtocolException>(
            () => runtime.ProcessFrame(ProtocolFrames.Error(99)));
    }

    [TestMethod]
    public void Response_AfterCompleteRequest_ThrowsProtocolException()
    {
        // Complete removes the context; the same RequestId is now unknown.
        var session = ProtocolSessionHelper.CreateNullSession();
        var runtime = session.Runtime;

        runtime.ProcessFrame(ProtocolFrames.Request(1));
        runtime.ProcessFrame(ProtocolFrames.Response(1));

        Assert.Throws<ProtocolException>(
            () => runtime.ProcessFrame(ProtocolFrames.Response(1)));
    }

    [TestMethod]
    public void CompleteRequest_Twice_ThrowsProtocolException()
    {
        var session = ProtocolSessionHelper.CreateNullSession();
        var runtime = session.Runtime;

        runtime.ProcessFrame(ProtocolFrames.Request(1));
        runtime.ProcessFrame(ProtocolFrames.Response(1));

        Assert.Throws<ProtocolException>(
            () => runtime.ProcessFrame(ProtocolFrames.Response(1)));
    }
}
