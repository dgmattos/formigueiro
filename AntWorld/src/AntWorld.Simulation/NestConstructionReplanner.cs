using AntWorld.Domain;
using AntWorld.WorldGeneration;
namespace AntWorld.Simulation;

// Replace a blocked plan without undoing excavated terrain or spending biological randomness.
public static class NestConstructionReplanner
{
    public static bool TryReplan(SimulationState state,Colony colony)
    {
        var world=state.World;var layout=colony.Layout!;var plan=layout.Construction;
        if(state.Tick-plan.LastPlanningTick<world.Configuration.NestLayout.PlanningIntervalTicks)return false;
        var stalled=plan.Jobs.Where(j=>j.Status is NestStructureStatus.Blocked or NestStructureStatus.WaitingDependencies && j.BlockedReason is not null)
            .Select(j=>j.Target).ToHashSet();
        if(stalled.Count==0)return false;
        // A healthy frontier is allowed to satisfy ordinary dependencies before attempting a new route.
        if(!plan.Jobs.Any(j=>j.Status==NestStructureStatus.Blocked)&&plan.Jobs.Any(j=>j.Status is NestStructureStatus.Available or NestStructureStatus.InProgress))return false;
        plan.LastPlanningTick=state.Tick;
        var others=state.Colonies.Where(c=>c.Id!=colony.Id&&c.Layout is not null).Select(c=>c.Layout!).ToArray();
        for(var i=0;i<layout.Tunnels.Count;i++)
        {
            var old=layout.Tunnels[i];
            if(old.Status is NestStructureStatus.Completed or NestStructureStatus.Cancelled||!old.Cells.Any(stalled.Contains))continue;
            var from=layout.Chambers.Single(c=>c.Id==old.FromChamberId);var to=layout.Chambers.Single(c=>c.Id==old.ToChamberId);
            var path=NestTunnelPlanner.Find(world,from.Access,to.Access,layout,world.Configuration.NestLayout.For(colony.Species),others,true);
            if(path.Length<2||path.SequenceEqual(old.Path))continue;
            var candidate=old with{Path=path,Cells=path,Status=NestStructureStatus.Planned};
            layout.Tunnels[i]=candidate;
            var reachable=new NestPhysicalProjection(world,NestGeometry.Footprint(layout)).Reachable(colony.Nest,true);
            if(layout.Chambers.Any(c=>!reachable.Contains(c.Access))){layout.Tunnels[i]=old;continue;}
            Reconcile(world,layout);
            if(layout.Diagnostics.Count<64)layout.Diagnostics.Add($"Tick {state.Tick}: túnel {old.Id} replanejado; escavações anteriores preservadas.");
            layout.Touch();return true;
        }
        if(layout.Diagnostics.Count<64)layout.Diagnostics.Add($"Tick {state.Tick}: nenhuma alternativa segura para a frente bloqueada; aguarda mudança ambiental ou acesso.");
        return false;
    }
    private static void Reconcile(WorldMap world,NestLayout layout)
    {
        var required=layout.Tunnels.Where(t=>t.Status!=NestStructureStatus.Completed).SelectMany(t=>t.Cells)
            .Concat(layout.Chambers.Where(c=>c.Status!=NestStructureStatus.Completed).SelectMany(c=>c.Cells)).Where(world.CanDig).ToHashSet();
        foreach(var job in layout.Construction.Jobs)
        {
            if(job.Status==NestStructureStatus.Completed)continue;
            if(!required.Contains(job.Target))
            {
                job.Status=NestStructureStatus.Cancelled;job.AssignedAntId=null;job.Support=null;job.BlockedReason="Substituído pelo novo percurso.";
            }
            else if(job.Status==NestStructureStatus.Cancelled){job.Status=NestStructureStatus.WaitingDependencies;job.BlockedReason=null;}
        }
        OrganicNestConstruction.Enqueue(world,layout);
    }
}
