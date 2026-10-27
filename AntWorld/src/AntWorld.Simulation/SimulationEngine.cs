using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class SimulationEngine
{
    public SimulationState State { get; }
    public AgentScheduler Scheduler { get; }=new();
    public List<AntEvent> Events { get; }=[];
    private readonly EnvironmentSystem environment;
    private readonly MushroomSystem mushrooms;
    private readonly AntBiologySystem biology=new();
    private readonly AgentSpatialIndex spatial=new();
    private readonly ColonyAiSystem colonyAi=new();
    private readonly ColonySystem colonies=new();
    private readonly ActionCatalog actions=new();
    private readonly MovementSystem movement=new();
    private readonly AntBehaviorSystem behavior;
    private readonly DecisionSystem decision;
    private readonly Dictionary<int,Colony> colonyIndex=[];
    public SimulationEngine(SimulationState state)
    {
        state.Performance.Validate(); foreach(var species in state.Species.Values) species.Validate();
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
            var ant=State.Ants[i]; biology.Step(State,ant);
            ant.MovementCredit=Math.Min(2,ant.MovementCredit+movement.EffectiveSpeed(State,ant)*SimulationState.TickSeconds);
        }
        spatial.Rebuild(State);
        if(State.Tick%State.Performance.ColonyIntervalTicks==0) { colonyAi.Step(State,spatial); colonies.Step(State); }
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
            if(!ant.IsAlive) colonyIndex[ant.ColonyId].Deaths++;
        }
        State.Ants.RemoveAll(a=>!a.IsAlive);
    }
    public void Run(int ticks)
    { ArgumentOutOfRangeException.ThrowIfNegative(ticks); for(var i=0;i<ticks;i++) Step(); }
}
