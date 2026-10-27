using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class MovementSystem
{
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
        return AntCapabilities.Attributes(state,ant).Speed*surfaceBonus*(.25+.75*ant.Energy/100)*
            (.3+.7*ant.Health/ant.MaxHealth)*ant.Efficiency(SkillType.Navigation)/(1+load+cell.WaterAmount/200.0+state.RainIntensity*.1);
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
        var home=destination is {} goal && !flee ? navigation.Distances(world,goal) : null;
        if(home is not null && !home.ContainsKey(world.Index(ant.Position))) return false;
        for(var i=0;i<count;i++)
        {
            var p=candidates[i]; var weight=1.0;
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
        var choice=state.Random.NextDouble()*total; var selected=count-1;
        for(var i=0;i<count;i++) { choice-=weights[i]; if(weights[i]>0 && choice<=0) { selected=i; break; } }
        ant.PreviousPosition=ant.Position; ant.Position=candidates[selected]; ant.MovementCredit-=1;
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
