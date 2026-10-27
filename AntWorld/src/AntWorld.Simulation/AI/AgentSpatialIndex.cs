using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class AgentSpatialIndex
{
    private readonly Dictionary<int,List<Ant>> buckets = [];
    private readonly Stack<List<Ant>> pool = [];
    public Dictionary<int,Ant> ById { get; } = [];
    public void Rebuild(SimulationState state)
    {
        foreach(var bucket in buckets.Values) { bucket.Clear(); pool.Push(bucket); }
        buckets.Clear(); ById.Clear();
        foreach(var ant in state.Ants)
        {
            if(!ant.IsAlive) continue;
            ById.Add(ant.Id,ant);
            var key=state.World.Index(ant.Position);
            if(!buckets.TryGetValue(key,out var bucket)) { bucket=pool.TryPop(out var reused)?reused:[]; buckets.Add(key,bucket); }
            bucket.Add(ant);
        }
    }
    public int Nearby(WorldMap world,CellPosition origin,Span<Ant> results,int radius=3,int offset=0)
    {
        if(radius<0 || radius>16) throw new ArgumentOutOfRangeException(nameof(radius));
        var count=0;
        // Bounded perception, including dense chambers; no all-pairs comparison.
        for(var distance=0;distance<=radius;distance++)
        for(var dy=-distance;dy<=distance;dy++)
        for(var dx=-distance;dx<=distance;dx++)
        {
            if(Math.Abs(dx)+Math.Abs(dy)!=distance) continue;
            var p=new CellPosition(origin.X+dx,origin.Y+dy);
            if(!world.Contains(p)||!buckets.TryGetValue(world.Index(p),out var bucket)) continue;
            for(var i=0;i<bucket.Count && count<results.Length;i++) results[count++]=bucket[(i+(int)((uint)offset%(uint)bucket.Count))%bucket.Count];
            if(count==results.Length) return count;
        }
        return count;
    }
}
