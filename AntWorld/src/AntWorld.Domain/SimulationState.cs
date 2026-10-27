namespace AntWorld.Domain;

public sealed class SimulationState(WorldMap world, DeterministicRandom random)
{
    public WorldMap World { get; } = world;
    public DeterministicRandom Random { get; } = random;
    public List<Colony> Colonies { get; } = [];
    public List<Ant> Ants { get; } = [];
    public Dictionary<int, PheromoneField> Pheromones { get; } = [];
    public ColonyRelations Relations { get; } = new();
    public PerformanceProfile Performance { get; set; } = new();
    public Dictionary<int, SpeciesDefinition> Species { get; } = new() { [0] = new(), [1] = new(), [2] = SpeciesDefinition.Strong };
    public int SchedulerCursor { get; set; }
    public const int TicksPerSecond = 20;
    public const double TickSeconds = 1.0 / TicksPerSecond;
    public long Tick { get; set; }
    public int NextAntId { get; set; } = 1;
    public int NextColonyId { get; set; } = 1;
    public double RainIntensity { get; set; }
    public int DryTicks { get; set; }
    public const int SecondsPerDay = 3 * 60 * 60;
    public const int TicksPerDay = SecondsPerDay * TicksPerSecond;
    public const int DaysPerMonth = 28;
    public const int MonthsPerYear = 4;
    public const int DaysPerYear = DaysPerMonth * MonthsPerYear;
    public const int DaysPerSeason = DaysPerMonth;
    public int Day => (int)(Tick / TicksPerDay);
    public int DayOfMonth => Day % DaysPerMonth + 1;
    public int Month => Day / DaysPerMonth % MonthsPerYear + 1;
    public int Year => Day / DaysPerYear + 1;
    public int Season => Month - 1;
    public double DayFraction => (double)(Tick % TicksPerDay) / TicksPerDay;
    public bool IsNight => DayFraction < .22 || DayFraction > .78;
}
