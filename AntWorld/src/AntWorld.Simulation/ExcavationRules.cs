using AntWorld.Domain;
namespace AntWorld.Simulation;
public static class ExcavationRules
{
    public const double SoilEnergyCost = .4;
    public const double GlobalSpeedMultiplier = .16;
    // Two actions per 125 ticks: alternating 63/62 ticks gives exactly 80% of the previous rate.
    private const int ActionCycleTicks = 125;
    public const double ActionIntervalTicks = ActionCycleTicks / 2.0;
    public static bool IsActionTick(long tick,int antId) => ((tick + antId) % ActionCycleTicks) * 2 % ActionCycleTicks < 2;
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
