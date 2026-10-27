using Xunit;
using AntWorld.Specifications;
namespace AntWorld.Domain.Tests;
public sealed class BlockTests
{
    [Fact] public void AutotileQueriesAreReadOnlyAndInvalidateAcrossSeam() => BlockScenarios.AutotileBoundaries();
    [Fact] public void QueriesDoNotAllocatePerCell() => BlockScenarios.NoPerCellAllocation();
    [Fact] public void FiveTypesAndCompactComposition() => BlockScenarios.PhysicalTypes();
    [Fact] public void MoistureFreeWaterAndRetention() => BlockScenarios.MoistureAndWater();
    [Fact] public void NeighborsCrossChunksAndHorizontalSeam() => BlockScenarios.Neighbors();
    [Fact] public void UnloadedQueriesAndBoundedStreaming() => BlockScenarios.UnloadedQueries();
    [Fact] public void IndependentChunkOrder() => BlockScenarios.GenerationOrder();
    [Fact] public void ConnectedDeterministicDepositsAndBudgets() => BlockScenarios.Deposits();
    [Fact] public void PartialExtractionAndExtensibleCatalog() => BlockScenarios.ExtractionAndCatalog();
    [Fact] public void ConfigurableDepthAndBiomes() => BlockScenarios.BiomesAndDepth();
}
