using AntWorld.Domain;
namespace AntWorld.Simulation;
public interface ILongevityPolicy { bool ShouldDie(Ant ant,CasteDefinition definition,SimulationState state,double elapsedSeconds); }
