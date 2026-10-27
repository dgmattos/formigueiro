using AntWorld.Domain;
namespace AntWorld.Simulation;

public sealed class EnvironmentSystem
{
    private static readonly double[] SeasonalTemperature = [2, 10, -2, -12];
    private static readonly double[] SeasonalGrowth = [1.4, 1, .65, .25];
    private ResourceEntry[] regeneratingResources = [];
    private int resourceLayoutRevision = -1;

    public EnvironmentSystem(WorldMap world) => RefreshResources(world);
    private void RefreshResources(WorldMap world)
    {
        if (resourceLayoutRevision == world.ResourceLayoutRevision) return;
        regeneratingResources = world.Resources().Where(e => world.ResourceCatalog.Get(e.ResourceId).Regeneration > 0).ToArray();
        resourceLayoutRevision = world.ResourceLayoutRevision;
    }

    public static double TemperatureAt(SimulationState state, CellPosition position)
    {
        var world = state.World;
        var baseline = BiomeProfile.For(world.Biomes[position.X]).Temperature;
        var surfaceTemperature = baseline + SeasonalTemperature[state.Season] + Math.Sin((state.DayFraction - .25) * Math.Tau) * 7 - state.RainIntensity * 3;
        var depth = Math.Max(0, position.Y - world.Surface[position.X]);
        var insulation = Math.Clamp(depth / 30.0, 0, .85);
        return surfaceTemperature * (1 - insulation) + baseline * insulation;
    }

    public static double LocalRain(SimulationState state, int x) => state.RainIntensity * BiomeProfile.For(state.World.Biomes[x]).RainChance;

    public void Step(SimulationState state)
    {
        if (state.Tick % (30*SimulationState.TicksPerSecond) == 0)
        {
            var probability = state.Season == 1 ? .22 : .48;
            state.RainIntensity = state.Random.NextDouble() < probability ? .25 + state.Random.NextDouble() * .75 : 0;
        }
        state.DryTicks = state.RainIntensity == 0 ? state.DryTicks + 1 : 0;
        var world = state.World;
        RefreshResources(world);
        for (var x = 0; x < world.Width; x++)
        {
            var heat = Math.Max(0, TemperatureAt(state, new(x, world.Surface[x] - 1)));
            world.Moisture[x] = Math.Clamp(world.Moisture[x] + LocalRain(state, x) * .008 - .00015 - heat * .00001, 0, 1);
            var position = new CellPosition(x, world.Surface[x]);
            var cell = world.GetCell(position, false);
            world.SetCell(position, cell with { Moisture = cell.SolidFill == 0 ? (byte)0 : (byte)Math.Round(world.Moisture[x] * 100),
                Temperature = (short)Math.Round(TemperatureAt(state, position) * 10), Flags = cell.Flags | CellFlags.EnvironmentChanged });
        }
        foreach (var entry in regeneratingResources)
        {
            var definition = world.ResourceCatalog.Get(entry.ResourceId);
            // Finite mineral/organic deposits never replenish their geological budget.
            if (entry.Stack.DepositId != 0) continue;
            var current = world.GetResource(entry.Position, entry.ResourceId);
            var profile = BiomeProfile.For(world.Biomes[entry.Position.X]);
            var cell = world.GetCell(entry.Position, false);
            var depth = Math.Max(0, (double)(entry.Position.Y - world.Surface[entry.Position.X]) / (world.Height - world.Surface[entry.Position.X]));
            if (!definition.Occurrence.Allows(cell, depth)) continue;
            var growth = definition.Regeneration * profile.Regeneration / .03 * SeasonalGrowth[state.Season] * (.08 + world.Moisture[entry.Position.X]) * profile.Fertility;
            var next = current with { Quantity = Math.Min(current.Capacity, current.Quantity + growth) };
            if (next != current) world.SetResource(entry.Position, entry.ResourceId, next);
        }
        foreach (var trail in state.Pheromones.Values) trail.Evaporate(state.Tick,state.RainIntensity);
    }
}
