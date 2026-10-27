using AntWorld.Domain;
namespace AntWorld.Simulation;

// Rooms are completed in order. Only DigAction removes material; this system publishes usable floors.
public sealed class ColonyExpansionSystem
{
    public const int RoomHalfWidth = 4;
    private readonly NestNavigation navigation = new();
    public void Step(SimulationState state)
    {
        Span<CellPosition> targets = stackalloc CellPosition[RoomHalfWidth * 2 + 1];
        foreach (var colony in state.Colonies)
        {
            if (!colony.IsAlive || colony.Queen?.Position != colony.Nest || !HasLowerFloor(state.World, colony)) continue;
            if (PendingTargets(state.World, colony, targets) != 0) continue;
            var nextNursery = new CellPosition(colony.Nest.X, colony.BroodChamber.Y + Colony.BroodChamberDepth);
            if (!state.World.CanWalk(nextNursery) || state.World.GetCell(nextNursery, false).WaterAmount >= 200) continue;
            if (!navigation.Distances(state.World, nextNursery).ContainsKey(state.World.Index(colony.Nest))) continue;
            if (state.Ants.Any(a => a.ColonyId == colony.Id && a.IsAlive && a.Stage == LifeStage.Egg &&
                (a.Position != colony.BroodChamber || a.CarrierId.HasValue))) continue;
            colony.AdvanceFloor();
        }
    }

    public static bool FindJob(AntContext context, out CellPosition target, out CellPosition access)
    {
        target = access = default;
        var world = context.State.World;
        Span<CellPosition> targets = stackalloc CellPosition[RoomHalfWidth * 2 + 1];
        var count = PendingTargets(world, context.Colony, targets);
        Span<CellPosition> neighbors = stackalloc CellPosition[4];
        var best = int.MaxValue;
        for (var i = 0; i < count; i++)
        {
            // Different workers start on different sides; distance still avoids unnecessary commuting.
            var candidate = targets[(i + context.Ant.Id) % count];
            if (world.GetCell(candidate, false).WaterAmount >= 200) continue;
            var neighborCount = world.ChunkManager.GetNeighbors(candidate, neighbors);
            for (var j = 0; j < neighborCount; j++)
            {
                var stand = neighbors[j];
                var cell = world.GetCell(stand, false);
                var distance = world.Distance(context.Ant.Position, stand);
                if (cell.SolidFill != 0 || cell.WaterAmount >= 200 || !world.CanWalk(stand) || distance >= best) continue;
                if (!context.Movement.CanReach(context, stand)) continue;
                best = distance; target = candidate; access = stand;
            }
        }
        return best != int.MaxValue;
    }

    private static bool HasLowerFloor(WorldMap world, Colony colony) =>
        colony.BroodChamber.Y + Colony.BroodChamberDepth < world.Height - 1;

    private static int PendingTargets(WorldMap world, Colony colony, Span<CellPosition> targets)
    {
        var count = PendingRoom(world, colony.BroodChamber, targets);
        if (count > 0 || !HasLowerFloor(world, colony)) return count;
        for (var y = colony.BroodChamber.Y + 1; y <= colony.BroodChamber.Y + Colony.BroodChamberDepth; y++)
        {
            var p = new CellPosition(colony.ShaftX, y);
            if (world.GetCell(p, false).SolidFill == 0) continue;
            targets[0] = p;
            return 1;
        }
        return PendingRoom(world, new(colony.Nest.X, colony.BroodChamber.Y + Colony.BroodChamberDepth), targets);
    }

    private static int PendingRoom(WorldMap world, CellPosition center, Span<CellPosition> targets)
    {
        var count = 0;
        for (var dx = -RoomHalfWidth; dx <= RoomHalfWidth; dx++)
        {
            var p = world.Normalize(new(center.X + dx, center.Y));
            if (world.GetCell(p, false).SolidFill > 0) targets[count++] = p;
        }
        return count;
    }
}
