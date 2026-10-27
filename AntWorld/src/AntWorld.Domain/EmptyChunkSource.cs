namespace AntWorld.Domain;

public sealed class EmptyChunkSource : IWorldChunkSource
{
    public IReadOnlyList<DepositDefinition> Deposits => Array.Empty<DepositDefinition>();
    public WorldCell SampleCell(CellPosition position) => default;
    public ResourceStack SampleResource(CellPosition position, ushort resourceId) => default;
    public IEnumerable<ResourceEntry> EnumerateResources() => Array.Empty<ResourceEntry>();
    public ResourceStorage GenerateResources(ChunkRegion region) => new();
    public WorldChunk Generate(ChunkRegion region) => new(region, new WorldCell[region.Width * region.Height], new());
}
