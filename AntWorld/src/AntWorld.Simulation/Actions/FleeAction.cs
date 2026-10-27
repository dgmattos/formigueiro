using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class FleeAction() : AntAction(AntTask.Flee,AntCapability.Move,.05,SkillType.Navigation)
{
    protected override bool IsValid(AntContext c) => true;
    protected override bool Apply(AntContext c) => c.Movement.Move(c,false,true);
    
}
