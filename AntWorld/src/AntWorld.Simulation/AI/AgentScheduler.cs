using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class AgentScheduler
{
    public int DecisionsLastTick { get; private set; }
    public void Decide(SimulationState state,Action<Ant> decision)
    {
        DecisionsLastTick=0;
        if(state.Ants.Count==0) return;
        var interval=SimulationState.TicksPerSecond/state.Performance.IndividualDecisionsPerSecond;
        var budget=Math.Max(1,state.Performance.MaxDetailedAgents/interval);
        var inspected=0;
        // Persistent rotating cursor bounds work without starving agents beyond the detailed budget.
        while(inspected<state.Ants.Count && DecisionsLastTick<budget)
        {
            var index=state.SchedulerCursor%state.Ants.Count;
            state.SchedulerCursor=(index+1)%state.Ants.Count;
            var ant=state.Ants[index]; inspected++;
            if(!ant.IsAlive || ant.Stage!=LifeStage.Adult || ant.CarrierId.HasValue) continue;
            var due=ant.LastDecisionTick<0 ? state.Tick>=1+ant.Id%interval : state.Tick-ant.LastDecisionTick>=interval;
            var urgent=ant.Health/ant.MaxHealth<.25 || ant.Energy<8 || ant.Hydration<.1;
            if(!due && !(urgent && state.Tick>ant.LastDecisionTick)) continue;
            decision(ant); ant.LastDecisionTick=state.Tick; DecisionsLastTick++;
        }
    }
}
