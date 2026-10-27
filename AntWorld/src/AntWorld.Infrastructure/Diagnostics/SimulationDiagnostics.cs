using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AntWorld.Domain;
namespace AntWorld.Infrastructure;

// Receives simulation evidence in memory; disk I/O only occurs on Flush, outside ticks.
public sealed class SimulationDiagnostics : IDisposable
{
    private const int MaximumPendingRecords = 4096;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter() } };
    private readonly SimulationLogSettings settings;
    private readonly Queue<string> pending = new();
    private readonly Dictionary<DamageCause, int> deathsByCause = [];
    private readonly string sessionId = Guid.NewGuid().ToString("N");
    private StreamWriter? writer;
    private long nextSampleTick, bytesWritten, lastTick;
    private int dropped, totalDeaths;
    private bool extinct, disposed;
    public string FilePath { get; }
    public string? Failure { get; private set; }
    public SimulationDiagnostics(string directory, SimulationState state, SimulationLogSettings? settings = null)
    {
        this.settings = settings ?? new(); this.settings.Validate();
        FilePath = Path.GetFullPath(Path.Combine(directory, "simulation.jsonl"));
        Directory.CreateDirectory(directory); Rotate();
        Enqueue("session_started", state.Tick, new
        {
            state.World.Configuration, species = state.Species, state.Performance, state.Survival,
            ticksPerSecond = SimulationState.TicksPerSecond, secondsPerDay = SimulationState.SecondsPerDay,
            state.Random.State, runtime = Environment.Version.ToString(), format = 1,
            snapshotVersion = BinarySnapshotCodec.Version
        });
        Observe(state);
    }
    public void RecordDeath(AntDeath death)
    {
        totalDeaths++;
        deathsByCause[death.Causes] = deathsByCause.GetValueOrDefault(death.Causes) + 1;
        Enqueue("ant_died", death.Tick, death);
    }
    public void Observe(SimulationState state)
    {
        lastTick = state.Tick;
        if (disposed || Failure is not null) return;
        if (state.Ants.Count == 0 && !extinct)
        {
            extinct = true;
            Enqueue("population_extinct", state.Tick, new { totalDeaths, deathsByCause });
        }
        if (state.Tick < nextSampleTick) return;
        nextSampleTick = state.Tick + settings.SampleIntervalSeconds * (long)SimulationState.TicksPerSecond;
        Enqueue("population_sample", state.Tick, new
        {
            population = state.Ants.Count, totalDeaths, deathsByCause,
            colonies = state.Colonies.Select(c =>
            {
                var ants = state.Ants.Where(a => a.ColonyId == c.Id).ToArray();
                return new
                {
                    c.Id, c.Species, c.Nest, biome = state.World.Biomes[c.Nest.X], population = ants.Length,
                    c.Food, c.Births, c.Deaths, c.QueenHealth, c.ExpansionLevel, c.BroodChamber,
                    workerRoles = ants.Where(a => a.Caste == AntCaste.Worker && a.Stage == LifeStage.Adult).GroupBy(a => a.WorkerRole).ToDictionary(g => g.Key, g => g.Count()),
                    meanHealthPercent = ants.Length == 0 ? 0 : ants.Average(a => a.Health / a.MaxHealth),
                    meanEnergy = ants.Length == 0 ? 0 : ants.Average(a => a.Energy),
                    meanNutrition = ants.Length == 0 ? 0 : ants.Average(a => a.Nutrition),
                    meanHydration = ants.Length == 0 ? 0 : ants.Average(a => a.Hydration),
                    emptyEnergy = ants.Count(a => a.Energy == 0), starving = ants.Count(a => a.Nutrition == 0),
                    dehydrated = ants.Count(a => a.Hydration == 0),
                    tasks = ants.GroupBy(a => a.CurrentTask).ToDictionary(g => g.Key, g => g.Count()),
                    stages = ants.GroupBy(a => a.Stage).ToDictionary(g => g.Key, g => g.Count())
                };
            }).ToArray()
        });
    }
    public void RecordPlayback(long tick, int multiplier, double pendingSeconds, double measuredTicksPerSecond) =>
        Enqueue("playback", tick, new { multiplier, pendingSeconds, measuredTicksPerSecond, effectiveMultiplier = measuredTicksPerSecond / SimulationState.TicksPerSecond });
    private void Enqueue(string type, long tick, object data)
    {
        if (disposed || Failure is not null) return;
        lastTick = Math.Max(lastTick, tick);
        if (pending.Count >= MaximumPendingRecords) { dropped++; return; }
        pending.Enqueue(JsonSerializer.Serialize(new
        {
            timestampUtc = DateTimeOffset.UtcNow, sessionId, type, tick,
            simulatedSeconds = tick * SimulationState.TickSeconds,
            day = tick / SimulationState.TicksPerDay + 1,
            dayProgress = (double)(tick % SimulationState.TicksPerDay) / SimulationState.TicksPerDay,
            data
        }, JsonOptions));
    }
    public void Flush()
    {
        if (disposed || Failure is not null || pending.Count == 0 && dropped == 0) return;
        try
        {
            if (dropped > 0 && pending.Count < MaximumPendingRecords)
            { Enqueue("records_dropped", 0, new { count = dropped }); dropped = 0; }
            while (pending.TryDequeue(out var line))
            {
                var bytes = Encoding.UTF8.GetByteCount(line) + 1;
                if (bytesWritten + bytes > settings.MaxFileSizeMb * 1024L * 1024 && bytesWritten > 0) Rotate();
                writer!.WriteLine(line); bytesWritten += bytes;
            }
            writer!.Flush();
            if (dropped > 0) { Enqueue("records_dropped", 0, new { count = dropped }); dropped = 0; Flush(); }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { Failure = $"Diagnóstico desativado: {ex.Message}"; pending.Clear(); }
    }
    private void Rotate()
    {
        writer?.Dispose(); writer = null;
        for (var i = settings.RetainedFiles - 1; i >= 1; i--)
        {
            var source = i == 1 ? FilePath : FilePath + $".{i - 1}";
            if (File.Exists(source)) File.Move(source, FilePath + $".{i}", true);
        }
        writer = new StreamWriter(new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.Read), new UTF8Encoding(false)) { NewLine = "\n" };
        bytesWritten = 0;
    }
    public void Dispose()
    {
        if (disposed) return;
        Enqueue("session_closed", lastTick, new { totalDeaths, deathsByCause });
        Flush(); disposed = true;
        try { writer?.Dispose(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Failure ??= ex.Message; }
    }
}
