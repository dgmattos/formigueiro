using AntWorld.Domain;
namespace AntWorld.Simulation;

public static class WaterSources
{
    public static bool TryAccessible(WorldMap world, CellPosition origin, out CellPosition source)
    {
        if (world.IsOpen(origin) && world.GetCell(origin, false).WaterAmount > 0) { source = world.Normalize(origin); return true; }
        Span<CellPosition> neighbors = stackalloc CellPosition[4];
        var count = world.ChunkManager.GetNeighbors(origin, neighbors);
        for (var i = 0; i < count; i++)
            if (world.IsOpen(neighbors[i]) && world.GetCell(neighbors[i], false).WaterAmount > 0)
            { source = neighbors[i]; return true; }
        source = default; return false;
    }
}
