using AntWorld.Domain;
namespace AntWorld.Simulation;

public static class NutritionRules
{
    public static double FoodNeeded(SimulationState state, Ant ant) => Math.Max(
        Math.Max((ant.MaxEnergy - ant.Energy) / 20, (1 - ant.Nutrition) * 2),
        (1 - ant.Hydration) / state.Survival.FoodHydration);
    public static void Feed(SimulationState state, Ant ant, double amount)
    {
        ant.Energy += amount * 20; ant.Nutrition += amount / 2;
        ant.Hydration += amount * state.Survival.FoodHydration;
    }
}
