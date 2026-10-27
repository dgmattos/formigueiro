using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class DepositAction() : AntAction(AntTask.Deposit,AntCapability.Carry,.05,SkillType.Carrying)
{
    protected override bool IsValid(AntContext c) => c.Ant.Position==c.Colony.Nest && c.Ant.Carrying>0 && ResourceIds.IsEdible(c.Ant.CargoResourceId);
    protected override bool Apply(AntContext c) => Deposit(c);
    private static bool Deposit(AntContext c) { c.Colony.Food+=c.Ant.Carrying; c.Ant.Carrying=0; c.Ant.CargoResourceId=ResourceIds.Food; c.Ant.Returning=false; return true; }
}
