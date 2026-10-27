using AntWorld.Domain;
namespace AntWorld.WorldGeneration;

public sealed class VegetationGenerator
{
    public void Populate(WorldMap world)
    {
        var settings = world.Configuration.Vegetation;
        if (!settings.Enabled) return;
        var candidates = new List<(int X, VegetationKind Kind, double Rank)>();
        foreach (var kind in Enum.GetValues<VegetationKind>())
        for (var x = 0; x < world.Width; x++)
        {
            var profile = settings.For(world.Biomes[x]);
            var density = profile.Density(kind);
            if (kind == VegetationKind.Flower)
                density *= 1 + Math.Sin(x * Math.Tau / world.Width * 8 + world.Configuration.Seed);
            var roll = SpatialNoise.Unit(world.Configuration.Seed, x, (int)kind, 70);
            if (roll < density * BiomeProfile.For(world.Biomes[x]).Fertility)
                candidates.Add((x, kind, SpatialNoise.Unit(world.Configuration.Seed, x, (int)kind, 71)));
        }
        // Global deterministic priority selection: spacing sees neighbors across every chunk and the seam.
        candidates.Sort((a, b) =>
        {
            var group = ((int)a.Kind / 3).CompareTo((int)b.Kind / 3);
            if (group != 0) return group;
            var rank = a.Rank.CompareTo(b.Rank); if (rank != 0) return rank;
            return a.X != b.X ? a.X.CompareTo(b.X) : a.Kind.CompareTo(b.Kind);
        });
        var accepted = new List<VegetationInstance>(); var occupied = new bool[world.Width]; var trees = new List<int>();
        foreach (var candidate in candidates)
        {
            var p = new CellPosition(candidate.X, world.Surface[candidate.X]);
            var definition = VegetationDefinition.For(candidate.Kind);
            if (occupied[p.X] || !CanPlace(world, p, definition)) continue;
            var overlap = false;
            for (var dx = -definition.SupportWidth / 2; dx <= definition.SupportWidth / 2; dx++)
                overlap |= occupied[world.ChunkManager.NormalizeX(p.X + dx)];
            if (overlap) continue;
            if (definition.IsTree && trees.Any(x => Math.Abs(world.HorizontalDelta(x, p.X)) < settings.MinimumTreeSpacing)) continue;
            var id = world.Index(p) + 1;
            accepted.Add(new(id, candidate.Kind, p, world.Biomes[p.X], 0, (long)(candidate.Rank * 100 * SimulationState.TicksPerDay), 100));
            for (var dx = -definition.SupportWidth / 2; dx <= definition.SupportWidth / 2; dx++) occupied[world.ChunkManager.NormalizeX(p.X + dx)] = true;
            if (definition.IsTree) trees.Add(p.X);
        }
        world.Vegetation.Initialize(accepted.OrderBy(v => v.Id));
    }
    public static bool CanPlace(WorldMap world, CellPosition p, VegetationDefinition definition)
    {
        var settings = world.Configuration.Vegetation; var cell = world.GetCell(p, false);
        var climate = BiomeProfile.For(world.Biomes[p.X]);
        if (p.Y < definition.VisualHeight || p.Y != world.TopSolidY(p.X) || cell.SolidFill != 255 || cell.RockFraction >= 128 ||
            cell.SoilMaterial == MaterialCatalog.Sand || cell.Moisture < settings.MinimumMoisture || cell.WaterAmount >= 200 ||
            climate.Temperature < settings.MinimumTemperature || climate.Temperature > settings.MaximumTemperature) return false;
        for (var dx = -definition.SupportWidth / 2; dx <= definition.SupportWidth / 2; dx++)
        {
            var support = world.GetCell(new(p.X + dx, p.Y), false);
            if (world.TopSolidY(p.X + dx) != p.Y || support.SolidFill != 255 || support.RockFraction >= 128 || support.WaterAmount >= 200) return false;
        }
        for (var dy = 1; dy <= definition.VisualHeight; dy++)
        for (var dx = -definition.VisualWidth / 2; dx <= definition.VisualWidth / 2; dx++)
            if (!world.IsOpen(new(p.X + dx, p.Y - dy))) return false;
        return true;
    }
}
