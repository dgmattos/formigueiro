using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class MovementSystem
{
    public const double GlobalSpeedMultiplier = .8;
    public const double VerticalSpeedMultiplier = .65;
    public const double AccelerationSeconds = .5;
    public const double BrakingSeconds = .2;
    public static double StepCost(CellPosition from,CellPosition to) => from.Y==to.Y?1:1/VerticalSpeedMultiplier;
    public void AdvanceKinematics(SimulationState state,Ant ant)
    {
        var maximum=EffectiveSpeed(state,ant);
        if(maximum==0){ant.MovementSpeed=0;ant.MovementCredit=0;return;}
        var moving=ant.IsAlive&&ant.CarrierId is null&&state.World.CanWalk(ant.Position)&&
            (ant.CurrentTask is AntTask.Move or AntTask.Explore or AntTask.Carry or AntTask.Flee) &&
            (ant.Destination!=ant.Position || ant.CurrentTask==AntTask.Flee) &&
            (ant.Destination is not {} goal || state.World.CanWalk(goal) || ant.CurrentTask==AntTask.Flee);
        var target=moving?maximum:0;
        var change=Math.Max(maximum,ant.MovementSpeed)*SimulationState.TickSeconds/(target>ant.MovementSpeed?AccelerationSeconds:BrakingSeconds);
        ant.MovementSpeed+=Math.Clamp(target-ant.MovementSpeed,-change,change);
        if(!moving||maximum==0){ant.MovementCredit=0;return;}
        ant.MovementCredit=Math.Min(2,ant.MovementCredit+ant.MovementSpeed*SimulationState.TickSeconds);
    }
    private readonly NestNavigation navigation=new();
    public bool CanReachBroodChamber(AntContext c) =>
        navigation.Distances(c.State.World,c.Colony,true).ContainsKey(c.State.World.Index(c.Ant.Position));
    public IReadOnlyDictionary<int,int> DistancesFrom(WorldMap world, CellPosition origin) => navigation.Distances(world,origin);
    public int DistanceTo(WorldMap world, CellPosition origin, CellPosition destination) =>
        navigation.Distances(world,destination).GetValueOrDefault(world.Index(origin),-1);
    public bool CanReach(AntContext c, CellPosition destination) => DistanceTo(c.State.World,c.Ant.Position,destination)>=0;
    public double EffectiveSpeed(SimulationState state,Ant ant)
    {
        if(!AntCapabilities.Has(state,ant,AntCapability.Move)) return 0;
        var capacity=AntCapabilities.Capacity(state,ant);
        var load=capacity>0 ? (ant.Carrying+(ant.CarriedAntId.HasValue?2:0))/capacity : 0;
        var cell=state.World.GetCell(ant.Position,false);
        var species = state.Species[ant.SpeciesId];
        var surfaceBonus = species.SurfaceMovementMultiplier != 1 && state.World.IsOnExposedSurface(ant.Position)
            ? species.SurfaceMovementMultiplier : 1;
        return GlobalSpeedMultiplier*AntCapabilities.Attributes(state,ant).Speed*surfaceBonus*(.25+.75*ant.Energy/100)*
            (.3+.7*ant.Health/ant.MaxHealth)*ant.Efficiency(SkillType.Navigation)/(1+load+cell.WaterAmount/200.0+state.RainIntensity*.1);
    }
    private IReadOnlyDictionary<int,int> EscortDistances(AntContext c,CellPosition goal)
    {
        if(!c.Ant.QueenEscort || c.Colony.Queen is not {IsAlive:true} queen)return navigation.Distances(c.State.World,goal);
        var world=c.State.World;
        var radius=Math.Max(QueenEscortSystem.MaximumRadius,world.Distance(c.Ant.Position,queen.Position));
        var distances=new Dictionary<int,int>();var queue=new Queue<CellPosition>();
        distances[world.Index(goal)]=0;queue.Enqueue(goal);
        Span<CellPosition> neighbors=stackalloc CellPosition[8];
        while(queue.TryDequeue(out var current) && distances.Count<32768)
        {
            var count=world.GetWalkableNeighbors(current,neighbors);
            for(var i=0;i<count;i++)
            {
                var next=neighbors[i];
                if(world.Distance(next,queen.Position)>radius)continue;
                if(distances.TryAdd(world.Index(next),distances[world.Index(current)]+1))queue.Enqueue(next);
            }
        }
        return distances;
    }
    public bool Move(AntContext c,bool explore=false,bool flee=false)
    {
        var ant=c.Ant; var state=c.State; var world=state.World;
        if(!world.CanWalk(ant.Position) || ant.MovementCredit<1 || ant.Destination == ant.Position && !flee) return false;
        Span<CellPosition> candidates=stackalloc CellPosition[8]; Span<double> weights=stackalloc double[8];
        var count=world.GetWalkableNeighbors(ant.Position,candidates);
        if(count==0) return false;
        var total=0.0;
        var destination=ant.Destination;
        if (destination is {} invalid && !world.CanWalk(invalid) && !flee) return false;
        var home=destination is {} goal && !flee ? EscortDistances(c,goal) : null;
        if(home is not null && !home.ContainsKey(world.Index(ant.Position))) return false;
        for(var i=0;i<count;i++)
        {
            var p=candidates[i]; var weight=1.0;
            if(ant.QueenEscort && c.Colony.Queen is {IsAlive:true} queen &&
                world.Distance(p,queen.Position)>Math.Max(QueenEscortSystem.MaximumRadius,world.Distance(ant.Position,queen.Position)))
            {weights[i]=0;continue;}
            if(destination is {} target)
            {
                var before=home?.GetValueOrDefault(world.Index(ant.Position),int.MaxValue)??world.Distance(ant.Position,target);
                var after=home?.GetValueOrDefault(world.Index(p),int.MaxValue)??world.Distance(p,target);
                // A selected route must make progress. Food smells cannot pull a returning carrier backwards.
                if (home is not null && after >= before) { weights[i] = 0; continue; }
                weight*=after<before?12:.05;
                if(home is not null) weight*=1+state.Pheromones[ant.ColonyId].Get(PheromoneType.HomeTrail,world.Index(p))*.05;
            }
            if(flee && c.Target is {} threat) weight*=world.Distance(p,threat.Position)>world.Distance(ant.Position,threat.Position)?20:.02;
            if(explore)
            {
                var trail=state.Pheromones[ant.ColonyId];
                weight*=1+trail.Get(PheromoneType.FoodTrail,world.Index(p))*.6;
                var profile=state.Species[ant.SpeciesId].Behaviors[(int)ant.Caste];
                var alarm=trail.Get(PheromoneType.Alarm,world.Index(p));
                weight*=profile.Defense>profile.Foraging ? 1+alarm*.7 : 1/(1+alarm*(1-ant.Genes.Aggression));
                if(profile.Defense>profile.Foraging) weight*=1+trail.Get(PheromoneType.Territory,world.Index(p))*.1;
                weight*=1+FoodSources.AmountAt(world,p)*2;
            }
            if(p==ant.PreviousPosition) weight*=.15;
            weights[i]=weight; total+=weight;
        }
        if (total <= 0) return false;
        var choice=state.Random.NextDouble()*total; var selected=0;
        for(var i=0;i<count;i++)if(weights[i]>0)selected=i;
        for(var i=0;i<count;i++) { choice-=weights[i]; if(weights[i]>0 && choice<=0) { selected=i; break; } }
        var cost=StepCost(ant.Position,candidates[selected]);
        if(ant.MovementCredit<cost)return false;
        ant.PreviousPosition=ant.Position; ant.Position=candidates[selected]; ant.MovementCredit-=cost;
        ant.Support=world.ContactSide(ant.Position,ant.Support);
        ant.Movement=flee?MovementState.Fleeing:ant.Carrying>0||ant.CarriedAntId.HasValue?MovementState.Carrying:
            ant.Position.Y!=ant.PreviousPosition.Y?MovementState.Climbing:MovementState.Walking;
        if (ant.Caste == AntCaste.Worker && ant.WorkerRole != WorkerRole.Digger)
            c.Colony.Foraging.Observe(world.Index(ant.Position),state.Tick,true,FoodSources.AmountAt(world,ant.Position)>0);
        var field=state.Pheromones[ant.ColonyId]; var index=world.Index(ant.Position);
        field.Deposit(PheromoneType.HomeTrail,index,.1);
        if(ant.Carrying>0 && ResourceIds.IsEdible(ant.CargoResourceId)) field.Deposit(PheromoneType.FoodTrail,index,Math.Min(2,ant.Carrying*.4));
        if(AntCapabilities.Has(state,ant,AntCapability.Attack)) field.Deposit(PheromoneType.Territory,index,.2);
        return true;
    }
}
