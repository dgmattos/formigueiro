using AntWorld.Domain;
using AntWorld.Infrastructure;
using AntWorld.Simulation;
using Xunit;
namespace AntWorld.Persistence.Tests;

public sealed class CircularStreamingTests
{
    [Theory]
    [InlineData(240, -20, 30)]
    [InlineData(240, 220, 270)]
    [InlineData(240, -1000, -950)]
    [InlineData(240, 970, 1020)]
    [InlineData(240, -300, 700)]
    [InlineData(256, 250, 280)]
    [InlineData(65, -20, 150)]
    public void EveryVisibleCellHasExactlyOneChunkIncludingShortLastChunk(int width, int left, int right)
    {
        var config = new WorldConfiguration(42, width, 96, 32);
        var chunks = CircularTerrainVisibility.Select(config, left, 15, right, 55, 0).ToArray();
        Assert.Equal(chunks.Length, chunks.Select(c => (c.ChunkId, c.Copy)).Distinct().Count());
        Assert.All(chunks, c => { Assert.InRange(c.Region.Width, 1, 32); Assert.InRange(c.Region.X, 0, width - 1); });
        for (var x = left; x < right; x++)
        for (var y = 15; y < 55; y++)
        {
            var match = Assert.Single(chunks, c => x >= c.OffsetX + c.Region.X && x < c.OffsetX + c.Region.X + c.Region.Width &&
                y >= c.Region.Y && y < c.Region.Y + c.Region.Height);
            Assert.Equal((int)HorizontalWrap.Normalize(x, width), x - match.OffsetX);
        }
    }
    [Fact]
    public void RepeatedViewsReuseLogicalTerrainAndPreserveExcavationsAndBiomePlan()
    {
        var state = SimulationFactory.Create(new(12345, 240, 96, 32) { MaximumLoadedChunks = 2 });
        Assert.Equal(5, state.World.Biomes.Distinct().Count());
        var world = state.World; var p = new CellPosition(239, 80);
        world.SetTerrain(p, TerrainKind.Rock); world.Dig(p);
        var codec = new BinarySnapshotCodec(); var original = codec.Encode(state);
        var chunks = CircularTerrainVisibility.Select(world.Configuration, -260, 64, 500, 96).ToArray();
        foreach (var c in chunks)
        {
            var loaded = world.ChunkManager.Load(new(c.Region.X, c.Region.Y));
            Assert.Equal(c.Region, loaded.Region);
            if (c.Region.X == 224 && c.Region.Y == 64)
                Assert.Equal(0, loaded.Cells.Span[c.Region.Index(p)].SolidFill);
            Assert.InRange(world.ChunkManager.LoadedCount, 0, 2);
        }
        Assert.Equal(original, codec.Encode(state));
        Assert.Empty(CircularTerrainVisibility.Select(world.Configuration, 0, -1000, 100, -900));
        Assert.Empty(CircularTerrainVisibility.Select(world.Configuration, 0, 1000, 100, 1100));
        Assert.Equal(world.Biomes[239], world.Biomes[world.ChunkManager.NormalizeX(-1)]);
    }
    [Fact]
    public void AgentSelectionWrapsWithoutDuplicatingPopulationAndHonorsLimit()
    {
        var state = SimulationFactory.Create(new(42, 64, 48));
        state.Performance = state.Performance with { MaxRenderedAgents = 3 };
        var original = state.Ants.Count;
        for (var i = 0; i < state.Ants.Count; i++) state.Ants[i].Position = new(i % 2 == 0 ? 63 : 0, 20);
        var selected = new List<Ant>();
        foreach (var offset in new[] { -256, 0, 256 })
        {
            Assert.Equal(3, AgentRenderSelection.Select(state, new(offset - 1, 19), new(offset + 1, 21), selected));
            Assert.Equal(3, selected.Select(a => a.Id).Distinct().Count());
            Assert.Equal(original, state.Ants.Count);
        }
        Assert.Equal(0, AgentRenderSelection.Select(state, new(20, 19), new(30, 21), selected));
        Assert.Equal(3, AgentRenderSelection.Select(state, new(-1000, 19), new(1000, 21), selected));
        Assert.Equal(63, HorizontalWrap.Normalize(-257, 64));
    }
}
