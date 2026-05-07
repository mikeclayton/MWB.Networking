namespace Layer2_Protocol;

[TestClass]
public sealed partial class Pipes
{
    public TestContext TestContext
    {
        get;
        set;
    }

    //[AssemblyInitialize]
    //public static void AssemblyInit(TestContext context)
    //{
    //    TaskScheduler.UnobservedTaskException += (sender, e) =>
    //    {
    //        // Break into debugger immediately
    //        Debugger.Break();

    //        // Or force test run to fail hard
    //        e.SetObserved();
    //        throw e.Exception;
    //    };
    //}

    [TestCleanup]
    public void Cleanup()
    {
        // force any unobserved exceptions from finalizers to surface during
        // test runs rather than being silently ignored - this makes it easier
        // to determine *which* test caused the issue (and fix it!).
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }    
}
