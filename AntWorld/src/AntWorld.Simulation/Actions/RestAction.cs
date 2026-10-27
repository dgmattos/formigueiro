using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class RestAction() : AntAction(AntTask.Rest,AntCapability.Rest,0)
{
    protected override bool IsValid(AntContext c) => true;
    protected override bool Apply(AntContext c) => Rest(c);
    private static bool Rest(AntContext c) { c.Ant.Movement=MovementState.Idle; if(c.Ant.Nutrition>.1) { c.Ant.Energy+=.5; c.Ant.Nutrition-=.001; } return true; }
}
