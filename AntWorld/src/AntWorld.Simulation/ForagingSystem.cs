using AntWorld.Domain;
namespace AntWorld.Simulation;

public sealed class ForagingSystem
{
    private const int SearchBudget = 4096;
    private readonly Queue<(CellPosition Position, int Depth)> queue = new();
    private readonly HashSet<int> seen = [];

    public bool HasKnownFood(AntContext c,CellPosition? position) => position is {} p &&
        c.Colony.Foraging.Food.Contains(c.State.World.Index(p)) && FoodSources.AmountAt(c.State.World,p)>0;

    public CellPosition? Destination(AntContext c)
    {
        Observe(c);
        var memory = c.Colony.Foraging;
        var ant = c.Ant; var world = c.State.World;
        // Commit to the selected path; decisions at 2 Hz must not reset progress between movement steps.
        if (ant.CurrentTask == AntTask.Explore && ant.Destination is {} current && current != ant.Position &&
            world.CanWalk(current) && c.Movement.CanReach(c,current) &&
            (!memory.Food.Contains(world.Index(current)) || FoodSources.AmountAt(world,current) >= 1)) return current;
        CellPosition? food = null; var bestValue = 0.0;
        IReadOnlyDictionary<int,int>? outwardDistances = null, homeDistances = null;
        foreach (var index in memory.Food.Order())
        {
            var p = world.Position(index);
            var amount = Math.Min(Math.Min(4,AntCapabilities.Capacity(c.State,ant)),FoodSources.AmountAt(world,p));
            if (amount <= 0 || !world.CanWalk(p)) continue;
            // Two shared distance fields evaluate every known source, instead of one BFS per source.
            outwardDistances ??= c.Movement.DistancesFrom(world,ant.Position);
            homeDistances ??= c.Movement.DistancesFrom(world,c.Colony.FoodStorage);
            var outward = outwardDistances.GetValueOrDefault(index,-1);
            var returning = homeDistances.GetValueOrDefault(index,-1);
            if (outward < 0 || returning < 0) continue;
            var value = amount / (1 + outward + returning);
            if (value > bestValue) { bestValue = value; food = p; }
        }
        if (food.HasValue) return food;
        return Frontier(c);
    }

    public static double HarvestQuality(AntContext c)
    {
        if (!FoodSources.TryCollectible(c.State.World,c.Ant,out var p,out var type,preferLargest:true)) return 0;
        var batch = Math.Max(.01,Math.Min(4,AntCapabilities.Capacity(c.State,c.Ant)));
        return .25 + .75 * Math.Min(1,c.State.World.GetResource(p,type).Quantity / batch);
    }

    private void Observe(AntContext c)
    {
        var world = c.State.World; var memory = c.Colony.Foraging;
        queue.Clear(); seen.Clear(); queue.Enqueue((c.Ant.Position,0)); seen.Add(world.Index(c.Ant.Position));
        Span<CellPosition> neighbors = stackalloc CellPosition[8];
        while (queue.TryDequeue(out var item))
        {
            var (p,depth) = item;
            memory.Observe(world.Index(p), c.State.Tick, p == c.Ant.Position, FoodSources.AmountAt(world,p) > 0);
            if (depth == 4) continue;
            var count = world.GetWalkableNeighbors(p,neighbors);
            for (var i=0;i<count;i++)
                if (seen.Add(world.Index(neighbors[i]))) queue.Enqueue((neighbors[i],depth+1));
        }
    }

    private CellPosition? Frontier(AntContext c)
    {
        var world = c.State.World; var memory = c.Colony.Foraging;
        queue.Clear(); seen.Clear(); queue.Enqueue((c.Ant.Position,0)); seen.Add(world.Index(c.Ant.Position));
        CellPosition? oldest = null; var oldestTick = long.MaxValue;
        Span<CellPosition> neighbors = stackalloc CellPosition[8];
        while (queue.TryDequeue(out var item))
        {
            var (p,depth) = item;
            var index = world.Index(p);
            if (p != c.Ant.Position && memory.Visits.TryGetValue(index,out var visited))
            {
                if (visited < 0) return p;
                if (visited < oldestTick) { oldest = p; oldestTick = visited; }
            }
            var count = world.GetWalkableNeighbors(p,neighbors);
            for (var i=0;i<count && seen.Count<SearchBudget;i++)
            {
                var next = neighbors[(i+c.Ant.Id)%count]; var nextIndex = world.Index(next);
                if (memory.Visits.ContainsKey(nextIndex) && seen.Add(nextIndex)) queue.Enqueue((next,depth+1));
            }
        }
        // Revisiting the oldest known corridor allows discovery of renewed mushrooms and new excavations.
        return oldest;
    }
}
