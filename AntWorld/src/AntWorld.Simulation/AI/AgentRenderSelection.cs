using AntWorld.Domain;
namespace AntWorld.Simulation;
public static class AgentRenderSelection
{
    public static int Select(SimulationState state,CellPosition minimum,CellPosition maximum,List<Ant> result)
    {
        result.Clear();
        foreach(var ant in state.Ants)
        {
            if(result.Count>=state.Performance.MaxRenderedAgents) break;
            if(!HorizontalWrap.Contains(ant.Position.X,minimum.X,maximum.X,state.World.Width)||ant.Position.Y<minimum.Y||ant.Position.Y>maximum.Y) continue;
            result.Add(ant);
        }
        return result.Count;
    }
    // L2/L3 deliberately not selected: no lossy population aggregation is implemented.
    public static SimulationLevel Level(bool visible) => visible ? SimulationLevel.Visible : SimulationLevel.Nearby;
}
