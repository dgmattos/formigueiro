using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class ExploreAction() : AntAction(AntTask.Explore,AntCapability.Explore,.05,SkillType.Exploration)
{
    protected override bool IsValid(AntContext c) => true;
    protected override bool Apply(AntContext c) => c.Movement.Move(c,true);
    
}
