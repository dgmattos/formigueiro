using AntWorld.Domain;
using AntWorld.WorldGeneration;
using Xunit;
namespace AntWorld.Simulation.Tests;

public sealed class VegetationVisualRulesTests
{
    [Fact] public void VegetationIsDeterministicIndependentOfChunkOrderAndHasValidAnchors()
    {
        var config = new WorldConfiguration(12345, 512, 128, 32);
        var a = new WorldGenerator().Generate(config); var b = new WorldGenerator().Generate(config);
        foreach (var chunk in b.Chunks().Reverse()) b.ChunkManager.Load(new(chunk.X, chunk.Y));
        Assert.Equal(a.Vegetation.All, b.Vegetation.All); Assert.NotEmpty(a.Vegetation.All);
        Assert.Contains(a.Vegetation.All, v => v.Definition.IsTree);
        Assert.Contains(a.Vegetation.All, v => v.Kind is VegetationKind.FallenLog or VegetationKind.TreeStump or VegetationKind.DeadWood);
        Assert.Equal(a.Vegetation.All.Count(), a.Vegetation.All.Select(v => v.Base).Distinct().Count());
        foreach (var item in a.Vegetation.All)
        {
            Assert.True(VegetationGenerator.CanPlace(a, item.Base, item.Definition));
            Assert.Equal(a.ChunkManager.ChunkId(item.Base), item.OwnerChunk(a));
        }
        var trees = a.Vegetation.All.Where(v => v.Definition.IsTree).ToArray();
        for (var i = 0; i < trees.Length; i++) for (var j = i + 1; j < trees.Length; j++)
            Assert.True(Math.Abs(a.HorizontalDelta(trees[i].Base.X, trees[j].Base.X)) >= config.Vegetation.MinimumTreeSpacing);
        Assert.Equal(0, a.ChunkManager.LoadedCount);
    }
    [Fact] public void RemovalUpdatesVegetationAndDeadTreeLeavesStump()
    {
        var world = new WorldGenerator().Generate(new(12345, 512, 128));
        var tree = world.Vegetation.All.First(v => v.Definition.IsTree);
        world.Vegetation.Damage(tree.Id, 100);
        Assert.Equal(VegetationKind.TreeStump, world.Vegetation.Get(tree.Id).Kind);
        Assert.Equal(tree.Base, world.Vegetation.Get(tree.Id).Base);
        world.Dig(tree.Base); Assert.True(world.Vegetation.Get(tree.Id).Removed);
        Assert.True(world.Vegetation.Revision >= 2);
    }
    [Fact] public void GrassRequiresSkyMoistureMaterialAndNoFloodAndTracksExcavation()
    {
        var world = new WorldMap(new(42, 64, 48) { Vegetation = new() { Forest = new(GrassCoverage: 1) } });
        var p = new CellPosition(10, 10);
        world.SetTerrain(p, TerrainKind.Soil);
        world.SetCell(p, world.GetCell(p, false) with { Moisture = 60 });
        Assert.True(TerrainAppearanceResolver.HasGrass(world, p));
        Assert.Equal(TerrainVisualType.GrassCorner, TerrainAppearanceResolver.Resolve(world, p).Type);
        world.SetTerrain(new(10, 9), TerrainKind.Soil); Assert.False(TerrainAppearanceResolver.HasGrass(world, p));
        world.Dig(new(10, 9)); Assert.True(TerrainAppearanceResolver.HasGrass(world, p));
        world.AddWater(p, 200); Assert.False(TerrainAppearanceResolver.HasGrass(world, p));
        world.SetTerrain(p, TerrainKind.Sand); Assert.False(TerrainAppearanceResolver.HasGrass(world, p));
    }
    [Fact] public void GrassEdgesCrossChunkSeamAndNoRandomSlopeIsSelected()
    {
        var world = new WorldMap(new(42, 64, 48) { Vegetation = new() { Forest = new(GrassCoverage: 1) } });
        foreach (var x in new[] { 63, 0, 1 })
        {
            world.SetTerrain(new(x, 10), TerrainKind.Soil);
            world.SetCell(new(x, 10), world.GetCell(new(x, 10), false) with { Moisture = 60 });
        }
        Assert.Equal(TerrainVisualType.GrassTop, TerrainAppearanceResolver.Resolve(world, new(0, 10)).Type);
        world.Dig(new(63, 10));
        Assert.Equal(TerrainVisualType.GrassLeftEdge, TerrainAppearanceResolver.Resolve(world, new(0, 10)).Type);
        Assert.Equal(0, world.ChunkManager.LoadedCount);
    }
    [Fact] public void SeasonChangePreservesVegetationIdentityAndLogicalState()
    {
        var state = SimulationFactory.Create(new(12345, 512, 128)); var before = state.World.Vegetation.All.ToArray();
        state.Tick = SimulationState.TicksPerDay * 28L;
        Assert.Equal(1, state.Season); Assert.Equal(before, state.World.Vegetation.All);
        Assert.All(before, v => Assert.Equal(v.InitialAgeTicks + state.Tick, v.AgeAt(state.Tick)));
    }
    [Fact] public void ConfigurableWoodCollisionDoesNotDependOnItsVisualSize()
    {
        var world = new WorldGenerator().Generate(new(12345, 512, 128) { Vegetation = new() { WoodBlocksMovement = true } });
        var wood = world.Vegetation.All.First(v => v.Definition.CollisionWidth > 0);
        var p = new CellPosition(wood.Base.X, wood.Base.Y - 1);
        Assert.True(world.IsOpen(p)); Assert.False(world.CanWalk(p));
        world.Vegetation.Set(wood with { Removed = true }); Assert.True(world.CanWalk(p));
    }
    [Fact] public void RemovedVegetationSuppressesGrassUntilExplicitlyRestored()
    {
        var world = new WorldGenerator().Generate(new(12345, 512, 128));
        var p = Enumerable.Range(0, world.Width).Select(x => new CellPosition(x, world.Surface[x])).First(p => TerrainAppearanceResolver.HasGrass(world, p));
        var cell = world.GetCell(p, false);
        world.SetCell(p, cell with { Flags = cell.Flags | CellFlags.VegetationRemoved });
        Assert.False(TerrainAppearanceResolver.HasGrass(world, p));
        Assert.Equal(TerrainVisualType.Soil, TerrainAppearanceResolver.Resolve(world, p).Type);
    }
}
