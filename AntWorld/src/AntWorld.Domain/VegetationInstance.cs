namespace AntWorld.Domain;
public readonly record struct VegetationInstance(int Id, VegetationKind Kind, CellPosition Base, BiomeKind Biome,
    long BornTick, long InitialAgeTicks, float Health, bool Removed = false)
{
    public VegetationDefinition Definition => VegetationDefinition.For(Kind);
    public long AgeAt(long tick) => InitialAgeTicks + Math.Max(0, tick - BornTick);
    public string GrowthState => Removed ? "Removed" : Kind == VegetationKind.TreeYoung ? "Young" : Definition.PlantSpecies == "dead_wood" ? "Remnant" : "Mature";
    public int OwnerChunk(WorldMap world) => world.ChunkManager.ChunkId(Base);
}
