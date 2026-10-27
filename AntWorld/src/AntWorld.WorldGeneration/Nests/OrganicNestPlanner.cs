using AntWorld.Domain;
namespace AntWorld.WorldGeneration;

public sealed class OrganicNestPlanner
{
    public NestLayout PlanFoundation(WorldMap world,int colonyId,int x,int species,IReadOnlyCollection<NestLayout>? neighbors=null)
    {
        var others=neighbors??[];var layout=new NestLayout(colonyId);var settings=world.Configuration.NestLayout;
        x=world.ChunkManager.NormalizeX(x);
        var access=new CellPosition(x+1,world.Configuration.Nests.QueenY(world,x,species));
        var queen=Chamber(world,1,NestChamberType.Queen,world.Normalize(access),2,1);
        if(!NestGeometry.Fits(world,layout,queen,others))
        {
            NestChamber? replacement=null;
            for(var shift=0;shift<32 && replacement is null;shift++)
            for(var depth=0;depth<8 && replacement is null;depth++)
            {
                if(access.Y+depth>=world.Height-2)continue;
                var candidate=Chamber(world,1,NestChamberType.Queen,world.Normalize(new(access.X+(shift%2==0?shift/2:-shift/2),access.Y+depth)),2,1);
                if(candidate.Cells.All(world.Contains)&&NestGeometry.Fits(world,layout,candidate,others))replacement=candidate;
            }
            queen=replacement??throw new InvalidOperationException("Fundação sem espaço seco para a rainha.");
            layout.Diagnostics.Add("Âncora da rainha ajustada ao relevo/reservas vizinhas.");
        }
        layout.AddChamber(queen);
        foreach(var type in new[]{NestChamberType.Brood,NestChamberType.FoodStorage,NestChamberType.General})
            if(!TryExpand(world,layout,species,type,others,true))layout.Diagnostics.Add($"Fundação: sem espaço para {type}.");
        // Legacy fallback destinations must never pretend that an unbuilt chamber exists.
        layout.BroodChamberId=layout.Chambers.FirstOrDefault(c=>c.Type==NestChamberType.Brood)?.Id??1;
        layout.FoodStorageChamberId=layout.Chambers.FirstOrDefault(c=>c.Type==NestChamberType.FoodStorage)?.Id??1;
        var desired=Math.Min(settings.MaximumEntrances,settings.For(species).DesiredEntrances);
        foreach(var room in layout.Chambers.OrderBy(c=>c.Id==1?0:1).ThenBy(c=>c.Id))
        {
            if(layout.Entrances.Count==desired)break;
            for(var dx=0;dx<12;dx++)
            {
                var ex=world.ChunkManager.NormalizeX(room.Center.X+dx);
                var surface=new CellPosition(ex,world.Surface[ex]-1);
                if(world.Surface[world.ChunkManager.NormalizeX(ex+1)]!=world.Surface[ex])continue;
                if(layout.Entrances.Any(e=>world.Distance(e.Position,surface)<5))continue;
                // Anchor beside the opening, preserving solid ground under the external deposit point.
                var mouth=world.Normalize(new(ex+1,surface.Y));
                var path=NestTunnelPlanner.Find(world,mouth,room.Access,layout,settings.For(species),others);
                if(path.Length==0||path.Contains(new(ex,surface.Y+1)))continue;
                layout.AddEntrance(new(layout.Entrances.Count+1,surface,room.Id){Path=new[]{surface}.Concat(path).Distinct().ToArray()});break;
            }
        }
        if(layout.Entrances.Count<desired)layout.Diagnostics.Add($"Entradas viáveis: {layout.Entrances.Count}/{desired}; espaço ou geologia incompatível.");
        if(layout.Entrances.Count==0)throw new InvalidOperationException("Fundação sem entrada segura.");
        return layout;
    }

