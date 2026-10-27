using AntWorld.Domain;
namespace AntWorld.Simulation;

public sealed class CarryAction() : AntAction(AntTask.Carry, AntCapability.Carry, .05, SkillType.Carrying)
{
    private readonly RecognitionService recognition = new();

    protected override bool IsValid(AntContext c)
    {
        if (c.Ant.Carrying > 0) return c.Ant.CarriedAntId is null;
        if (!AntCapabilities.Has(c.State, c.Ant, AntCapability.BroodCare) || AntCapabilities.Capacity(c.State, c.Ant) < BroodTransport.CarryWeight) return false;
        var brood = c.Ant.CarriedAntId is {} id ? c.Index.ById.GetValueOrDefault(id) : c.Target;
        if (brood is null || !brood.IsTransportableBrood || !recognition.Recognize(c.Ant, brood, c.State).KnownMembership) return false;
        var destination = c.Ant.Destination ?? c.Colony.BroodChamber;
        if (!c.State.World.CanWalk(destination) || c.State.World.GetCell(destination, false).WaterAmount >= 200) return false;
        // Eggs belong below the queen chamber; larvae can still be evacuated to other safe chambers.
        if (brood.Stage == LifeStage.Egg && destination != c.Colony.BroodChamber) return false;
        if (destination == c.Colony.BroodChamber && !c.Movement.CanReachBroodChamber(c)) return false;
        if (c.Ant.CarriedAntId.HasValue) return brood.CarrierId == c.Ant.Id;
        return brood.CarrierId is null && brood.Position != destination && c.State.World.CanWalk(brood.Position) && c.State.World.Distance(c.Ant.Position, brood.Position) <= 1;
    }

    protected override bool Apply(AntContext c)
    {
        if (c.Ant.Carrying > 0) return c.Movement.Move(c);
        c.Ant.Destination ??= c.Colony.BroodChamber;
        if (c.Ant.CarriedAntId is null && c.Target is {} brood)
        {
            c.Ant.CarriedAntId = brood.Id;
            brood.CarrierId = c.Ant.Id;
            brood.PreviousPosition = brood.Position;
            brood.Position = c.Ant.Position;
            c.Ant.Destination ??= c.Colony.BroodChamber;
            c.Ant.Movement = MovementState.Carrying;
            return true;
        }
        var carried = c.Index.ById[c.Ant.CarriedAntId!.Value];
        if (c.Ant.Position == c.Ant.Destination)
        {
            carried.Position = c.Ant.Position;
            carried.CarrierId = null;
            c.Ant.CarriedAntId = null;
            c.Ant.TargetId = null;
            c.Ant.Movement = MovementState.Idle;
            return true;
        }
        if (!c.Movement.Move(c)) return false;
        carried.PreviousPosition = carried.Position;
        carried.Position = c.Ant.Position;
        return true;
    }
}
