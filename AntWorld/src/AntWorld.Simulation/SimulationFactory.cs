using AntWorld.Domain;
using AntWorld.WorldGeneration;
using AntWorld.Genetics;
namespace AntWorld.Simulation;

public static class SimulationFactory
{
    public static SimulationState Create(WorldConfiguration configuration, IReadOnlyList<ColonySpawnSettings>? initialColonies = null, SurvivalSettings? survival = null)
    {
        var preset = new SimulationPreset(configuration, initialColonies ?? SimulationPreset.DefaultColonies) { Survival = survival ?? new() };
        preset.Validate();
        var world = new WorldGenerator().Generate(configuration);
        var state = new SimulationState(world, DeterministicRandom.FromSeed(configuration.Seed, 4)) { Survival = preset.Survival };
        var inheritance = new GenomeInheritance();
        foreach (var spawn in preset.Colonies)
        {
            var species = spawn.SpeciesId;
            var x = (int)(world.Width * spawn.NestPositionRatio);
            var layout = NestFoundation.Create(world, state.NextColonyId, x, species, state.Colonies.Select(c=>c.Layout!).Where(n=>n is not null).ToArray());
            var nest = layout.Designated(layout.QueenChamberId)!.Access;
            var genes = species == 1 ? Genome.Moss : Genome.Ember;
            var colony = new Colony(state.NextColonyId++, species, nest, genes) { Food = spawn.InitialFood, ShaftX = x, Layout = layout };
            state.Colonies.Add(colony);
            colony.Signature=ChemicalSignature.ForColony(configuration.Seed,colony.Id,species);
            state.Pheromones[colony.Id] = new(world.CellCount);
            var queen=AntFactory.Create(state,colony,AntCaste.Queen);
            colony.Queen=queen; colony.QueenId=queen.Id; state.Ants.Add(queen);
            for (var i = 0; i < spawn.Soldiers + spawn.Workers; i++)
                state.Ants.Add(AntFactory.Create(state, colony, i < spawn.Soldiers ? AntCaste.Soldier : AntCaste.Worker,
                    genes: inheritance.Inherit(genes, state.Random), motherId: queen.Id));
            for (var i = 0; i < spawn.Larvae; i++)
                state.Ants.Add(AntFactory.Create(state, colony, stage: LifeStage.Larva, motherId: queen.Id));
        }
        return state;
    }

    public static CellPosition ExcavateNest(WorldMap world, int x, int species = 0)
    {
        // Initial/founded chambers are construction, not ant digging: rock is cleared only here.
        x = world.ChunkManager.NormalizeX(x);
        var nestY = world.Configuration.Nests.QueenY(world, x, species);
        var broodY = nestY + Colony.BroodChamberDepth;
        if (broodY >= world.Height) throw new ArgumentException("Profundidade insuficiente para o ninho e a câmara de crias.");
        for (var y = world.Surface[x] - 1; y <= broodY; y++)
        {
            var p = new CellPosition(x, y);
            world.Terrain[world.Index(p)] = TerrainKind.Soil;
            world.Dig(p);
        }
        for (var dx = -2; dx <= 2; dx++)
        {
            var p = new CellPosition(x + dx, nestY);
            world.Terrain[world.Index(p)] = TerrainKind.Soil;
            world.Dig(p);
            var brood = new CellPosition(x + dx, broodY);
            world.Terrain[world.Index(brood)] = TerrainKind.Soil;
            world.Dig(brood);
        }
        // Initial construction provides a supporting wall where the shaft crosses a natural cavern.
        for (var y = world.Surface[x]; y <= broodY; y++)
        {
            if (y == nestY || y == broodY) continue;
            if (world.ContactSide(new(x,y)) == SupportSide.None) world.SetTerrain(new(x-1,y),TerrainKind.Soil);
        }
        foreach (var y in new[] { nestY, broodY })
        for (var dx = -2; dx <= 2; dx++)
        {
            if (dx == 0 || world.ContactSide(new(x+dx,y)) != SupportSide.None) continue;
            world.SetTerrain(new(x+dx,y+1),TerrainKind.Soil);
        }
        PopulateNestMushrooms(world, x, world.Surface[x] - 1, broodY);
        return world.Normalize(new(x + 1, nestY));
    }

    private static void PopulateNestMushrooms(WorldMap world, int x, int top, int bottom)
    {
        var regions = new SortedDictionary<int, ChunkRegion>();
        for (var dx = -2; dx <= 2; dx++)
        for (var y = top; y <= Math.Min(world.Height - 1, bottom + 1); y++)
        {
            var p = new CellPosition(x + dx, y);
            regions[world.ChunkManager.ChunkId(p)] = world.ChunkManager.Region(p);
        }
        var growth = new MushroomGrowth(world.Configuration, world.Surface, p => world.GetCell(p, false), world.GetResource, world.SetResource);
        foreach (var region in regions.Values) growth.Grow(region, 0);
    }
}
