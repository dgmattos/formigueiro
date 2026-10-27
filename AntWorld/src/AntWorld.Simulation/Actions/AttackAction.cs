using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class AttackAction() : AntAction(AntTask.Attack,AntCapability.Attack,0,SkillType.Combat)
{
    private readonly CombatSystem combat=new();
    protected override bool IsValid(AntContext c) => combat.CanAttack(c.State,c.Ant,c.Target);
    protected override bool Apply(AntContext c) => combat.Attack(c.State,c.Ant,c.Target);
}
