using AntWorld.WorldGeneration;
using AntWorld.Domain;
namespace AntWorld.Game;
public sealed record SpriteDefinition
{
    public string Id { get; init; } = "";
    public string Category { get; init; } = "";
    public TerrainVisualType? Terrain { get; init; }
    public VegetationKind? Kind { get; init; }
    public int Variation { get; init; }
    public int[] Region { get; init; } = [];
    public int[] Anchor { get; init; } = [];
    public Dictionary<string, int[]> SeasonAnchors { get; init; } = [];
    public double Weight { get; init; } = 1;
    public bool MirrorX { get; init; }
    public bool RemoveBackground { get; init; }
    public bool KeepLargestComponent { get; init; }
    public bool SnapAnchorToBase { get; init; } = true;
    public bool SeasonInvariant { get; init; }
    public string Material { get; init; } = "";
    public string NeighborRule { get; init; } = "";
    public int VisualWidth { get; init; } = 1;
    public int VisualHeight { get; init; } = 1;
}
