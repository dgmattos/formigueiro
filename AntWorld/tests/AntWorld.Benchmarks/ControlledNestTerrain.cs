using AntWorld.Domain;
namespace AntWorld.Benchmarks;
internal sealed class ControlledNestTerrain : IWorldChunkSource
{
    public IReadOnlyList<DepositDefinition> Deposits=>[];
    public WorldCell SampleCell(CellPosition p)=>p.Y<16?default:new(){SolidFill=255,SoilMaterial=MaterialCatalog.Loam,RockMaterial=MaterialCatalog.Granite,RockFraction=(byte)(p.Y>110?255:0)};
    public ResourceStack SampleResource(CellPosition p,ushort id)=>default;
    public IEnumerable<ResourceEntry> EnumerateResources()=>[];
    public ResourceStorage GenerateResources(ChunkRegion region)=>new();
    public WorldChunk Generate(ChunkRegion region)=>new(region,Enumerable.Range(0,region.Width*region.Height).Select(i=>SampleCell(region.Position(i))).ToArray(),new());
}
