using AntWorld.Domain;
namespace AntWorld.Simulation;

public static class CollectorFoodDelivery
{
    public const double CollectorSatisfaction = .5;
    public const double QueenSatisfaction = .8;
    public static bool IsCollector(Ant ant) => ant.Caste==AntCaste.Worker && ant.Stage==LifeStage.Adult && ant.WorkerRole==WorkerRole.Collector;
    public static bool HasFood(Ant ant) => ant.Carrying>0 && ResourceIds.IsEdible(ant.CargoResourceId);
    public static void Prepare(AntContext c)
    {
        var ant=c.Ant;
        if(!IsCollector(ant) || !HasFood(ant) || c.Colony.Queen is not {IsAlive:true})
        {ant.FoodDelivery=FoodDeliveryStage.None;return;}
        if(ant.FoodDelivery==FoodDeliveryStage.None && ant.Nutrition>=CollectorSatisfaction)
            ant.FoodDelivery=FoodDeliveryStage.InspectQueen;
        // This is the only observation of the queen's nutrition: strictly orthogonal contact.
        if(ant.FoodDelivery==FoodDeliveryStage.InspectQueen && c.State.World.Distance(ant.Position,c.Colony.Queen.Position)==1)
            ant.FoodDelivery=c.Colony.Queen.Nutrition>=QueenSatisfaction?FoodDeliveryStage.Nursery:FoodDeliveryStage.Queen;
    }
    public static CellPosition? Recipient(AntContext c) => c.Ant.FoodDelivery switch
    {
        FoodDeliveryStage.InspectQueen or FoodDeliveryStage.Queen => c.Colony.Queen is {IsAlive:true} queen?queen.Position:null,
        FoodDeliveryStage.Nursery => c.Colony.BroodChamber,
        _ => null
    };
    public static CellPosition? Destination(AntContext c)
    {
        if(Recipient(c) is not {} recipient)return null;
        CellPosition? selected=null;var nearest=int.MaxValue;
        ReadOnlySpan<CellPosition> offsets=[new(-1,0),new(1,0),new(0,-1),new(0,1)];
        foreach(var offset in offsets)
        {
            var point=c.State.World.Normalize(new(recipient.X+offset.X,recipient.Y+offset.Y));
            if(!c.State.World.Contains(point)||!c.State.World.CanWalk(point)||!QueenEscortSystem.Allows(c,point)||c.State.World.GetCell(point,false).WaterAmount>=200)continue;
            var distance=c.Movement.DistanceTo(c.State.World,c.Ant.Position,point);
            if(distance>=0 && distance<nearest){selected=point;nearest=distance;}
        }
        return selected;
    }
    public static bool CanDeposit(AntContext c) => c.Ant.FoodDelivery is FoodDeliveryStage.Queen or FoodDeliveryStage.Nursery &&
        Recipient(c) is {} recipient && c.State.World.CanWalk(recipient) && c.State.World.GetCell(recipient,false).WaterAmount<200 &&
        c.State.World.Distance(c.Ant.Position,recipient)==1;
}
