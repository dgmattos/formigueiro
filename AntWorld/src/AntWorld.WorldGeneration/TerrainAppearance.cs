using AntWorld.Domain;
namespace AntWorld.WorldGeneration;
public readonly record struct TerrainAppearance(TerrainVisualType Type, int Variation, ExposedSides Edges);
