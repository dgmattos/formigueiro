using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class NestNavigation
{
    private readonly Dictionary<int,Dictionary<int,int>> distances=[];
    private WorldMap? cachedWorld;
    private int revision=-1;
    private int vegetationRevision=-1;
    public IReadOnlyDictionary<int,int> Distances(WorldMap world,Colony colony,bool broodChamber=false)
        => Distances(world, broodChamber ? colony.BroodChamber : colony.Nest);
    public IReadOnlyDictionary<int,int> Distances(WorldMap world, CellPosition destination)
    {
        var objects = world.Configuration.Vegetation.WoodBlocksMovement ? world.Vegetation.Revision : 0;
        if(cachedWorld!=world || revision!=world.NavigationRevision || vegetationRevision!=objects)
        { distances.Clear(); revision=world.NavigationRevision; vegetationRevision=objects; cachedWorld=world; }
        var key=world.Index(destination);
        if(distances.TryGetValue(key,out var existing)) return existing;
        if (distances.Count >= 32) distances.Clear();
        var result=new Dictionary<int,int>(); var queue=new Queue<CellPosition>();
        distances[key]=result;
        if(!world.CanWalk(destination)) return result;
        result[world.Index(destination)]=0; queue.Enqueue(destination);
        Span<CellPosition> neighbors=stackalloc CellPosition[8];
        while(queue.TryDequeue(out var current) && result.Count<32768)
        {
            var count=world.GetWalkableNeighbors(current,neighbors);
            for(var i=0;i<count;i++)
            {
                var next=neighbors[i]; var index=world.Index(next);
                if(result.TryAdd(index,result[world.Index(current)]+1)) queue.Enqueue(next);
            }
        }
        return result;
    }
}
