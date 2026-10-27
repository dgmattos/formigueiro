using AntWorld.Domain;
namespace AntWorld.Simulation;

public static class DroppedSpoil
{
    public static bool Find(WorldMap world, CellPosition origin, out CellPosition position)
    {
        position = default;
        var resource = world.ResourceCatalog.Definitions.FirstOrDefault(d => d.Kind == ExcavationSpoil.Kind);
        if (resource is null) return false;
        var best = int.MaxValue;
        // Only loose underground material is recovered. Surface deposits stay outside the nest.
        for (var dy = -4; dy <= 4; dy++) for (var dx = -4; dx <= 4; dx++)
        {
            var p = new CellPosition(origin.X + dx, origin.Y + dy);
            if (!world.Contains(p)) continue;
            p = world.Normalize(p);
            if (p.Y < world.Surface[p.X] || !world.CanWalk(p)) continue;
            var distance = world.Distance(origin, p);
            if (distance >= best || world.GetResource(p, resource.Id).Quantity <= 0) continue;
            position = p; best = distance;
        }
        return best != int.MaxValue;
    }
}
