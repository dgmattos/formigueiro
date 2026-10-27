using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class DrinkAction() : AntAction(AntTask.Drink,AntCapability.Feed,0)
{
    protected override bool IsValid(AntContext c) => c.State.World.GetCell(c.Ant.Position,false).WaterAmount>0;
    protected override bool Apply(AntContext c) => Drink(c);
    private static bool Drink(AntContext c) { var cell=c.State.World.GetCell(c.Ant.Position,false); var units=(ushort)Math.Min(cell.WaterAmount,Math.Ceiling((1-c.Ant.Hydration)*10)); if(units==0) return false; c.State.World.SetCell(c.Ant.Position,cell with { WaterAmount=(ushort)(cell.WaterAmount-units) }); c.Ant.Hydration+=units/10.0; return true; }
}
