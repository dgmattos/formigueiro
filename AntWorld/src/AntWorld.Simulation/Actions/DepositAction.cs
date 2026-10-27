using AntWorld.Domain;
namespace AntWorld.Simulation;

public sealed class DepositAction() : AntAction(AntTask.Deposit, AntCapability.Carry, .05, SkillType.Carrying)
{
    protected override bool IsValid(AntContext c) => c.Ant.Carrying > 0 &&
        (ResourceIds.IsEdible(c.Ant.CargoResourceId) ? c.Ant.Position == c.Colony.Nest :
        ExcavationSpoil.Is(c.State.World, c.Ant.CargoResourceId) && c.Ant.Position == ExcavationSpoil.Exit(c.State.World, c.Colony));
    protected override bool Apply(AntContext c)
    {
        if (ResourceIds.IsEdible(c.Ant.CargoResourceId)) c.Colony.Food += c.Ant.Carrying;
        else ExcavationSpoil.Drop(c.State.World, c.Ant.Position, c.Ant.Carrying);
        c.Ant.Carrying = 0; c.Ant.CargoResourceId = ResourceIds.Food; c.Ant.Returning = false;
        return true;
    }
}
