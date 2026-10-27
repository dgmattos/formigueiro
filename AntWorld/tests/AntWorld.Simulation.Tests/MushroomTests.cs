using AntWorld.Domain;
using AntWorld.WorldGeneration;
using Xunit;
namespace AntWorld.Simulation.Tests;

public sealed class MushroomTests
{
    private static WorldMap Flat(MushroomSettings? settings = null)
    {
        var world = new WorldMap(new(42, 64, 48) { Mushrooms = settings ?? new() });
        for (var x = 0; x < world.Width; x++)
        {
            world.Surface[x] = 8;
            world.SetTerrain(new(x, 8), TerrainKind.Soil);
        }
        return world;
    }
    private static MushroomGrowth Growth(WorldMap world) => new(world.Configuration, world.Surface, p => world.GetCell(p, false), world.GetResource, world.SetResource);
    private static ResourceEntry[] Mushrooms(WorldMap world) => world.Resources().Where(e => ResourceIds.IsMushroom(e.ResourceId) && e.Stack.Quantity > 0).OrderBy(e => world.Index(e.Position)).ThenBy(e => e.ResourceId).ToArray();

    [Fact] public void ProbabilitiesUseFractionsAndOneAdjacentBonus()
    {
        var settings = new MushroomSettings();
        Assert.Equal(.1, settings.Chance(ResourceIds.RedMushroom, false, false));
        Assert.Equal(.2, settings.Chance(ResourceIds.BrownMushroom, false, false));
        Assert.Equal(.5, settings.Chance(ResourceIds.RedMushroom, true, false));
        Assert.Equal(.6, settings.Chance(ResourceIds.BrownMushroom, true, false));
        Assert.Equal(.18, settings.Chance(ResourceIds.RedMushroom, false, true), 8);
        Assert.Equal(.68, settings.Chance(ResourceIds.BrownMushroom, true, true), 8);
        Assert.Equal(1, (settings with { SurfaceRedChance = 1 }).Chance(ResourceIds.RedMushroom, true, true));
    }
    [Fact] public void BothTypesShareCapAndCanGrowOnTheSameBlock()
    {
        var world = Flat(new() { SurfaceRedChance = 1, SurfaceBrownChance = 1 });
        var region = world.ChunkManager.Region(new(0, 8)); var growth = Growth(world);
        growth.Grow(region, 0);
        Assert.Equal(30, world.CountMushrooms(region));
        Assert.Contains(Mushrooms(world).GroupBy(e => e.Position), group => group.Count() == 2);
        for (var cycle = 1; cycle <= 10; cycle++) growth.Grow(region, cycle);
        Assert.Equal(30, world.CountMushrooms(region));
        var empty = Enumerable.Range(0, 16).Select(x => new CellPosition(x, 8)).First(p => world.GetResource(p, ResourceIds.RedMushroom).Quantity == 0);
        Assert.Throws<ArgumentException>(() => world.SetResource(empty, ResourceIds.RedMushroom, new(4, 4, 10000)));
    }
    [Fact] public void MinimumIsReplenishedAfterHarvestWithoutIgnoringHabitat()
    {
        var world = Flat(new() { SurfaceRedChance = 0, SurfaceBrownChance = 0, UndergroundRedChance = 0, UndergroundBrownChance = 0, AdjacentBonus = 0 });
        var region = world.ChunkManager.Region(new(0, 8)); var growth = Growth(world);
        growth.Grow(region, 0); Assert.Equal(2, world.CountMushrooms(region));
        var first = Mushrooms(world)[0]; world.Extract(first.Position, first.ResourceId, 2);
        Assert.Equal(2, world.CountMushrooms(region));
        growth.Grow(region, 1); Assert.Equal(2, world.GetResource(first.Position, first.ResourceId).Quantity);
        foreach (var mushroom in Mushrooms(world)) world.Extract(mushroom.Position, mushroom.ResourceId, 100);
        Assert.Equal(0, world.CountMushrooms(region)); growth.Grow(region, 2);
        Assert.Equal(2, world.CountMushrooms(region));
        var airOnly = world.ChunkManager.Region(new(0, 32)); growth.Grow(airOnly, 0);
        Assert.Equal(0, world.CountMushrooms(airOnly));
    }
    [Fact] public void SoilRockWorksButPureRockBuriedAndFloodedBlocksDoNot()
    {
        var world = Flat(); var p = new CellPosition(0, 8);
        world.SetCell(p, world.GetCell(p, false) with { RockFraction = 100 });
        Assert.Equal(BlockType.SoilAndRock, world.GetCell(p, false).Type);
        world.SetResource(p, ResourceIds.RedMushroom, new(4, 4, 10000));
        world.SetTerrain(new(0, 7), TerrainKind.Soil);
        Assert.Equal(0, world.GetResource(p, ResourceIds.RedMushroom).Quantity);
        Assert.Throws<ArgumentException>(() => world.SetResource(p, ResourceIds.RedMushroom, new(4, 4, 10000)));
        world.SetTerrain(new(0, 7), TerrainKind.Air); world.SetResource(p, ResourceIds.RedMushroom, new(4, 4, 10000));
        world.AddWater(new(0, 7), 200); Assert.Equal(0, world.GetResource(p, ResourceIds.RedMushroom).Quantity);
        world.SetTerrain(new(1, 8), TerrainKind.Rock); Assert.False(MushroomHabitat.Allows(world, new(1, 8)));
        world.SetResource(new(2, 8), ResourceIds.BrownMushroom, new(4, 4, 10000)); world.Dig(new(2, 8));
        Assert.Equal(0, world.GetResource(new(2, 8), ResourceIds.BrownMushroom).Quantity);
    }
    [Fact] public void AdjacencyCrossesChunksAndCircularSeam()
    {
        var world = Flat(); var growth = Growth(world);
        world.SetResource(new(63, 8), ResourceIds.RedMushroom, new(4, 4, 10000));
        Assert.True(growth.HasAdjacent(new(0, 8)));
        world.SetResource(new(15, 8), ResourceIds.BrownMushroom, new(4, 4, 10000));
        Assert.True(growth.HasAdjacent(new(16, 8))); Assert.False(growth.HasAdjacent(new(30, 8)));
        Assert.Equal(0, world.ChunkManager.LoadedCount);
    }
    [Fact] public void ProceduralCountsAndDistributionAreIndependentOfChunkLoading()
    {
        var configuration = new WorldConfiguration(42, 96, 48);
        var a = new WorldGenerator().Generate(configuration); var b = new WorldGenerator().Generate(configuration);
        foreach (var region in a.Chunks()) a.ChunkManager.Load(new(region.X, region.Y));
        foreach (var region in b.Chunks().Reverse()) b.ChunkManager.Load(new(region.X, region.Y));
        Assert.Equal(Mushrooms(a), Mushrooms(b)); Assert.NotEmpty(Mushrooms(a));
        foreach (var region in a.Chunks())
        {
            var sites = Enumerable.Range(0, region.Width * region.Height).Count(i => MushroomHabitat.Allows(a, region.Position(i)));
            Assert.InRange(a.CountMushrooms(region), Math.Min(2, sites * 2), 30);
        }
    }
    [Fact] public void RenewalsUseSimulatedTimeAndWorkWithoutLoadingChunks()
    {
        var world = Flat(new() { RenewalIntervalSeconds = 1, SurfaceRedChance = 0, SurfaceBrownChance = 0, AdjacentBonus = 0 });
        var system = new MushroomSystem(world);
        for (var tick = 0; tick < 20; tick++) system.Step(tick);
        Assert.Empty(Mushrooms(world));
        for (var tick = 20; tick < 40; tick++) system.Step(tick);
        Assert.Equal(8, Mushrooms(world).Length); Assert.Equal(0, world.ChunkManager.LoadedCount);
    }
    [Fact] public void SettingsAreEditableAndInvalidLimitsAreRejected()
    {
        var preset = SimulationPreset.Parse("""{"world":{"mushrooms":{"minimumPerChunk":2,"maximumPerChunk":30,"renewalIntervalSeconds":60}}}""");
        Assert.Equal(60, preset.World.Mushrooms.RenewalIntervalSeconds);
        Assert.Throws<ArgumentException>(() => new MushroomSettings { MinimumPerChunk = 31 }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new MushroomSettings { AdjacentBonus = double.NaN }.Validate());
    }
    [Fact] public void SurfaceMushroomsExistInEveryBiomeAlongsidePartialTerrainEdges()
    {
        var world = new WorldGenerator().Generate(new(42));
        var surfaceMushrooms = Mushrooms(world).Where(e => e.Position.Y == world.Surface[e.Position.X]).ToArray();
        foreach (var biome in Enum.GetValues<BiomeKind>())
            Assert.Contains(surfaceMushrooms, e => world.Biomes[e.Position.X] == biome);
        Assert.Contains(Enumerable.Range(0, world.Width), x => world.GetCell(new(x, world.Surface[x]), false).Type == BlockType.AirAndSoil);
        Assert.All(surfaceMushrooms, e => Assert.Equal(BlockType.Soil, world.GetCell(e.Position, false).Type));
    }
    [Fact] public void WorkerFindsCollectsAndDepositsMushroomsWithoutDuplicatingFood()
    {
        var state = SimulationFactory.Create(new(42, 64, 48));
        var ant = state.Ants.First(a => a.Caste == AntCaste.Worker); var colony = state.Colonies[0];
        ant.WorkerRole = WorkerRole.Collector;
        // This scenario isolates resource collection; hungry brood is covered by care tests.
        foreach (var larva in state.Ants.Where(a => a.Stage == LifeStage.Larva)) larva.Energy = larva.MaxEnergy;
        var support = new CellPosition(ant.Position.X, ant.Position.Y + 1);
        state.World.ClearFood(true); state.World.SetTerrain(support, TerrainKind.Soil);
        state.World.SetResource(support, ResourceIds.RedMushroom, new(4, 4, 10000));
        var spatial = new AgentSpatialIndex(); spatial.Rebuild(state); var context = new AntContext(state, ant, colony, spatial, new());
        new DecisionSystem(new()).Decide(context); Assert.Equal(AntTask.Collect, ant.CurrentTask);
        var total = colony.Food + 4;
        Assert.True(new CollectAction().Execute(context)); Assert.Equal(ResourceIds.RedMushroom, ant.CargoResourceId);
        Assert.Equal(4, ant.Carrying); Assert.Equal(0, state.World.GetResource(support, ResourceIds.RedMushroom).Quantity);
        Assert.True(new DepositAction().Execute(context)); Assert.Equal(total, colony.Food); Assert.Equal(0, ant.Carrying);
        ant.CargoResourceId = ResourceIds.BrownMushroom; ant.Carrying = 1; ant.Energy = 10;
        Assert.True(new FeedAction().Execute(context)); Assert.Equal(.5, ant.Carrying); Assert.True(ant.Energy > 10);
    }
    [Fact] public void MaterialCollectionStillWorksAndFeedingDoesNotConsumeNonEdibleCargo()
    {
        var state = SimulationFactory.Create(new(42, 64, 48)); var colony = state.Colonies[0];
        var ant = state.Ants.First(a => a.Caste == AntCaste.Worker);
        state.World.ResourceCatalog.Register(new(99, "Material", "material", true, 0, new(31)));
        state.World.SetResource(ant.Position, 99, new(5, 5, 10000)); ant.CargoResourceId = 99;
        var index = new AgentSpatialIndex(); index.Rebuild(state); var context = new AntContext(state, ant, colony, index, new());
        Assert.True(new CollectAction().Execute(context)); Assert.Equal(4, ant.Carrying);
        ant.Energy = 10; var food = colony.Food;
        Assert.True(new FeedAction().Execute(context)); Assert.Equal(4, ant.Carrying); Assert.Equal(food - .5, colony.Food);
        Assert.False(new DepositAction().Execute(context));
    }
}
