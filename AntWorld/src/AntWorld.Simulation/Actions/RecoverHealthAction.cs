using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class RecoverHealthAction() : AntAction(AntTask.RecoverHealth,AntCapability.Recover,.05)
{
    protected override bool IsValid(AntContext c) => c.Ant.Health<c.Ant.MaxHealth && c.Ant.Nutrition>.5 && c.Ant.Hydration>.4;
    protected override bool Apply(AntContext c) => Recover(c);
    private static bool Recover(AntContext c) { c.Ant.Recover(c.Ant.RecoveryRate); c.Ant.Nutrition-=.001; return true; }
}
