using System.Diagnostics;

namespace Performance;

/// <summary>
/// Throughput tests that measure wall-clock time for high-volume frame processing.
/// No hard timing assertions are made — results are written to the test output for
/// comparison across runs. Each test also validates that the session state is correct
/// after the run to ensure no frames were silently dropped.
/// </summary>
[TestClass]
public sealed class Layer2_Protocol_Performance
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

    internal static readonly byte[] FourBytes = [0x01, 0x02, 0x03, 0x04];

    internal const int Iterations = 10_000;
    internal const int WarmupIterations = 200;

    // ---------------------------------------------------------------
    // Helper
    // ---------------------------------------------------------------

    internal static void Report(TestContext testContext, string label, Stopwatch sw, int count)
    {
        var totalMs = sw.Elapsed.TotalMilliseconds;
        var usPerOp = sw.Elapsed.TotalMicroseconds / count;
        testContext.WriteLine($"{label}: {count:N0} iterations in {totalMs:F2} ms ({usPerOp:F2} µs/op)");
    }
}
