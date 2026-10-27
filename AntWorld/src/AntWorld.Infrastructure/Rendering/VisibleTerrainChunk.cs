using AntWorld.Domain;
namespace AntWorld.Infrastructure;

public readonly record struct VisibleTerrainChunk(int ChunkId, int Copy, int OffsetX, ChunkRegion Region);
