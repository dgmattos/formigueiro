namespace AntWorld.Domain;
public sealed record BiomeVegetationSettings(
    double LargeTrees = .02, double SmallTrees = .04, double YoungTrees = .02,
    double FallenLogs = .02, double Stumps = .015, double DeadWood = .01,
    double GrassClumps = .18, double TallGrass = .12, double Bushes = .08,
    double BerryBushes = .03, double Flowers = .12, double SmallPlants = .1, double GrassCoverage = .95)
{
    public double Density(VegetationKind kind) => kind switch
    {
        VegetationKind.TreeLarge => LargeTrees, VegetationKind.TreeSmall => SmallTrees,
        VegetationKind.TreeYoung => YoungTrees, VegetationKind.FallenLog => FallenLogs,
        VegetationKind.TreeStump => Stumps, VegetationKind.DeadWood => DeadWood,
        VegetationKind.GrassClump => GrassClumps, VegetationKind.TallGrass => TallGrass,
        VegetationKind.Bush => Bushes, VegetationKind.BerryBush => BerryBushes,
        VegetationKind.Flower => Flowers, VegetationKind.SmallPlant => SmallPlants,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    public void Validate()
    {
        foreach (var kind in Enum.GetValues<VegetationKind>())
            if (!double.IsFinite(Density(kind)) || Density(kind) is < 0 or > 1) throw new ArgumentException("Densidade vegetal deve estar entre 0 e 1.");
        if (!double.IsFinite(GrassCoverage) || GrassCoverage is < 0 or > 1) throw new ArgumentException("Cobertura de grama inválida.");
    }
}
