using AntWorld.Domain;
namespace AntWorld.WorldGeneration;

internal sealed class DepositGenerator(WorldConfiguration configuration, int[] surface, Func<CellPosition, WorldCell> sample)
{
    private readonly ResourceCatalog catalog = new();
    public void Populate(ResourcePlan plan)
    {
        var settings = configuration.Geology;
        var count = Math.Max(2, configuration.Width / settings.DepositSpacing);
        for (var i = 0; i < count; i++)
        {
            Grow(plan, i * 2 + 1, ResourceIds.Iron, i, count, settings.MineralDepth, .91, settings.VeinBudget);
            Grow(plan, i * 2 + 2, ResourceIds.OrganicMatter, i, count, .01, settings.OrganicDepth, settings.VeinBudget * .35);
        }
    }
    private void Grow(ResourcePlan plan, int id, ushort resourceId, int band, int bands, double minimumDepth, double maximumDepth, double budget)
    {
        var random = DeterministicRandom.FromSeed(configuration.Seed, (uint)(100 + id));
        var x = band == 0 ? 0 : Wrap(band * configuration.Width / bands + random.NextInt(configuration.Geology.DepositSpacing / 2));
        var origin = new CellPosition(x, surface[x] + (int)((minimumDepth + random.NextDouble() * (maximumDepth - minimumDepth)) * (configuration.Height - surface[x])));
        // Find a compatible stratum without loading any chunk.
        for (var tries = 0; tries < configuration.Height && !Compatible(origin, resourceId, minimumDepth, maximumDepth); tries++)
            origin = origin with { Y = surface[x] + (origin.Y - surface[x] + 1) % (configuration.Height - surface[x]) };
        if (!Compatible(origin, resourceId, minimumDepth, maximumDepth)) return;
        var position = origin; var remaining = budget; CellPosition? allocatedOrigin = null;
        var ownCells = new HashSet<int>();
        for (var step = 0; step < configuration.Geology.VeinSteps && remaining > 0; step++)
        {
            var index = position.Y * configuration.Width + position.X;
            var key = new ResourceKey(index, resourceId);
            if (!plan.Entries.ContainsKey(key))
            {
                var amount = Math.Min(remaining, budget / configuration.Geology.VeinSteps * (.6 + random.NextDouble()));
                plan.Entries[key] = new(amount, amount, (ushort)(2500 + random.NextInt(7501)), id);
                allocatedOrigin ??= position; ownCells.Add(index); remaining -= amount;
            }
            var candidates = new[] { new CellPosition(Wrap(position.X - 1), position.Y), new(Wrap(position.X + 1), position.Y), new(position.X, position.Y - 1), new(position.X, position.Y + 1) };
            // The first step across the cylinder is a normal orthogonal edge, not a chunk boundary.
            var offset = band == 0 && step == 0 ? 0 : random.NextInt(4);
            for (var attempt = 0; attempt < 4; attempt++)
            {
                var next = candidates[(offset + attempt) % 4]; var nextIndex = next.Y * configuration.Width + next.X;
                if (!Compatible(next, resourceId, minimumDepth, maximumDepth)) continue;
                if (plan.Entries.ContainsKey(new(nextIndex, resourceId)) && !ownCells.Contains(nextIndex)) continue;
                position = next; break;
            }
        }
        if (ownCells.Count > 0) plan.Deposits.Add(new(id, resourceId, allocatedOrigin!.Value, budget, budget - remaining));
    }
    private int Wrap(int x) => (x % configuration.Width + configuration.Width) % configuration.Width;
    private bool Compatible(CellPosition p, ushort resourceId, double min, double max)
    {
        if (p.Y < 0 || p.Y >= configuration.Height) return false;
        var depth = (double)(p.Y - surface[p.X]) / (configuration.Height - surface[p.X]);
        if (depth < min || depth > max) return false;
        var cell = sample(p);
        if (!catalog.Get(resourceId).Occurrence.Allows(cell, depth)) return false;
        return resourceId == ResourceIds.Iron ? cell.RockFraction > 100 : cell.SolidFill > 0 && cell.RockFraction < 170;
    }
}
