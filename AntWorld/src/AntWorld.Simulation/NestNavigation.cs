using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class NestNavigation
{
    private readonly Dictionary<(int ColonyId, bool BroodChamber),Dictionary<int,int>> distances=[];
    private WorldMap? cachedWorld;
    private int revision=-1;
    private int vegetationRevision=-1;
    public IReadOnlyDictionary<int,int> Distances(WorldMap world,Colony colony,bool broodChamber=false)
    {
        var objects = world.Configuration.Vegetation.WoodBlocksMovement ? world.Vegetation.Revision : 0;
        if(cachedWorld!=world || revision!=world.TerrainRevision || vegetationRevision!=objects)
        { distances.Clear(); revision=world.TerrainRevision; vegetationRevision=objects; cachedWorld=world; }
        var key=(colony.Id,broodChamber);
        if(distances.TryGetValue(key,out var existing)) return existing;
        var result=new Dictionary<int,int>(); var queue=new Queue<CellPosition>();
        distances[key]=result;
        var destination=broodChamber?colony.BroodChamber:colony.Nest;
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
