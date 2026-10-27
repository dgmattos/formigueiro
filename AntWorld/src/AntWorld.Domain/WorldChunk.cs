namespace AntWorld.Domain;

public sealed class WorldChunk(ChunkRegion region, WorldCell[] cells, ResourceStorage resources)
{
    public ChunkRegion Region { get; } = region;
    internal WorldCell[] CellData { get; } = cells;
    public ReadOnlyMemory<WorldCell> Cells => CellData;
    public ResourceStorage Resources { get; } = resources;
}
