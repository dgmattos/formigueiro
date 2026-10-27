using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class SimulationEngine
{
    public SimulationState State { get; }
    public AgentScheduler Scheduler { get; }=new();
    public List<AntEvent> Events { get; }=[];
    public event Action<AntDeath>? AntDied;
    private readonly EnvironmentSystem environment;
    private readonly MushroomSystem mushrooms;
    private readonly AntBiologySystem biology=new();
    private readonly AgentSpatialIndex spatial=new();
    private readonly ColonyAiSystem colonyAi=new();
    private readonly ColonySystem colonies=new();
    private readonly ColonyExpansionSystem expansion=new();
    private readonly QueenEscortSystem escorts=new();
    private readonly NurseryChamberSelection nursery=new();
    private readonly ActionCatalog actions=new();
    private readonly MovementSystem movement=new();
    private readonly AntBehaviorSystem behavior;
    private readonly DecisionSystem decision;
    private readonly Dictionary<int,Colony> colonyIndex=[];
    public SimulationEngine(SimulationState state)
    {
        state.Survival.Validate(); state.Performance.Validate(); foreach(var species in state.Species.Values) species.Validate();
        State=state; environment=new(state.World); behavior=new(actions); decision=new(actions);
        mushrooms=new(state.World);
    }
    private AntContext Context(Ant ant) => new(State,ant,colonyIndex[ant.ColonyId],spatial,movement);
    public void Step()
    {
        State.Tick++; Events.Clear();
        if(State.Tick%SimulationState.TicksPerSecond==0) environment.Step(State);
        mushrooms.Step(State.Tick);
        colonyIndex.Clear(); foreach(var colony in State.Colonies) colonyIndex.Add(colony.Id,colony);
        var count=State.Ants.Count;
        for(var i=0;i<count;i++)
        {
            var ant=State.Ants[i]; SurfaceAttachmentSystem.Step(State.World, ant); biology.Step(State,ant);
            movement.AdvanceKinematics(State,ant);
        }
        spatial.Rebuild(State);
        if(State.Tick%State.Performance.ColonyIntervalTicks==0) { colonyAi.Step(State,spatial); expansion.Step(State); colonies.Step(State); }
        if(State.Tick==1 || State.Tick%State.Performance.ColonyIntervalTicks==0)
        { escorts.Step(State); foreach(var colony in State.Colonies)nursery.Step(State,colony); }
        Scheduler.Decide(State,a=>decision.Decide(Context(a)));
        // Rotating execution order avoids permanent priority for low IDs at contested resources.
        for(var i=0;i<count;i++)
        {
            var ant=State.Ants[(i+(int)(State.Tick%Math.Max(1,count)))%count];
            var context=Context(ant);
            if(behavior.Step(context) && Events.Count<1024) Events.Add(new(State.Tick,ant.Id,ant.CurrentTask,ant.TargetId));
            if(ant.Position==context.Colony.Nest && State.Tick%20==ant.Id%20) ant.Signature=ant.Signature.Assimilate(context.Colony.Signature,(int)(State.Tick/20%64));
        }
        foreach(var ant in State.Ants)
        {
            if(ant.CarrierId is {} carrierId)
            {
                if(spatial.ById.TryGetValue(carrierId,out var carrier) && carrier.IsAlive)
                {
                    ant.PreviousPosition=ant.Position; ant.Position=carrier.Position;
                    if(!ant.IsTransportableBrood) { carrier.CarriedAntId=null;ant.CarrierId=null; }
                }
                else ant.CarrierId=null;
            }
            if(ant.CarriedAntId is {} broodId && (!spatial.ById.TryGetValue(broodId,out var brood)||!brood.IsAlive)) ant.CarriedAntId=null;
            if(!ant.IsAlive)
            {
                var colony = colonyIndex[ant.ColonyId]; colony.Deaths++;
                if(colony.Layout is {} layout)foreach(var job in layout.Construction.Jobs)if(job.AssignedAntId==ant.Id){job.AssignedAntId=null;job.Status=NestStructureStatus.WaitingDependencies;}
                if (ant.Carrying > 0 && ExcavationSpoil.Is(State.World, ant.CargoResourceId))
                    ExcavationSpoil.Drop(State.World, ant.Position, ant.Carrying);
                if (AntDied is not null)
                    AntDied(new(State.Tick, ant.Identity, ant.Caste, ant.Stage, ant.Position, ant.Destination,
                        ant.Age, ant.CurrentTask, ant.FatalDamageCauses, ant.FatalAttackerId,
                        ant.HealthBeforeFatalDamage, ant.FatalDamageAmount, ant.MaxHealth,
                        ant.Energy, ant.Nutrition, ant.Hydration, EnvironmentSystem.TemperatureAt(State, ant.Position),
                        ant.Genes.ThermalOptimum, ant.Genes.ColdTolerance, colony.Food, ant.Carrying,
                        ant.CargoResourceId, State.World.GetCell(ant.Position, false).WaterAmount));
            }
        }
        if(State.Ants.RemoveAll(a=>!a.IsAlive)>0)escorts.Step(State);
    }
    public void Run(int ticks)
    { ArgumentOutOfRangeException.ThrowIfNegative(ticks); for(var i=0;i<ticks;i++) Step(); }
}
