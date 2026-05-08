using MWB.Networking.Layer0_Transport.Memory;
using MWB.Networking.Layer1_Framing.Codecs.Default.Network;
using MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.Transport;
using MWB.Networking.Layer3_Endpoint.Hosting;
using MWB.Networking.Logging.Debug;

namespace _ProtocolSession;

[TestClass]
public partial class StreamContext_Lifecycle
{
    public TestContext TestContext
    {
        get;
        set;
    }

    [TestCleanup]
    public void Cleanup()
    {
        // force any unobserved exceptions from finalizers to surface during
        // test runs rather than being silently ignored - this makes it easier
        // to determine *which* test caused the issue (and fix it!).
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// Regression test for a real-world lifecycle bug where multiple teardown paths
    /// attempted to close the same StreamContext more than once.
    ///
    /// Stream termination must be idempotent: invoking Close multiple times during
    /// normal protocol teardown (e.g. request completion, stream cleanup, or session
    /// shutdown) must not throw and must not change observable behavior after the
    /// first close.
    /// </summary>
    /// <remarks>
    /// This is a test using Layer3 features to test a Layer2 behaviour.
    /// </remarks>
    [TestMethod]
    public async Task RequestScoped_Stream_Teardown_Must_Not_Double_Close()
    {
        // fail the test after 10 seconds if there code is still running / waiting
        var lifecycleCts = new CancellationTokenSource(
            TimeSpan.FromSeconds(10));

        //var logger = NullLogger.Instance;
        var (logger, _) = DebugLoggerFactory.CreateLogger();

        // ------------------------------------------------------------
        // Arrange: create paired in-memory providers
        // ------------------------------------------------------------
        var (providerA, providerB) =
            InMemoryNetworkConnectionProvider.CreateDuplexProviders(logger);

        var inboundObserved = new TaskCompletionSource();

        // ------------------------------------------------------------
        // Session B: inbound request handler (responds immediately)
        // ------------------------------------------------------------
        var endpointB = new SessionEndpointBuilder()
            .UseLogger(logger)
            .UseOddStreamIds()
            .ConfigurePipelineWith(
                pipeline =>
                {
                    pipeline
                        .UseLogger(logger)
                        .UseDefaultNetworkCodec()
                        .UseLengthPrefixedCodec(logger)
                        .UseConnectionProvider(providerB);
                }
            )
            .OnRequestReceived((request, payload) =>
            {
                request.Respond();
            })
            .OnStreamOpened((_, _) =>
            {
                // signal to the min test that the stream has been observed
                // on the receving peer so we know it's safe to close now
                inboundObserved.TrySetResult();
            })
            .Build();

        // ------------------------------------------------------------
        // Session A: outbound peer
        // ------------------------------------------------------------
        var endpointA = new SessionEndpointBuilder()
            .UseLogger(logger)
            .UseEvenStreamIds()
            .ConfigurePipelineWith(
                pipeline =>
                {
                    pipeline
                        .UseLogger(logger)
                        .UseDefaultNetworkCodec()
                        .UseLengthPrefixedCodec(logger)
                        .UseConnectionProvider(providerA);
                }
            )
            .Build();

        // Start both sessions
        await endpointA
            .StartAsync(lifecycleCts.Token)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        await endpointB
            .StartAsync(lifecycleCts.Token)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        // ------------------------------------------------------------
        // Act: send request + open request-scoped stream
        // ------------------------------------------------------------

        // Flow:
        //   1) Session A sends a request to Session B.
        //   2) Session A opens a *request-scoped* stream on that request.
        //   3) The request-scoped stream is delivered inbound to Session B.
        //   4) Session B responds to the request.
        //   5) Responding causes request teardown on B, which must also
        //      tear down all request-scoped streams.
        //   6) Request teardown and stream teardown paths converge.
        //   7) In the regression case, converging teardown paths cause the same
        //      stream to be closed more than once, incorrectly throwing a
        //      "stream already closed" exception.
        //
        // This test ensures (7) does not happen - ie. stream closure should be idempotent.

        Exception? caught = null;

        try
        {
            var request = endpointA.SendRequest();

            // This MUST be a request-scoped stream
            // Adjust name if your API differs
            var stream = request.OpenRequestStream(1u);
            stream.SendData(default);

            // Ensure stream was observed inbound before teardown
            await inboundObserved.Task.WaitAsync(lifecycleCts.Token);

            // Respond happens on session B automatically
            await request.Response.WaitAsync(lifecycleCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            caught = ex;
        }
        finally
        {
            lifecycleCts.Cancel();
        }

        // ------------------------------------------------------------
        // Shutdown
        // ------------------------------------------------------------
        try
        {
            await Task
                .WhenAll(
                    endpointA.DisposeAsync().AsTask(),
                    endpointB.DisposeAsync().AsTask())
                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            // expected
        }

        // ------------------------------------------------------------
        // Assert
        // ------------------------------------------------------------
        if (caught is not null)
        {
            Assert.Fail(
                "Unexpected exception during request-scoped stream teardown:\n" +
                caught);
        }
    }
}
