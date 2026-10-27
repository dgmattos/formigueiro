namespace AntWorld.Domain;
public sealed record VegetationSettings
{
    public bool Enabled { get; init; } = true;
    public bool WoodBlocksMovement { get; init; }
    public byte MinimumMoisture { get; init; } = 20;
    public double MinimumTemperature { get; init; } = -10;
    public double MaximumTemperature { get; init; } = 42;
    public int MinimumTreeSpacing { get; init; } = 6;
    public BiomeVegetationSettings Forest { get; init; } = new(.12, .1, .04, .05, .03, .02, .2, .15, .15, .06, .12, .1);
    public BiomeVegetationSettings Grassland { get; init; } = new();
    public BiomeVegetationSettings Desert { get; init; } = new(0, 0, 0, .005, .005, .005, 0, 0, .01, 0, 0, .02, 0);
    public BiomeVegetationSettings Swamp { get; init; } = new(.02, .04, .02, .03, .02, .02, .12, .25, .15, .03, .05, .15);
    public BiomeVegetationSettings Tundra { get; init; } = new(.005, .01, .005, .01, .01, .01, .06, .03, .02, .005, .02, .03, .65);
    public BiomeVegetationSettings For(BiomeKind biome) => biome switch
    {
        BiomeKind.Forest => Forest, BiomeKind.Grassland => Grassland, BiomeKind.Desert => Desert,
        BiomeKind.Swamp => Swamp, BiomeKind.Tundra => Tundra, _ => throw new ArgumentOutOfRangeException(nameof(biome))
    };
    public void Validate()
    {
        if (MinimumMoisture > 100 || MinimumTreeSpacing is < 1 or > 64 || !double.IsFinite(MinimumTemperature) ||
            !double.IsFinite(MaximumTemperature) || MinimumTemperature >= MaximumTemperature) throw new ArgumentException("Condições de vegetação inválidas.");
        foreach (var biome in Enum.GetValues<BiomeKind>()) For(biome).Validate();
    }
}
