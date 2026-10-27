using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class AntBiologySystem
{
    private readonly NoAgeLimitPolicy unlimited = new();
    private readonly HealthBasedLongevityPolicy aging = new();
    public void Step(SimulationState state, Ant ant)
    {
        if (!ant.IsAlive) return;
        ant.Age++;
        var rules = state.Survival;
        var temperature = EnvironmentSystem.TemperatureAt(state, ant.Position);
        var stress = Math.Max(0, Math.Abs(temperature - ant.Genes.ThermalOptimum) - ant.Genes.ColdTolerance);
        var elapsedDays = 1.0 / SimulationState.TicksPerDay;
        if (ant.Stage is LifeStage.Egg or LifeStage.Pupa)
        {
            // Sealed stages spend finite internal reserves as development proceeds; they do not forage or rest.
            ant.Hydration -= rules.HydrationPerDay * .02 * (1 + stress * .05) * elapsedDays;
        }
        else
        {
            var metabolism = ant.Stage == LifeStage.Larva ? .25 : 1;
            ant.Energy -= rules.EnergyPerDay * ant.Genes.Metabolism * metabolism * (1 + stress * .12) * elapsedDays;
            ant.Nutrition -= rules.NutritionPerDay * metabolism * elapsedDays;
            ant.Hydration -= rules.HydrationPerDay * (1 + stress * .05) * elapsedDays;
        }
        var causes = DamageCause.Unknown;
        if (stress > 0) causes |= DamageCause.ThermalStress;
        if (ant.Energy == 0) causes |= DamageCause.EnergyDepletion;
        if (ant.Nutrition == 0) causes |= DamageCause.Starvation;
        if (ant.Hydration == 0) causes |= DamageCause.Dehydration;
        var damage = stress * rules.ThermalDamagePerDegreeDay +
            (ant.Energy == 0 ? rules.ExhaustionDamagePerDay : 0) +
            (ant.Nutrition == 0 ? rules.StarvationDamagePerDay : 0) +
            (ant.Hydration == 0 ? rules.DehydrationDamagePerDay : 0);
        ant.Damage(damage * elapsedDays, causes);
        ant.BiologicalCondition = (ant.Nutrition + ant.Hydration + ant.Health / ant.MaxHealth) / 3;
        if (!ant.IsAlive) return;
        if (ant.Stage != LifeStage.Adult)
        {
            Develop(state, ant, temperature);
            return;
        }
        if (state.Tick % state.Performance.BiologicalIntervalTicks != ant.Id % state.Performance.BiologicalIntervalTicks) return;
        var definition = AntCapabilities.Definition(state, ant);
        ILongevityPolicy policy = definition.LongevityDays == 0 ? unlimited : aging;
        if (policy.ShouldDie(ant, definition, state, state.Performance.BiologicalIntervalTicks * SimulationState.TickSeconds))
            ant.Damage(ant.Health, DamageCause.Aging);
    }
    private static void Develop(SimulationState state, Ant ant, double temperature)
    {
        ant.CurrentTask = AntTask.Rest; ant.Movement = MovementState.Idle;
        var growth = ant.Nutrition * ant.Health / ant.MaxHealth * Math.Clamp(1 - Math.Abs(temperature - ant.Genes.ThermalOptimum) / 35, 0, 1);
        var species = state.Species[ant.SpeciesId];
        var days = ant.Stage switch { LifeStage.Egg => species.EggDays, LifeStage.Larva => species.LarvaDays, _ => species.PupaDays };
        var required = days * SimulationState.TicksPerDay;
        var progress = Math.Min(growth, Math.Max(0, required - ant.DevelopmentTicks));
        if (ant.Stage is LifeStage.Egg or LifeStage.Pupa)
        {
            // Eggs consume no energy. Both sealed stages retain their material development costs.
            var fraction = progress / required;
            if (ant.Stage == LifeStage.Pupa) ant.Energy -= 40 * fraction;
            ant.Nutrition -= .25 * fraction; ant.Hydration -= .25 * fraction;
        }
        ant.DevelopmentTicks += progress;
        if (ant.DevelopmentTicks < required) return;
        ant.DevelopmentTicks = 0; ant.Stage++;
        if (ant.Stage == LifeStage.Adult)
        {
            AntFactory.AssignWorkerRole(state, ant);
            var health = AntCapabilities.Definition(state, ant).MaximumHealth;
            ant.ChangeMaximumHealth(health); ant.Recover(health);
        }
    }
}
