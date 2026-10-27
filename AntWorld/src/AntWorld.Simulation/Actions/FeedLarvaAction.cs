using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class FeedLarvaAction() : AntAction(AntTask.FeedLarva,AntCapability.BroodCare,.1,SkillType.BroodCare)
{
    private readonly RecognitionService recognition=new();
    protected override bool IsValid(AntContext c) => c.Target is { Stage:LifeStage.Larva,IsAlive:true } larva &&
        c.State.World.Distance(c.Ant.Position,larva.Position)<=1 && recognition.Recognize(c.Ant,larva,c.State).KnownMembership &&
        larva.Nutrition<.95 && (c.Ant.Carrying>0 && ResourceIds.IsEdible(c.Ant.CargoResourceId) ||
            (c.Ant.Position==c.Colony.Nest || c.Ant.Position==c.Colony.BroodChamber) && c.Colony.Food>0);
    protected override bool Apply(AntContext c)
    {
        var amount=Math.Min(.5,(1-c.Target!.Nutrition)*2);
        if(c.Ant.Carrying>0 && ResourceIds.IsEdible(c.Ant.CargoResourceId)) { amount=Math.Min(amount,c.Ant.Carrying); c.Ant.Carrying-=amount; }
        else { amount=Math.Min(amount,c.Colony.Food); c.Colony.Food-=amount; }
        c.Target.Nutrition+=amount/2; c.Target.Hydration+=amount*.2; c.Target.Energy+=amount*20; return amount>0;
    }
}
