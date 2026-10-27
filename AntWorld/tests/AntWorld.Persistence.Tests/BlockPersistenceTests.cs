using Xunit;
using AntWorld.Specifications;
namespace AntWorld.Persistence.Tests;
public sealed class BlockPersistenceTests
{
    [Fact] public void ModifiedChunksResourcesAndCatalogRoundTrip() => BlockPersistenceScenarios.ModifiedCellsAndResources();
    [Fact] public void IncrementalSizeAndExplicitOldVersionRejection() => BlockPersistenceScenarios.SeedBasedSizeAndVersion();
}
