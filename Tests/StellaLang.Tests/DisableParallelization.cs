using Xunit;

// Disable xUnit parallelization for the entire test assembly.
// If many tests fail unpredictably, consider disable parallelization again.
[assembly: CollectionBehavior(DisableTestParallelization = false)]
