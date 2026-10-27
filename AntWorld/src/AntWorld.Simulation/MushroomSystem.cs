using AntWorld.Domain;
using AntWorld.WorldGeneration;
namespace AntWorld.Simulation;

public sealed class MushroomSystem
{
    private readonly WorldMap world;
    private readonly MushroomGrowth growth;
    private readonly long intervalTicks;
    private readonly int chunkCount;

    public MushroomSystem(WorldMap world)
    {
        this.world = world;
        growth = new(world.Configuration, world.Surface, p => world.GetCell(p, false), world.GetResource, world.SetResource);
        intervalTicks = (long)world.Configuration.Mushrooms.RenewalIntervalSeconds * SimulationState.TicksPerSecond;
        chunkCount = world.ChunkManager.ChunkColumns * ((world.Height + world.Configuration.ChunkSize - 1) / world.Configuration.ChunkSize);
    }

    public void Step(long tick)
    {
        if (tick < intervalTicks) return;
        var offset = tick % intervalTicks;
        var first = offset * chunkCount / intervalTicks;
        var end = (offset + 1) * chunkCount / intervalTicks;
        for (var id = first; id < end; id++)
        {
            var size = world.Configuration.ChunkSize;
            var p = new CellPosition((int)(id % world.ChunkManager.ChunkColumns) * size, (int)(id / world.ChunkManager.ChunkColumns) * size);
            growth.Grow(world.ChunkManager.Region(p), tick / intervalTicks);
        }
    }
}
