using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using AntWorld.Domain;
using AntWorld.WorldGeneration;
using AntWorld.Infrastructure;
using AntWorld.Simulation;

if(args.Contains("--nests")) { AntWorld.Benchmarks.NestBenchmarks.Run(args.Length>1?args[1]:"artifacts/nest-validation-report.json"); return; }

if(args.Contains("--agents")) { AntWorld.Benchmarks.AgentBenchmarks.Run(); return; }

const int repetitions = 5;
var configuration = new WorldConfiguration(42, 512, 128) { MaximumLoadedChunks = 8 };
var world = new WorldGenerator().Generate(configuration);
var regions = world.Chunks().ToArray();
var terrain = new TerrainGenerator();
var source = new ProceduralChunkSource(configuration, world.Biomes, world.Surface, terrain);
var results = new List<object>();
long checksum = 0;
void Measure(string name, int operations, Action action)
{
    action(); // JIT/cache warmup excluded.
    var times = new double[repetitions]; var allocations = new long[repetitions];
    for (var i = 0; i < repetitions; i++)
    {
        var before = GC.GetAllocatedBytesForCurrentThread(); var timer = Stopwatch.StartNew();
        action(); timer.Stop(); times[i] = timer.Elapsed.TotalMilliseconds;
        allocations[i] = GC.GetAllocatedBytesForCurrentThread() - before;
    }
    results.Add(new { name, operations, meanMilliseconds = times.Average(), minimumMilliseconds = times.Min(),
        nanosecondsPerOperation = times.Average() * 1e6 / operations, meanAllocatedBytes = allocations.Average() });
}
Measure("world_metadata_and_deposit_plan", 1, () => checksum += new WorldGenerator().Generate(configuration).Resources().Count());
Measure("chunk_generation", regions.Length, () =>
{
    foreach (var region in regions) { world.ChunkManager.Unload(new(region.X, region.Y)); checksum += world.ChunkManager.Load(new(region.X, region.Y)).Cells.Length; }
});
Measure("logical_cell_queries", 100000, () =>
{
    for (var i = 0; i < 100000; i++) checksum += world.GetCell(new(i % configuration.Width, i * 31 % configuration.Height), false).SolidFill;
});
Measure("orthogonal_neighbors", 100000, () =>
{
    Span<CellPosition> neighbors = stackalloc CellPosition[4];
    for (var i = 0; i < 100000; i++) checksum += world.ChunkManager.GetNeighbors(new(i - 50000, 20), neighbors);
});
Measure("resource_distribution_into_chunks", regions.Length, () =>
{
    foreach (var region in regions) checksum += source.GenerateResources(region).Count;
});
var state = SimulationFactory.Create(configuration);
var codec = new BinarySnapshotCodec(); var baseBytes = codec.Encode(state);
new SimulationEngine(state).Run(100);
var modified = codec.Encode(state);
Measure("serialization", 1, () => checksum += codec.Encode(state).Length);
Measure("deserialization", 1, () => checksum += codec.Decode(modified).World.ChunkManager.CellChanges.Count);
Console.WriteLine(JsonSerializer.Serialize(new { configuration, cellBytes = Marshal.SizeOf<WorldCell>(), repetitions,
    loadedChunks = world.ChunkManager.LoadedCount, baseSnapshotBytes = baseBytes.Length, modifiedSnapshotBytes = modified.Length,
    changedCells = state.World.ChunkManager.CellChanges.Count, changedResources = state.World.ChunkManager.ResourceChanges.Count,
    checksum, results }, new JsonSerializerOptions { WriteIndented = true }));
