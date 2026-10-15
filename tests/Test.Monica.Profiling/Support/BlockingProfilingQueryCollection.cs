using Xunit;

namespace Test.Monica.Profiling.Support;

/// <summary>
/// Isolates lifecycle scenarios that deliberately park ThreadPool workers in synchronous query seams.
/// </summary>
[CollectionDefinition(NAME, DisableParallelization = true)]
public sealed class BlockingProfilingQueryCollection
{
    public const string NAME = "Blocking profiling query workers";
}
