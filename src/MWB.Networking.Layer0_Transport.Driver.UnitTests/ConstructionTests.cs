using MWB.Networking.Layer0_Transport.Driver.UnitTests.Helpers;

namespace MWB.Networking.Layer0_Transport.Driver.UnitTests;

// ============================================================
//  Construction tests
//
//  Verify that TransportDriver validates its constructor
//  arguments and subscribes to transport events at construction
//  time, before Start() is ever called.
// ============================================================

[TestClass]
public sealed class ConstructionTests
{
    public TestContext TestContext { get; set; } = null!;

    // ------------------------------------------------------------------
    // Argument validation
    // ------------------------------------------------------------------

    /// <summary>
    /// Passing a null transport must throw immediately rather than
    /// silently storing a null and crashing later.
    /// </summary>
    [TestMethod]
    public void Constructor_NullTransport_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new TransportDriver(null!, TestPipeline.CreateLengthPrefixed()));
    }

    /// <summary>
    /// Passing a null pipeline must throw immediately.
    /// </summary>
    [TestMethod]
    public void Constructor_NullPipeline_ThrowsArgumentNullException()
    {
        var transport = new FakeTransportStack();
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new TransportDriver(transport, null!));
    }

    // ------------------------------------------------------------------
    // Event subscription at construction time
    // ------------------------------------------------------------------

    /// <summary>
    /// The driver must subscribe to <see cref="ITransportEvents.TransportClosed"/>
    /// during construction so that a transport-side close can be observed even
    /// before <see cref="TransportDriver.Start"/> is called.
    /// </summary>
    [TestMethod]
    public void Constructor_SubscribesToTransportClosedEvent()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        var closedFired = false;
        driver.Closed += () => closedFired = true;

        transport.RaiseTransportClosed();

        Assert.IsTrue(closedFired,
            "Closed event should fire when TransportClosed is raised, even before Start().");
    }

    /// <summary>
    /// The driver must subscribe to <see cref="ITransportEvents.TransportFaulted"/>
    /// during construction so that a transport-side fault can be observed even
    /// before <see cref="TransportDriver.Start"/> is called.
    /// </summary>
    [TestMethod]
    public void Constructor_SubscribesToTransportFaultedEvent()
    {
        var transport = new FakeTransportStack();
        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());

        Exception? receivedException = null;
        driver.Faulted += ex => receivedException = ex;

        var injectedFault = new IOException("simulated transport fault");
        transport.RaiseTransportFaulted(injectedFault);

        Assert.IsNotNull(receivedException,
            "Faulted event should fire when TransportFaulted is raised, even before Start().");
        Assert.AreSame(injectedFault, receivedException,
            "The exact exception object raised by the transport should be forwarded.");
    }

    /// <summary>
    /// A freshly constructed driver — before Start() or any transport events —
    /// must not have raised Closed or Faulted.
    /// </summary>
    [TestMethod]
    public void Constructor_WithValidArguments_DoesNotRaiseEventsImmediately()
    {
        var transport = new FakeTransportStack();
        var closedFired = false;
        Exception? faultedEx = null;

        using var driver = new TransportDriver(transport, TestPipeline.CreateLengthPrefixed());
        driver.Closed += () => closedFired = true;
        driver.Faulted += ex => faultedEx = ex;

        Assert.IsFalse(closedFired, "Closed must not fire on construction.");
        Assert.IsNull(faultedEx, "Faulted must not fire on construction.");
    }
}
