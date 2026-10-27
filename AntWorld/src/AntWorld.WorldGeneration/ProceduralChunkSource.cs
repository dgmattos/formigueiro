using AntWorld.Domain;
namespace AntWorld.WorldGeneration;

public sealed class ProceduralChunkSource : IWorldChunkSource
{
    private static readonly ushort[] GeneratedResourceIds = [ResourceIds.Food, ResourceIds.Iron, ResourceIds.OrganicMatter, ResourceIds.Fungus, ResourceIds.RedMushroom, ResourceIds.BrownMushroom];
    private readonly WorldConfiguration configuration;
    private readonly BiomeKind[] biomes;
    private readonly int[] surface;
    private readonly TerrainGenerator terrain;
    private readonly ResourcePlan resources;
    public IReadOnlyList<DepositDefinition> Deposits => resources.Deposits;
    public ProceduralChunkSource(WorldConfiguration configuration, BiomeKind[] biomes, int[] surface, TerrainGenerator terrain)
    {
        this.configuration = configuration; this.biomes = biomes; this.surface = surface; this.terrain = terrain;
        resources = new ResourceDistributor().CreatePlan(configuration, biomes, surface, SampleCell);
    }
    public WorldCell SampleCell(CellPosition p) => terrain.Sample(configuration, biomes[p.X], surface[p.X], p);
    public ResourceStack SampleResource(CellPosition p, ushort resourceId) => resources.Entries.GetValueOrDefault(new(p.Y * configuration.Width + p.X, resourceId));
    public IEnumerable<ResourceEntry> EnumerateResources() => resources.Entries.Select(pair => new ResourceEntry(new(pair.Key.CellIndex % configuration.Width, pair.Key.CellIndex / configuration.Width), pair.Key.ResourceId, pair.Value));
    public ResourceStorage GenerateResources(ChunkRegion region)
    {
        var storage = new ResourceStorage();
        for (var i = 0; i < region.Width * region.Height; i++)
        foreach (var id in GeneratedResourceIds)
        {
            var stack = SampleResource(region.Position(i), id);
            if (stack.Capacity > 0) storage.Set(new(i, id), stack);
        }
        return storage;
    }
    public WorldChunk Generate(ChunkRegion region)
    {
        var cells = new WorldCell[region.Width * region.Height];
        for (var i = 0; i < cells.Length; i++) cells[i] = SampleCell(region.Position(i));
        return new(region, cells, GenerateResources(region));
    }
}
