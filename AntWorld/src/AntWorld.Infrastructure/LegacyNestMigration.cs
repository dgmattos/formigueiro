using AntWorld.Domain;
namespace AntWorld.Infrastructure;

internal static class LegacyNestMigration
{
    public static void Apply(WorldMap world,Colony colony)
    {
        var queen=colony.Nest;var brood=colony.BroodChamber;
        var layout=new NestLayout(colony.Id){FoodStorageChamberId=1};
        layout.AddChamber(new(1,NestChamberType.Queen,queen,1,1,NestStructureStatus.Completed){Access=queen,Cells=[queen],Capacity=5});
        var path=FindPath(world,queen,brood);
        if(path.Length==0)
        {
            // Preserve the legacy geometry and behavior explicitly instead of inventing connectivity.
            return;
        }
        layout.AddChamber(new(2,NestChamberType.Brood,brood,1,1,NestStructureStatus.Completed){Access=brood,Cells=[brood],Capacity=5});
        layout.AddTunnel(new(1,1,2,path,NestStructureStatus.Completed));
        var exit=ExcavationSpoil.Exit(world,colony);var entrancePath=FindPath(world,exit,queen);
        if(entrancePath.Length>0)layout.AddEntrance(new(1,exit,1,NestStructureStatus.Completed){Path=entrancePath});
        layout.Diagnostics.Add("Migrado do formato anterior: apenas câmaras e percursos fisicamente existentes; depósito compartilhado com a rainha. Geometria original preservada.");
        colony.Layout=layout;
    }
    private static CellPosition[] FindPath(WorldMap world,CellPosition from,CellPosition to)
    {
        if(!world.CanWalk(from)||!world.CanWalk(to))return [];
        var queue=new Queue<CellPosition>();queue.Enqueue(from);var parents=new Dictionary<CellPosition,CellPosition>{{from,from}};
        Span<CellPosition> neighbors=stackalloc CellPosition[8];
        while(queue.TryDequeue(out var p)&&parents.Count<32768)
        {
            if(p==to){var path=new List<CellPosition>{to};while(p!=from){p=parents[p];path.Add(p);}path.Reverse();return path.ToArray();}
            var count=world.GetWalkableNeighbors(p,neighbors);
            for(var i=0;i<count;i++)if(parents.TryAdd(neighbors[i],p))queue.Enqueue(neighbors[i]);
        }
        return [];
    }
}
