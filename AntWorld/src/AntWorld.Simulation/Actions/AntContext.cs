using AntWorld.Domain;
namespace AntWorld.Simulation;
public readonly record struct AntContext(SimulationState State,Ant Ant,Colony Colony,AgentSpatialIndex Index,MovementSystem Movement)
{
    public Ant? Target => Ant.TargetId is {} id ? Index.ById.GetValueOrDefault(id) : null;
}
