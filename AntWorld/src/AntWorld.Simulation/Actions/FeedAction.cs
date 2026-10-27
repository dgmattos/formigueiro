using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class FeedAction() : AntAction(AntTask.Feed,AntCapability.Feed,0)
{
    protected override bool IsValid(AntContext c) => ResourceIds.IsEdible(c.Ant.CargoResourceId) && c.Ant.Carrying>0 || c.Ant.Position==c.Colony.Nest && c.Colony.Food>0;
    protected override bool Apply(AntContext c) => Feed(c);
    private static bool Feed(AntContext c) { var need=Math.Max((100-c.Ant.Energy)/20,(1-c.Ant.Nutrition)*2); var amount=Math.Min(.5,need); if(amount<=0) return false; if(c.Ant.Carrying>0 && ResourceIds.IsEdible(c.Ant.CargoResourceId)) { amount=Math.Min(amount,c.Ant.Carrying); c.Ant.Carrying-=amount; } else { amount=Math.Min(amount,c.Colony.Food); c.Colony.Food-=amount; } c.Ant.Energy+=amount*20; c.Ant.Nutrition+=amount/2; c.Ant.Hydration+=amount*.1; return amount>0; }
}
