namespace AntWorld.Domain;

public static class MushroomHabitat
{
    // Resources belong to the supporting solid cell; the sprite and collector occupy the open cell above.
    public static bool Allows(WorldCell support, WorldCell above) =>
        support.Type is BlockType.Soil or BlockType.SoilAndRock && !support.IsPassable &&
        above.IsPassable && above.WaterAmount < 200 && support.WaterAmount < 200;

    public static bool Allows(WorldMap world, CellPosition support) => support.Y > 0 && world.Contains(support) &&
        Allows(world.GetCell(support, false), world.GetCell(new(support.X, support.Y - 1), false));
}
