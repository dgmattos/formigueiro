using AntWorld.Domain;
namespace AntWorld.Simulation;
public static class ExcavationRules
{
    public const double SoilEnergyCost = .4;
    public const double GlobalSpeedMultiplier = .4;
    // Was one work action per 10 ticks. 25 ticks preserves 40% of the rate without rounding tiny rock work up.
    public const int ActionIntervalTicks = (int)(10 / GlobalSpeedMultiplier);
    public static double SpeedFactor(SpeciesDefinition species, WorldCell cell) => cell.Type switch
    {
        BlockType.Rock => species.SoilDiggingMultiplier * species.SolidRockSpeedFactor,
        BlockType.SoilAndRock => species.MixedDiggingMultiplier,
        _ => species.SoilDiggingMultiplier
    };
    public static double EnergyCost(SpeciesDefinition species, WorldCell cell) => SoilEnergyCost * (cell.Type == BlockType.Rock ? species.SolidRockEnergyFactor : 1);
    public static double Work(AntContext context, WorldCell cell)
    {
        // Compare substrates at equal moisture, skills and genes. Solid rock uses the explicit species penalty.
        var resistance = 1 + (100 - cell.Moisture) / 100.0;
        return AntCapabilities.Attributes(context.State, context.Ant).DiggingPower * 25 / resistance * SpeedFactor(context.State.Species[context.Ant.SpeciesId], cell);
    }
}
