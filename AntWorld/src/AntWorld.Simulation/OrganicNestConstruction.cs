using AntWorld.Domain;
using AntWorld.WorldGeneration;
namespace AntWorld.Simulation;

public sealed class OrganicNestConstruction
{
    private readonly OrganicNestPlanner planner=new();
    private readonly QueenChamberSelection queenChambers=new();
    private readonly NestNavigation navigation=new();
    private readonly ExcavationAccessGuard accessGuard=new();
    public void Step(SimulationState state,Colony colony)
    {
        var layout=colony.Layout!;var world=state.World;var plan=layout.Construction;
        RefreshJobs(state,colony,navigation.Distances(world,colony.Nest));
        PublishCompleted(world,layout,navigation.Distances(world,colony.Nest));
        queenChambers.Step(state,colony);
        var settings=world.Configuration.NestLayout;
        if(plan.HasPending)
        {
            if(NestConstructionReplanner.TryReplan(state,colony))RefreshJobs(state,colony,navigation.Distances(world,colony.Nest));
            return;
        }
        if(state.Tick-plan.LastPlanningTick<settings.PlanningIntervalTicks || !NeedsSpace(state,colony))return;
        plan.LastPlanningTick=state.Tick;
        var others=state.Colonies.Where(c=>c.Id!=colony.Id&&c.Layout is not null).Select(c=>c.Layout!).ToArray();
        var type=NeededRoom(state,colony);
        if(!planner.TryExpand(world,layout,colony.Species,type,others))
        {
            if(layout.Diagnostics.Count<64)layout.Diagnostics.Add($"Tick {state.Tick}: expansão sem espaço ou rota seca; nova avaliação após intervalo.");
            return;
        }
        Enqueue(world,layout);
        RefreshJobs(state,colony,navigation.Distances(world,colony.Nest));
    }
    private static bool NeedsSpace(SimulationState state,Colony colony)
    {
        var layout=colony.Layout!;
        var population=state.Ants.Count(a=>a.ColonyId==colony.Id&&a.IsAlive);
        var capacity=layout.Chambers.Where(c=>c.Status==NestStructureStatus.Completed).Sum(c=>c.Capacity);
        return colony.Food>=Math.Max(10,population*.25) && (population>capacity*.5 || NeededRoom(state,colony)!=NestChamberType.General);
    }
    private static NestChamberType NeededRoom(SimulationState state,Colony colony)
    {
        var layout=colony.Layout!;
        var brood=state.Ants.Count(a=>a.ColonyId==colony.Id&&a.Stage!=LifeStage.Adult);
        var broodCapacity=layout.Chambers.Where(c=>c.Status==NestStructureStatus.Completed&&c.Type==NestChamberType.Brood).Sum(c=>c.Capacity);
        if(brood>broodCapacity*.75)return NestChamberType.Brood;
        var storage=layout.Chambers.Where(c=>c.Status==NestStructureStatus.Completed&&c.Type==NestChamberType.FoodStorage).Sum(c=>c.Capacity)*8;
        return colony.Food>Math.Max(100,storage)*.9?NestChamberType.FoodStorage:NestChamberType.General;
    }
    public static void Enqueue(WorldMap world,NestLayout layout)
    {
        var jobs=layout.Construction.Jobs;var known=jobs.Select(j=>j.Target).ToHashSet();
        var cells=layout.Tunnels.Where(t=>t.Status!=NestStructureStatus.Completed).SelectMany(t=>t.Cells)
            .Concat(layout.Chambers.Where(c=>c.Status!=NestStructureStatus.Completed).SelectMany(c=>c.Cells));
        foreach(var p in cells.Distinct())
            if(world.CanDig(p)&&known.Add(p))jobs.Add(new(jobs.Count+1,p));
        layout.Construction.Revision++;
    }
    private void RefreshJobs(SimulationState state,Colony colony,IReadOnlyDictionary<int,int> reachable)
    {
        var world=state.World;var jobs=colony.Layout!.Construction.Jobs;var active=0;
        Span<CellPosition> neighbors=stackalloc CellPosition[4];
        foreach(var job in jobs)
        {
            if(job.Status==NestStructureStatus.Cancelled)continue;
            if(world.IsOpen(job.Target)){job.Status=NestStructureStatus.Completed;job.AssignedAntId=null;continue;}
            if(job.AssignedAntId is {} id && (!state.Ants.Any(a=>a.Id==id&&a.IsAlive&&a.Carrying==0&&a.CarriedAntId is null)||state.Tick-job.LastProgressTick>1200))job.AssignedAntId=null;
            job.Support=null;job.BlockedReason=null;
            if(world.GetCell(job.Target,false).WaterAmount>=200)
            {job.Status=NestStructureStatus.Blocked;job.AssignedAntId=null;job.BlockedReason="Frente inundada; aguarda drenagem ou novo acesso.";continue;}
            if(world.GetCell(job.Target,false).SolidFill<128&&!accessGuard.CanRemove(state,colony,job.Target))
            {job.Status=NestStructureStatus.WaitingDependencies;job.AssignedAntId=null;job.BlockedReason="Remoção do apoio isolaria indivíduos; construir acesso alternativo primeiro.";continue;}
            var count=world.ChunkManager.GetNeighbors(job.Target,neighbors);
            for(var i=0;i<count;i++)
                if(reachable.ContainsKey(world.Index(neighbors[i]))&&world.GetCell(neighbors[i],false).WaterAmount<200){job.Support=neighbors[i];break;}
            if(job.Support is null){job.Status=NestStructureStatus.WaitingDependencies;job.AssignedAntId=null;job.BlockedReason="Aguarda escavação de frente com apoio e rota até a rainha.";continue;}
            if(active>=world.Configuration.NestLayout.MaximumConcurrentExcavationJobs){job.Status=NestStructureStatus.WaitingDependencies;job.AssignedAntId=null;continue;}
            active++;job.Status=job.AssignedAntId.HasValue?NestStructureStatus.InProgress:NestStructureStatus.Available;
        }
    }
    public static bool FindJob(AntContext c,out CellPosition target,out CellPosition support)
    {
        target=support=default;var layout=c.Colony.Layout!;
        var jobs=layout.Construction.Jobs;
        foreach(var job in jobs.OrderBy(j=>j.AssignedAntId==c.Ant.Id?0:1).ThenBy(j=>c.State.World.Distance(c.Ant.Position,j.Target)))
        {
            if(job.Status is not (NestStructureStatus.Available or NestStructureStatus.InProgress)||job.AssignedAntId is {} id&&id!=c.Ant.Id || job.Support is not {} access)continue;
            if(!c.State.World.CanDig(job.Target)||!c.Movement.CanReach(c,access))
            {if(job.AssignedAntId==c.Ant.Id)job.AssignedAntId=null;continue;}
            if(job.AssignedAntId is null)job.LastProgressTick=c.State.Tick;
            foreach(var previous in jobs)if(previous!=job&&previous.AssignedAntId==c.Ant.Id)previous.AssignedAntId=null;
            job.AssignedAntId=c.Ant.Id;job.Status=NestStructureStatus.InProgress;
            target=job.Target;support=access;return true;
        }
        return false;
    }
    public static void RecordDig(AntContext c,CellPosition target,bool completed)
    {
        var job=c.Colony.Layout?.Construction.Jobs.FirstOrDefault(j=>j.Target==target);
        if(job is null)return;
        job.WorkActions++;job.LastProgressTick=c.State.Tick;
        if(completed){job.Status=NestStructureStatus.Completed;job.AssignedAntId=null;}
    }
    private static void PublishCompleted(WorldMap world,NestLayout layout,IReadOnlyDictionary<int,int> reachable)
    {
        var changed=false;
        for(var i=0;i<layout.Chambers.Count;i++)
        {
            var room=layout.Chambers[i];
            if(room.Status==NestStructureStatus.Completed||!room.Cells.All(world.IsOpen)||!reachable.ContainsKey(world.Index(room.Access)))continue;
            layout.Chambers[i]=room with{Status=NestStructureStatus.Completed};changed=true;
            if(room.Type==NestChamberType.Brood&&room.Access.Y>layout.Designated(layout.BroodChamberId)!.Access.Y)layout.BroodChamberId=room.Id;
        }
        for(var i=0;i<layout.Tunnels.Count;i++)
        {
            var tunnel=layout.Tunnels[i];var a=layout.Designated(tunnel.FromChamberId);var b=layout.Designated(tunnel.ToChamberId);
            if(tunnel.Status==NestStructureStatus.Completed||a is null||b is null||!tunnel.Cells.All(world.IsOpen)||!reachable.ContainsKey(world.Index(b.Access)))continue;
            layout.Tunnels[i]=tunnel with{Status=NestStructureStatus.Completed};changed=true;
        }
        if(changed)layout.Touch();
    }
}
