using AntWorld.Domain;
namespace AntWorld.Simulation;

public static class FoodSources
{
    public static double AmountAt(WorldMap world, CellPosition position)
    {
        var amount = world.GetResource(position, ResourceIds.Food).Quantity;
        var support = new CellPosition(position.X, position.Y + 1);
        if (world.Contains(support))
            amount += world.GetResource(support, ResourceIds.RedMushroom).Quantity + world.GetResource(support, ResourceIds.BrownMushroom).Quantity;
        return amount;
    }

    public static bool TryCollectible(WorldMap world, Ant ant, out CellPosition resourcePosition, out ushort id)
    {
        ReadOnlySpan<ushort> types = [ResourceIds.Food, ResourceIds.RedMushroom, ResourceIds.BrownMushroom];
        foreach (var type in types)
        {
            if (ant.Carrying > 0 && ant.CargoResourceId != type) continue;
            var p = ResourceIds.IsMushroom(type) ? new CellPosition(ant.Position.X, ant.Position.Y + 1) : ant.Position;
            if (!world.Contains(p) || world.GetResource(p, type).Quantity <= 0) continue;
            if (ResourceIds.IsMushroom(type) && !MushroomHabitat.Allows(world, p)) continue;
            resourcePosition = p; id = type; return true;
        }
        resourcePosition = default; id = 0; return false;
    }
}
