namespace AntWorld.Domain;
public sealed record VegetationDefinition(VegetationKind Kind, string PlantSpecies, int VisualWidth, int VisualHeight, int SupportWidth, bool IsTree)
{
    // Optional wood collision uses a ground-level footprint, independent of the canopy/image size.
    public int CollisionWidth => PlantSpecies == "dead_wood" ? SupportWidth : 0;
    public int CollisionHeight => CollisionWidth > 0 ? 1 : 0;
    private static readonly VegetationDefinition[] Catalog =
    [
        new(VegetationKind.TreeLarge, "plains_tree", 6, 7, 1, true),
        new(VegetationKind.TreeSmall, "plains_tree", 4, 5, 1, true),
        new(VegetationKind.TreeYoung, "plains_tree", 2, 3, 1, true),
        new(VegetationKind.FallenLog, "dead_wood", 3, 2, 3, false),
        new(VegetationKind.TreeStump, "dead_wood", 2, 2, 1, false),
        new(VegetationKind.DeadWood, "dead_wood", 2, 1, 2, false),
        new(VegetationKind.GrassClump, "grass", 1, 1, 1, false),
        new(VegetationKind.TallGrass, "grass", 1, 2, 1, false),
        new(VegetationKind.Bush, "plains_bush", 2, 2, 1, false),
        new(VegetationKind.BerryBush, "berry_bush", 2, 2, 1, false),
        new(VegetationKind.Flower, "wildflower", 1, 1, 1, false),
        new(VegetationKind.SmallPlant, "small_plant", 1, 2, 1, false)
    ];
    public static VegetationDefinition For(VegetationKind kind) => (uint)kind < Catalog.Length ? Catalog[(int)kind] : throw new ArgumentOutOfRangeException(nameof(kind));
}
