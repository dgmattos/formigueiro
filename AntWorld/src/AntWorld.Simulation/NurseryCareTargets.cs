using AntWorld.Domain;
namespace AntWorld.Simulation;

// Rebuild once per decision tick, rather than scanning the whole population for every worker.
public sealed class NurseryCareTargets
{
    private SimulationState? cachedState;
    private long tick=-1;
    private readonly Dictionary<int,List<Ant>> displaced=[];
    public Ant? Nearest(AntContext context)
    {
        var state=context.State;
        if(cachedState!=state || tick!=state.Tick)
        {
            cachedState=state;tick=state.Tick;
            foreach(var list in displaced.Values)list.Clear();
            foreach(var ant in state.Ants)
            {
                if(!ant.IsTransportableBrood || ant.CarrierId.HasValue)continue;
                if(!displaced.TryGetValue(ant.ColonyId,out var list))displaced[ant.ColonyId]=list=[];
                list.Add(ant);
            }
        }
        if(!QueenEscortSystem.Allows(context,context.Colony.BroodChamber) || !context.Movement.CanReachBroodChamber(context) ||
            !displaced.TryGetValue(context.Colony.Id,out var candidates))return null;
        Ant? selected=null;var nearest=int.MaxValue;
        foreach(var brood in candidates)
        {
            if(!BroodTransport.NeedsRelocation(state.World,context.Colony,brood) || !QueenEscortSystem.Allows(context,brood.Position))continue;
            var distance=state.World.Distance(context.Ant.Position,brood.Position);
            if(distance>=nearest || !context.Movement.CanReach(context,brood.Position))continue;
            nearest=distance;selected=brood;
        }
        return selected;
    }
}
