namespace AntWorld.Domain;

public interface ISnapshotStore
{
    void Save(string slot, SimulationState state);
    SimulationState Load(string slot);
}