    public bool TryExpand(WorldMap world,NestLayout layout,int species,NestChamberType type=NestChamberType.General,IReadOnlyCollection<NestLayout>? neighbors=null,bool foundation=false)
    {
        var settings=world.Configuration.NestLayout;if(layout.Chambers.Count>=settings.MaximumChambers)return false;
        var others=neighbors??[];var profile=settings.For(species);
        var id=layout.Chambers.Count+1;
        var random=DeterministicRandom.FromSeed(world.Configuration.Seed,unchecked((uint)(layout.ColonyId*1009+id*7919)));
        for(var attempt=0;attempt<96;attempt++)
        {
            var parent=layout.Chambers[id<=4?0:random.NextDouble()<settings.BranchingProbability?random.NextInt(layout.Chambers.Count):layout.Chambers.Count-1];
            var rx=settings.MinimumChamberRadiusX+random.NextInt(settings.MaximumChamberRadiusX-settings.MinimumChamberRadiusX+1);
            var ry=settings.MinimumChamberRadiusY+random.NextInt(settings.MaximumChamberRadiusY-settings.MinimumChamberRadiusY+1);
            var direction=attempt%2==0?1:-1;
            var dx=direction*(parent.RadiusX+rx+settings.MinimumChamberSpacing+random.NextInt(7)+attempt/8*3);
            var dy=type==NestChamberType.Brood?4:random.NextInt(7)-3;
            if(id>4 && random.NextDouble()>profile.HorizontalBias)dy+=(int)Math.Round(profile.DepthBias*8);
            var y=parent.Access.Y+dy;
            if(type==NestChamberType.Brood&&id>4)y=Math.Max(y,Math.Min(world.Height-3,layout.Chambers.Max(c=>c.Access.Y)+3));
            if(y<ry+1 || y>=world.Height-2)continue;
            var access=world.Normalize(new(parent.Access.X+dx,y));
            var room=Chamber(world,id,type,access,rx,ry);
            if(!NestGeometry.Fits(world,layout,room,others))continue;
            // Include the candidate to protect its supporting floor while routing.
            layout.Chambers.Add(room);
            var path=NestTunnelPlanner.Find(world,parent.Access,room.Access,layout,profile,others);
            layout.Chambers.RemoveAt(layout.Chambers.Count-1);
            if(path.Length<2)continue;
            if(!foundation)
            {
                var openings=NestGeometry.Footprint(layout);openings.UnionWith(room.Cells);openings.UnionWith(path);
                var reachable=new NestPhysicalProjection(world,openings).Reachable(layout.Chambers[0].Access,true);
                if(!reachable.Contains(room.Access)||layout.Chambers.Any(c=>!reachable.Contains(c.Access)))continue;
            }
            layout.AddChamber(room);
            layout.AddTunnel(new(layout.Tunnels.Count+1,parent.Id,id,path){EstimatedCost=path.Sum(p=>1+world.GetCell(p,false).RockFraction/255.0*profile.RockCost)});
            AddAlternativeRoutes(world,layout,species,others);return true;
        }
        return false;
    }

    public void AddAlternativeRoutes(WorldMap world,NestLayout layout,int species,IReadOnlyCollection<NestLayout>? neighbors=null)
    {
        var settings=world.Configuration.NestLayout;
        if(layout.Chambers.Count<settings.MinimumChambersForLoops)return;
        var desired=(int)Math.Round((layout.Chambers.Count-1)/(1-settings.TargetAlternativeRouteRatio));
        var pairs=(from a in layout.Chambers from b in layout.Chambers where a.Id<b.Id
            where !layout.Tunnels.Any(t=>t.FromChamberId==a.Id&&t.ToChamberId==b.Id||t.FromChamberId==b.Id&&t.ToChamberId==a.Id)
            orderby world.Distance(a.Access,b.Access),a.Id,b.Id select (a,b)).ToArray();
        foreach(var(a,b)in pairs)
        {
            if(layout.Tunnels.Count>=desired)break;
            var path=NestTunnelPlanner.Find(world,a.Access,b.Access,layout,settings.For(species),neighbors??[],true);
            var used=NestGeometry.Footprint(layout);
            if(path.Length<2||path.Count(p=>!used.Contains(p))<3)continue;
            used.UnionWith(path);
            var reachable=new NestPhysicalProjection(world,used).Reachable(layout.Chambers[0].Access,true);
            if(layout.Chambers.Any(c=>!reachable.Contains(c.Access)))continue;
            layout.AddTunnel(new(layout.Tunnels.Count+1,a.Id,b.Id,path){IsAlternative=true});
        }
    }
    private static NestChamber Chamber(WorldMap world,int id,NestChamberType type,CellPosition access,int rx,int ry)
    {
        var center=new CellPosition(access.X,access.Y-ry);var cells=NestGeometry.Ellipse(world,center,rx,ry);
        return new(id,type,center,rx,ry){Access=access,Cells=cells,Capacity=cells.Length};
    }
}
