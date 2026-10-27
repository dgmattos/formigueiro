using AntWorld.Domain;
namespace AntWorld.WorldGeneration;

// Read-only construction preview. Uses the same contact/corner rules as WorldMap navigation.
public sealed class NestPhysicalProjection(WorldMap world,HashSet<CellPosition> openings,HashSet<CellPosition>? retainingWalls=null)
{
    public bool Open(CellPosition p)
    {
        if(!world.Contains(p))return false;p=world.Normalize(p);
        return openings.Contains(p)||retainingWalls?.Contains(p)!=true&&world.IsOpen(p);
    }
    private bool Solid(CellPosition p)=>world.Contains(p)&&!Open(p);
    public bool Walkable(CellPosition p)=>Open(p)&&!world.Vegetation.BlocksMovement(world,p)&&
        (Solid(new(p.X-1,p.Y))||Solid(new(p.X+1,p.Y))||Solid(new(p.X,p.Y-1))||Solid(new(p.X,p.Y+1)));
    public bool Exposed(CellPosition p)
    {
        if(!Walkable(p)||!Solid(new(p.X,p.Y+1)))return false;
        for(var y=p.Y-1;y>=0;y--)if(!Open(new(p.X,y)))return false;
        return true;
    }
    public HashSet<CellPosition> Reachable(CellPosition origin,bool nestOnly=false)
    {
        var seen=new HashSet<CellPosition>();var queue=new Queue<CellPosition>();
        if(!Walkable(origin))return seen;
        seen.Add(origin);queue.Enqueue(origin);
        while(queue.TryDequeue(out var p)&&seen.Count<32768)
        for(var dy=-1;dy<=1;dy++)for(var dx=-1;dx<=1;dx++)
        {
            if(dx==0&&dy==0)continue;var next=new CellPosition(p.X+dx,p.Y+dy);
            if(!world.Contains(next))continue;next=world.Normalize(next);
            if(nestOnly&&!openings.Contains(next)||seen.Contains(next)||!Walkable(next))continue;
            if(dx!=0&&dy!=0&&!Open(new(p.X+dx,p.Y))&&!Open(new(p.X,p.Y+dy)))continue;
            seen.Add(next);queue.Enqueue(next);
        }
        return seen;
    }
}
