using AntWorld.Domain;
namespace AntWorld.WorldGeneration;

public sealed class WorldGenerator
{
    public const int Version = 3;
    public WorldMap Generate(WorldConfiguration configuration)
    {
        configuration.Validate();
        var biomes = new BiomeDistributor().Distribute(configuration);
        var terrain = new TerrainGenerator(); var surface = terrain.CreateSurface(configuration);
        var source = new ProceduralChunkSource(configuration, biomes, surface, terrain);
        var world = new WorldMap(configuration, source, biomes, surface);
        new VegetationGenerator().Populate(world);
        return world;
    }
}
