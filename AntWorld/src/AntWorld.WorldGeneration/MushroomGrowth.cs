using AntWorld.Domain;
namespace AntWorld.WorldGeneration;

public sealed class MushroomGrowth(
    WorldConfiguration configuration, int[] surface,
    Func<CellPosition, WorldCell> sample,
    Func<CellPosition, ushort, ResourceStack> resource,
    Action<CellPosition, ushort, ResourceStack> store)
{
    private readonly List<(CellPosition Position, ushort Type)> vacant = [];

    public void Grow(ChunkRegion region, long cycle)
    {
        var settings = configuration.Mushrooms;
        var count = FindVacancies(region);
        if (count >= settings.MaximumPerChunk || vacant.Count == 0) return;
        // A separate deterministic stream does not disturb agent decisions or require a saved cursor.
        var seed = unchecked((ulong)(uint)configuration.Seed ^ (ulong)(region.Y * configuration.Width + region.X) * 0x9E3779B97F4A7C15UL ^ (ulong)cycle * 0xD1B54A32D192ED03UL);
        var random = new DeterministicRandom(seed);
        for (var i = vacant.Count - 1; i > 0; i--)
        {
            var j = random.NextInt(i + 1);
            (vacant[i], vacant[j]) = (vacant[j], vacant[i]);
        }
        foreach (var candidate in vacant)
        {
            if (count >= settings.MaximumPerChunk) break;
            var p = candidate.Position;
            if (random.NextDouble() >= settings.Chance(candidate.Type, p.Y <= surface[p.X], HasAdjacent(p))) continue;
            Spawn(p, candidate.Type); count++;
        }
        // The minimum overrides failed rolls, but never overrides substrate or available space.
        foreach (var candidate in vacant)
        {
            if (count >= settings.MinimumPerChunk) break;
            if (resource(candidate.Position, candidate.Type).Quantity > 0) continue;
            Spawn(candidate.Position, candidate.Type); count++;
        }
    }

    private int FindVacancies(ChunkRegion region)
    {
        vacant.Clear(); var count = 0;
        for (var i = 0; i < region.Width * region.Height; i++)
        {
            var p = region.Position(i);
            var valid = p.Y > 0 && MushroomHabitat.Allows(sample(p), sample(new(p.X, p.Y - 1)));
            for (var id = ResourceIds.RedMushroom; id <= ResourceIds.BrownMushroom; id++)
            {
                var stack = resource(p, id);
                if (!valid)
                {
                    if (stack.Capacity > 0) store(p, id, default);
                }
                else if (stack.Quantity > 0) count++;
                else vacant.Add((p, id));
            }
        }
        return count;
    }

    public bool HasAdjacent(CellPosition p)
    {
        ReadOnlySpan<CellPosition> offsets = [new(-1, 0), new(1, 0), new(0, -1), new(0, 1)];
        foreach (var offset in offsets)
        {
            var y = p.Y + offset.Y;
            if (y < 0 || y >= configuration.Height) continue;
            var next = new CellPosition((p.X + offset.X + configuration.Width) % configuration.Width, y);
            if (resource(next, ResourceIds.RedMushroom).Quantity > 0 || resource(next, ResourceIds.BrownMushroom).Quantity > 0) return true;
        }
        return false;
    }

    private void Spawn(CellPosition p, ushort type) =>
        store(p, type, new(configuration.Mushrooms.FoodPerMushroom, configuration.Mushrooms.FoodPerMushroom, 10000));
}
