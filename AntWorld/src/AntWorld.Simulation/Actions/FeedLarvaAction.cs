using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class FeedLarvaAction() : AntAction(AntTask.FeedLarva,AntCapability.BroodCare,.1,SkillType.BroodCare)
{
    private readonly RecognitionService recognition=new();
    protected override bool IsValid(AntContext c) => c.Target is { Stage:LifeStage.Larva,IsAlive:true } larva &&
        c.State.World.Distance(c.Ant.Position,larva.Position)<=1 && recognition.Recognize(c.Ant,larva,c.State).KnownMembership &&
        NutritionRules.FoodNeeded(c.State,larva)>.1 && (LocalProvisions.Amount(c.State.World,larva.Position)>0 || c.Ant.Carrying>0 && ResourceIds.IsEdible(c.Ant.CargoResourceId) ||
            (c.Ant.Position==c.Colony.Nest || c.Ant.Position==c.Colony.BroodChamber) && c.Colony.Food>0);
    protected override bool Apply(AntContext c)
    {
        var amount=Math.Min(.5,NutritionRules.FoodNeeded(c.State,c.Target!));
        if(c.Ant.Carrying>0 && ResourceIds.IsEdible(c.Ant.CargoResourceId)) { amount=Math.Min(amount,c.Ant.Carrying); c.Ant.Carrying-=amount; }
        else if(LocalProvisions.Amount(c.State.World,c.Target!.Position)>0) amount=LocalProvisions.Consume(c.State.World,c.Target.Position,amount);
        else { amount=Math.Min(amount,c.Colony.Food); c.Colony.Food-=amount; }
        NutritionRules.Feed(c.State,c.Target!,amount); return amount>0;
    }
}
