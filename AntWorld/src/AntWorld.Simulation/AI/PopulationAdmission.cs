using AntWorld.Domain;
namespace AntWorld.Simulation;
// Deterministic admission estimate, not an OS memory limiter. Existing agents are never culled.
public static class PopulationAdmission
{
    public static bool CanAdd(SimulationState state)
    {
        if(state.Ants.Count>=state.Performance.MaximumPopulation) return false;
        var world=state.World;
        var chunks=(long)world.Configuration.MaximumLoadedChunks*world.Configuration.ChunkSize*world.Configuration.ChunkSize*32;
        var changes=(long)world.ChunkManager.CellChanges.Count*64+world.ChunkManager.ResourceChanges.Count*96L;
        var trails=0L;foreach(var field in state.Pheromones.Values) trails+=field.ActiveCellCount*64L;
        var agents=(state.Ants.Count+1L)*2048;
        // Reserve for engine/assets, baseline resource plan and bounded navigation caches.
        var reserve=32L*1024*1024+world.Width*32L+state.Colonies.Count*2*32768L*64;
        return agents+chunks+changes+trails+reserve<state.Performance.MaxMemoryBudgetMb*1024L*1024;
    }
}
