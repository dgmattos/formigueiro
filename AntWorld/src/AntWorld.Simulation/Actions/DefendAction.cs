using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class DefendAction() : AntAction(AntTask.Defend,AntCapability.Defend,0,SkillType.Defense)
{
    private readonly CombatSystem combat=new();
    protected override bool IsValid(AntContext c) => combat.CanAttack(c.State,c.Ant,c.Target);
    protected override bool Apply(AntContext c) => combat.Attack(c.State,c.Ant,c.Target);
}
