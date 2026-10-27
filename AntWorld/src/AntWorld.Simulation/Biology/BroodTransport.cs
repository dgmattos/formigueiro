using AntWorld.Domain;
namespace AntWorld.Simulation;

public static class BroodTransport
{
    public const double CarryWeight = 2;

    public static bool NurseryAvailable(WorldMap world, Colony colony) =>
        world.CanWalk(colony.BroodChamber) && world.GetCell(colony.BroodChamber, false).WaterAmount < 200;

    public static bool NeedsRelocation(WorldMap world, Colony colony, Ant brood)
    {
        if (!brood.IsTransportableBrood || brood.CarrierId.HasValue || brood.ColonyId != colony.Id || !NurseryAvailable(world, colony)) return false;
        if (brood.Position == colony.BroodChamber) return false;
        return true;
    }
}
