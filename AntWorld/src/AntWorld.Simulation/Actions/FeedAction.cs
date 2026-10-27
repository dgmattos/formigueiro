using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class FeedAction() : AntAction(AntTask.Feed, AntCapability.Feed, 0)
{
    protected override bool IsValid(AntContext c) => NutritionRules.FoodNeeded(c.State, c.Ant) > .000001 &&
        (HasFoodCargo(c.Ant) || LocalProvisions.Amount(c.State.World,c.Ant.Position)>0 || AtPantry(c) && c.Colony.Food > 0 || FoodSources.TryCollectible(c.State.World, c.Ant, out _, out _));
    private static bool HasFoodCargo(Ant ant) => ResourceIds.IsEdible(ant.CargoResourceId) && ant.Carrying > 0;
    private static bool AtPantry(AntContext c) => c.Ant.Position == c.Colony.FoodStorage || c.Ant.Position == c.Colony.Nest || c.Ant.Position == c.Colony.BroodChamber;
    protected override bool Apply(AntContext c)
    {
        var amount = Math.Min(.5, NutritionRules.FoodNeeded(c.State, c.Ant));
        if (HasFoodCargo(c.Ant)) { amount = Math.Min(amount, c.Ant.Carrying); c.Ant.Carrying -= amount; }
        else if(LocalProvisions.Amount(c.State.World,c.Ant.Position)>0) amount=LocalProvisions.Consume(c.State.World,c.Ant.Position,amount);
        else if (AtPantry(c) && c.Colony.Food > 0) { amount = Math.Min(amount, c.Colony.Food); c.Colony.Food -= amount; }
        else if (FoodSources.TryCollectible(c.State.World, c.Ant, out var position, out var type))
            amount = c.State.World.Extract(position, type, amount);
        else return false;
        NutritionRules.Feed(c.State, c.Ant, amount);
        return amount > 0;
    }
}
