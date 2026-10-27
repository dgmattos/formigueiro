using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class NoAgeLimitPolicy : ILongevityPolicy
{
    public bool ShouldDie(Ant ant,CasteDefinition definition,SimulationState state,double elapsedSeconds) => false;
}
