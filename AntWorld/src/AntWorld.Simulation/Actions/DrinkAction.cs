using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class DrinkAction() : AntAction(AntTask.Drink, AntCapability.Feed, 0)
{
    protected override bool IsValid(AntContext c) => c.Ant.Hydration <= .9 && WaterSources.TryAccessible(c.State.World, c.Ant.Position, out _);
    protected override bool Apply(AntContext c)
    {
        if (!WaterSources.TryAccessible(c.State.World, c.Ant.Position, out var source)) return false;
        var cell = c.State.World.GetCell(source, false);
        var units = (ushort)Math.Min(cell.WaterAmount, Math.Floor((1 - c.Ant.Hydration) * 10 + 1e-9));
        if (units == 0) return false;
        c.State.World.SetCell(source, cell with { WaterAmount = (ushort)(cell.WaterAmount - units) });
        c.Ant.Hydration += units / 10.0;
        return true;
    }
}
