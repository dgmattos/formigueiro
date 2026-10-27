using AntWorld.Domain;
namespace AntWorld.WorldGeneration;

public sealed class ResourceDistributor
{
    internal ResourcePlan CreatePlan(WorldConfiguration configuration, BiomeKind[] biomes, int[] surface, Func<CellPosition, WorldCell> sample)
    {
        var plan = new ResourcePlan();
        for (var x = 0; x < configuration.Width; x++)
        {
            var profile = BiomeProfile.For(biomes[x]);
            if (SpatialNoise.Unit(configuration.Seed, x, 0, 3) < profile.ResourceDensity)
            {
                var capacity = (20 + SpatialNoise.Unit(configuration.Seed, x, 0, 4) * 30) * profile.Fertility;
                plan.Entries[new((surface[x] - 1) * configuration.Width + x, ResourceIds.Food)] = new(capacity * .75, capacity, 10000);
            }
        }
        PopulateCavernFood(plan, configuration, biomes, surface, sample);
        new DepositGenerator(configuration, surface, sample).Populate(plan);
        var growth = new MushroomGrowth(configuration, surface, sample,
            (p, id) => plan.Entries.GetValueOrDefault(new(p.Y * configuration.Width + p.X, id)),
            (p, id, stack) => plan.Entries[new(p.Y * configuration.Width + p.X, id)] = stack);
        // One canonical baseline, independent of chunk loading/render order.
        var size = configuration.ChunkSize;
        for (var y = 0; y < configuration.Height; y += size)
        for (var x = 0; x < configuration.Width; x += size)
            growth.Grow(new(x, y, Math.Min(size, configuration.Width - x), Math.Min(size, configuration.Height - y)), 0);
        return plan;
    }
    private static void PopulateCavernFood(ResourcePlan plan, WorldConfiguration c, BiomeKind[] biomes, int[] surface, Func<CellPosition, WorldCell> sample)
    {
        var catalog = new ResourceCatalog();
        if (c.Geology.CaveStart >= .95) return;
        for (var x = 0; x < c.Width; x++)
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var humidity = BiomeProfile.For(biomes[x]).Humidity;
            if (SpatialNoise.Unit(c.Seed, x, attempt, 31) > humidity) continue;
            var depth = c.Geology.CaveStart + SpatialNoise.Unit(c.Seed, x, attempt, 32) * (.95 - c.Geology.CaveStart);
            var y = surface[x] + (int)(depth * (c.Height - surface[x]));
            if (sample(new(x, y)).SolidFill != 0) continue;
            while (y < c.Height - 2 && sample(new(x, y + 1)).SolidFill == 0) y++;
            var capacity = 8 + SpatialNoise.Unit(c.Seed, x, y, 33) * 16;
            plan.Entries[new(y * c.Width + x, ResourceIds.Food)] = new(capacity * .75, capacity, 10000);
            var substrate = sample(new(x, y + 1));
            var substrateDepth = (double)(y + 1 - surface[x]) / (c.Height - surface[x]);
            if (catalog.Get(ResourceIds.Fungus).Occurrence.Allows(substrate, substrateDepth))
                plan.Entries[new((y + 1) * c.Width + x, ResourceIds.Fungus)] = new(capacity * .5, capacity, 8000);
        }
    }

}
