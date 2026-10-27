using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class CollectAction() : AntAction(AntTask.Collect,AntCapability.Collect,.05,SkillType.ResourceDetection)
{
    protected override bool IsValid(AntContext c) => c.Ant.CarriedAntId is null && c.Ant.Carrying<AntCapabilities.Capacity(c.State,c.Ant) && TrySource(c,out _,out _);
    protected override bool Apply(AntContext c) => Collect(c);
    private static bool Collect(AntContext c)
    {
        if (!TrySource(c,out var position,out var type)) return false;
        var amount=c.State.World.Extract(position,type,Math.Min(4,AntCapabilities.Capacity(c.State,c.Ant)-c.Ant.Carrying));
        if(amount<=0) return false;
        c.Ant.FoodDelivery=FoodDeliveryStage.None; c.Ant.CargoResourceId=type; c.Ant.Carrying+=amount; c.Ant.Returning=true; c.Ant.Destination=ExcavationSpoil.Is(c.State.World,type) ? ExcavationSpoil.Exit(c.State.World,c.Colony) : c.Colony.Nest; return true;
    }
    private static bool TrySource(AntContext c, out CellPosition position, out ushort type)
    {
        if (c.Ant.WorkerRole == WorkerRole.Digger && c.Ant.Carrying == 0 &&
            DroppedSpoil.Find(c.State.World, c.Ant.Position, out position) && position == c.Ant.Position)
        {
            type = ExcavationSpoil.ResourceId(c.State.World);
            return true;
        }
        // Preserve explicitly selected material collection for construction and future resource tasks.
        if (!ResourceIds.IsEdible(c.Ant.CargoResourceId))
        {
            position = c.Ant.Position; type = c.Ant.CargoResourceId;
            return c.State.World.ResourceCatalog.Get(type).Extractable && c.State.World.GetResource(position, type).Quantity > 0;
        }
        return FoodSources.TryCollectible(c.State.World, c.Ant, out position, out type, preferLargest:true);
    }
}
