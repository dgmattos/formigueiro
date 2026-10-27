using AntWorld.Domain;
using AntWorld.WorldGeneration;
namespace AntWorld.Simulation;

public static class NestFoundation
{
    public static NestLayout Create(WorldMap world,int colonyId,int x,int species,IReadOnlyCollection<NestLayout>? neighbors=null,IReadOnlyCollection<CellPosition>? founders=null)
    {
        var layout=new OrganicNestPlanner().PlanFoundation(world,colonyId,x,species,neighbors);
        var footprint=NestGeometry.Footprint(layout);var shell=RetainingWalls(world,footprint);
        if(neighbors is not null) shell.ExceptWith(neighbors.SelectMany(NestGeometry.Footprint));
        var projection=new NestPhysicalProjection(world,footprint,shell);
        var reachable=projection.Reachable(layout.Chambers[0].Access);
        if(layout.Chambers.Any(c=>!reachable.Contains(c.Access)))
            throw new InvalidOperationException($"Fundação {colonyId} não possui passagem apoiada para todas as câmaras.");
        for(var i=0;i<layout.Entrances.Count;i++)
        {
            var entry=layout.Entrances[i];
            if(reachable.Contains(entry.Position)&&projection.Exposed(entry.Position))continue;
            var candidates=reachable.Where(projection.Exposed)
                .Where(p=>!layout.Entrances.Where((_,index)=>index!=i).Any(e=>e.Position==p))
                .OrderBy(p=>world.Distance(p,entry.Position)).ThenBy(p=>p.X).ToArray();
            if(candidates.Length>0&&world.Distance(candidates[0],entry.Position)<=12)
                layout.Entrances[i]=entry with{Position=candidates[0],Path=entry.Path.Append(candidates[0]).Distinct().ToArray()};
            else
            {
                layout.Entrances[i]=entry with{Status=NestStructureStatus.Blocked,Safe=false};
                layout.Diagnostics.Add($"Entrada {entry.Id} indisponível após validação do relevo.");
            }
        }
        if(!layout.Entrances.Any(e=>e.Safe&&reachable.Contains(e.Position)))throw new InvalidOperationException("Fundação sem saída apoiada para a superfície.");
        if(founders is not null && founders.Any(p=>!reachable.Contains(p)))throw new InvalidOperationException("Fundadores sem rota física até a nova colônia.");
        // Commit only after the complete preview passed. Initial retaining walls never intersect another layout.
        foreach(var p in shell)if(world.IsOpen(p))world.SetTerrain(p,TerrainKind.Soil);
        foreach(var p in footprint)world.SetTerrain(p,TerrainKind.Tunnel);
        for(var i=0;i<layout.Chambers.Count;i++)layout.Chambers[i]=layout.Chambers[i] with{Status=NestStructureStatus.Completed};
        for(var i=0;i<layout.Tunnels.Count;i++)layout.Tunnels[i]=layout.Tunnels[i] with{Status=NestStructureStatus.Completed};
        for(var i=0;i<layout.Entrances.Count;i++)if(layout.Entrances[i].Safe)layout.Entrances[i]=layout.Entrances[i] with{Status=NestStructureStatus.Completed};
        ReplenishMushrooms(world,footprint.Concat(shell));
        layout.Touch();return layout;
    }
    private static void ReplenishMushrooms(WorldMap world,IEnumerable<CellPosition> changed)
    {
        var regions=new SortedDictionary<int,ChunkRegion>();
        foreach(var p in changed)
        for(var dy=0;dy<=1;dy++)
        {
            var support=new CellPosition(p.X,p.Y+dy);if(!world.Contains(support))continue;
            regions[world.ChunkManager.ChunkId(support)]=world.ChunkManager.Region(support);
        }
        var growth=new MushroomGrowth(world.Configuration,world.Surface,p=>world.GetCell(p,false),world.GetResource,world.SetResource);
        foreach(var region in regions.Values)growth.Grow(region,0);
    }
    private static HashSet<CellPosition> RetainingWalls(WorldMap world,HashSet<CellPosition> footprint)
    {
        var shell=new HashSet<CellPosition>();
        foreach(var p in footprint)
        for(var dy=-1;dy<=1;dy++)for(var dx=-1;dx<=1;dx++)
        {
            var q=new CellPosition(p.X+dx,p.Y+dy);if(!world.Contains(q))continue;q=world.Normalize(q);
            if(q.Y>=world.Surface[q.X]&&!footprint.Contains(q))shell.Add(q);
        }
        return shell;
    }
}
