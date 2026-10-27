using System.Diagnostics;
using System.Text.Json;
using AntWorld.Domain;
using AntWorld.Simulation;
using AntWorld.Infrastructure;

try
{
    if (args.Contains("--help"))
    {
        Console.WriteLine("AntWorld: --seed N --ticks N --width N --height N [--load arquivo.antworld] [--save arquivo.antworld] [--svg mapa.svg] [--settings settings.json] [--diagnostics diretório]");
        return 0;
    }
    var options = new Dictionary<string, string>();
    var allowed = new[] { "--seed", "--ticks", "--width", "--height", "--load", "--save", "--svg", "--settings", "--diagnostics" };
    for (var i = 0; i < args.Length; i += 2)
    {
        if (!allowed.Contains(args[i]) || i + 1 == args.Length || !options.TryAdd(args[i], args[i + 1])) throw new ArgumentException("Argumento inválido ou repetido. Use --help.");
    }
    int Number(string name, int fallback) => options.TryGetValue(name, out var value) ? int.Parse(value, System.Globalization.CultureInfo.InvariantCulture) : fallback;
    var ticks = Number("--ticks", 1000);
    if (ticks is < 0 or > 10000000) throw new ArgumentOutOfRangeException(nameof(ticks), "Ticks: 0–10.000.000.");
    var files = new BinarySnapshotFile();
    var preset = options.TryGetValue("--settings", out var settingsPath) ? SimulationPreset.Parse(File.ReadAllText(settingsPath)) : null;
    if (preset is not null && new[] { "--load", "--seed", "--width", "--height" }.Any(options.ContainsKey))
        throw new ArgumentException("Use --settings sem --load/--seed/--width/--height para evitar configurações ambíguas.");
    var state = preset is not null ? SimulationFactory.Create(preset.World, preset.Colonies, preset.Survival) : options.TryGetValue("--load", out var load) ? files.Load(load) : SimulationFactory.Create(new(Number("--seed", 12345), Number("--width", 240), Number("--height", 96)));
    var engine = new SimulationEngine(state);
    using var diagnostics = options.TryGetValue("--diagnostics", out var diagnosticDirectory)
        ? new SimulationDiagnostics(diagnosticDirectory, state) : null;
    if (diagnostics is not null) engine.AntDied += diagnostics.RecordDeath;
    var watch = Stopwatch.StartNew();
    for (var i = 0; i < ticks; i++)
    {
        engine.Step(); diagnostics?.Observe(state);
        if (i % 1000 == 0) diagnostics?.Flush();
    }
    watch.Stop(); diagnostics?.Flush();
    if (diagnostics?.Failure is {} failure) Console.Error.WriteLine(failure);
    SnapshotValidator.Validate(state);
    if (options.TryGetValue("--save", out var save)) files.Save(save, state);
    if (options.TryGetValue("--svg", out var svg)) new WorldSvgExporter().Export(svg, state);
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        seed = state.World.Configuration.Seed, tick = state.Tick, day = state.Day,
        season = new[] { "Primavera", "Verão", "Outono", "Inverno" }[state.Season],
        ants = state.Ants.Count, elapsedMilliseconds = watch.Elapsed.TotalMilliseconds,
        ticksPerSecond = ticks / Math.Max(.000001, watch.Elapsed.TotalSeconds),
        colonies = state.Colonies.Select(c => new { c.Id, species = c.Species switch { 3 => "Superfície", 2 => "Forte", 1 => "Musgo", _ => "Normal/Brasa" }, c.Generation, alive = c.IsAlive, population = state.Ants.Count(a => a.ColonyId == c.Id), food = Math.Round(c.Food, 2), queenHealth = Math.Round(c.QueenHealth, 2), c.Births, c.Deaths, c.ExpansionLevel, queenY = c.Queen?.Position.Y, nurseryY = c.BroodChamber.Y,
            collectors = state.Ants.Count(a => a.ColonyId == c.Id && a.WorkerRole == WorkerRole.Collector),
            diggers = state.Ants.Count(a => a.ColonyId == c.Id && a.WorkerRole == WorkerRole.Digger),
            surfaceSpoil = state.World.ResourceCatalog.Definitions.Where(d => d.Kind == ExcavationSpoil.Kind).Sum(d => state.World.GetResource(ExcavationSpoil.Exit(state.World,c), d.Id).Quantity) })
    }, new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}
catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException or IOException or InvalidOperationException)
{
    Console.Error.WriteLine($"AntWorld: {ex.Message}");
    return 1;
}
