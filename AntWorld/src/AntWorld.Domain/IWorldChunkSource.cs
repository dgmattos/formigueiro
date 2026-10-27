namespace AntWorld.Domain;

public interface IWorldChunkSource
{
    ResourceStack SampleResource(CellPosition position, ushort resourceId);
    IEnumerable<ResourceEntry> EnumerateResources();
    WorldCell SampleCell(CellPosition position);
    ResourceStorage GenerateResources(ChunkRegion region);
    WorldChunk Generate(ChunkRegion region);
    IReadOnlyList<DepositDefinition> Deposits { get; }
}
