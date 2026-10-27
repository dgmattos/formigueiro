using AntWorld.Domain;
namespace AntWorld.WorldGeneration;

// Bounded weighted A*: no terrain mutation, chunk loading or simulation PRNG consumption.
public static class NestTunnelPlanner
{
    public static CellPosition[] Find(WorldMap world,CellPosition start,CellPosition end,NestLayout layout,
        NestSpeciesProfile profile,IReadOnlyCollection<NestLayout> others,bool preferNew=false)
    {
        start=world.Normalize(start);end=world.Normalize(end);
        var protectedCells=layout.Chambers.Select(c=>world.Normalize(new(c.Access.X,c.Access.Y+1))).ToHashSet();
        var forbidden=NestGeometry.Reservations(world,others);
        var existing=preferNew?layout.Tunnels.SelectMany(t=>t.Cells).ToHashSet():[];
        var queue=new PriorityQueue<CellPosition,(double,int)>();var costs=new Dictionary<CellPosition,double>{{start,0}};
        var parents=new Dictionary<CellPosition,CellPosition>(); var sequence=0;
        queue.Enqueue(start,(world.Distance(start,end),sequence++));
        ReadOnlySpan<CellPosition> offsets=[new(1,0),new(-1,0),new(0,1),new(0,-1)];
        while(queue.TryDequeue(out var p,out _) && costs.Count<=8192)
        {
            if(p==end)
            {
                var path=new List<CellPosition>{end};
                while(p!=start){p=parents[p];path.Add(p);}path.Reverse();return path.ToArray();
            }
            foreach(var offset in offsets)
            {
                var q=new CellPosition(p.X+offset.X,p.Y+offset.Y);
                if(!world.Contains(q)||q.Y>=world.Height-1)continue;
                q=world.Normalize(q);
                if((q.Y<world.Surface[q.X] && q!=start && q!=end) || protectedCells.Contains(q)||forbidden.Contains(q))continue;
                var cell=world.GetCell(q,false);if(cell.WaterAmount>=200)continue;
                var noise=unchecked((uint)(q.X*73856093^q.Y*19349663^world.Configuration.Seed));
                var cost=costs[p]+1+cell.RockFraction/255.0*profile.RockCost*.2+(noise%11)*.025+(existing.Contains(q)?4:0);
                if(costs.TryGetValue(q,out var old)&&old<=cost)continue;
                costs[q]=cost;parents[q]=p;queue.Enqueue(q,(cost+world.Distance(q,end),sequence++));
            }
        }
        return [];
    }
}
