namespace AntWorld.Domain;

// Rates use biological days (three simulated hours), never rendered frames or playback multipliers.
public sealed record SurvivalSettings(
    double EnergyPerDay = 12, double NutritionPerDay = .35, double HydrationPerDay = .6,
    double ThermalDamagePerDegreeDay = .5, double ExhaustionDamagePerDay = 60,
    double StarvationDamagePerDay = 30, double DehydrationDamagePerDay = 60,
    double MinimumEggIntervalDays = .25, double FoodReservePerIndividual = 2,
    double FoodHydration = .5)
{
    public void Validate()
    {
        if (new[] { EnergyPerDay, NutritionPerDay, HydrationPerDay, ThermalDamagePerDegreeDay,
                ExhaustionDamagePerDay, StarvationDamagePerDay, DehydrationDamagePerDay,
                MinimumEggIntervalDays, FoodReservePerIndividual, FoodHydration }
            .Any(value => !double.IsFinite(value) || value <= 0 || value > 10000) ||
            MinimumEggIntervalDays > 28 || FoodHydration > 1)
            throw new ArgumentException("survival: taxas devem ser finitas e positivas; intervalo de ovos até 28 dias, hidratação por alimento até 1.");
    }
}
