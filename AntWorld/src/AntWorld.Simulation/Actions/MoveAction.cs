using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class MoveAction() : AntAction(AntTask.Move,AntCapability.Move,.05,SkillType.Navigation)
{
    protected override bool IsValid(AntContext c) => c.Ant.Destination.HasValue;
    protected override bool Apply(AntContext c) => c.Movement.Move(c);
    
}
