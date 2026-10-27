using AntWorld.Domain;
namespace AntWorld.Simulation;

// Bounded perception search; reused buffers, circular coordinates, no chunk generation.
public sealed class WaterSearch
{
    private readonly CellPosition[] queue = new CellPosition[128];
    private readonly int[] parents = new int[128];
    private readonly HashSet<int> visited = new(128);
    public CellPosition? NextStep(WorldMap world, CellPosition origin)
    {
        visited.Clear(); origin = world.Normalize(origin);
        queue[0] = origin; parents[0] = -1; visited.Add(world.Index(origin));
        var count = 1;
        Span<CellPosition> neighbors = stackalloc CellPosition[8];
        for (var head = 0; head < count; head++)
        {
            var current = queue[head];
            if (WaterSources.TryAccessible(world, current, out _))
            {
                var step = head;
                while (parents[step] > 0) step = parents[step];
                return queue[step];
            }
            var n = world.GetWalkableNeighbors(current, neighbors);
            for (var i = 0; i < n && count < queue.Length; i++)
            {
                var next = neighbors[i];
                if (world.Distance(origin, next) > 8 || !visited.Add(world.Index(next))) continue;
                queue[count] = next; parents[count] = head; count++;
            }
        }
        return null;
    }
}
