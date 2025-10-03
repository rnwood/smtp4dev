using Xunit;

namespace Rnwood.Smtp4dev.Tests.E2E
{
    /// <summary>
    /// Collection definition to ensure E2E tests that may use Docker with fixed ports
    /// do not run in parallel, preventing port conflicts.
    /// </summary>
    [CollectionDefinition("E2ETests", DisableParallelization = true)]
    public class E2ETestCollection
    {
        // This class is never instantiated. It just serves as a marker for the collection.
    }
}
