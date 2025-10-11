using Xunit;

// Disable xUnit parallelization for the entire test assembly.
// Many tests redirect Console.Out and Console.SetOut; running tests in parallel
// can cause intermittent failures when multiple tests attempt to capture output.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
