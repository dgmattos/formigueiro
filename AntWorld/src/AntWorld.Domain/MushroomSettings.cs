namespace AntWorld.Domain;

public sealed record MushroomSettings
{
    public double UndergroundRedChance { get; init; } = .1;
    public double UndergroundBrownChance { get; init; } = .2;
    public double SurfaceRedChance { get; init; } = .5;
    public double SurfaceBrownChance { get; init; } = .6;
    public double AdjacentBonus { get; init; } = .08;
    public int RenewalIntervalSeconds { get; init; } = 3600;
    public int MinimumPerChunk { get; init; } = 2;
    public int MaximumPerChunk { get; init; } = 30;
    public double FoodPerMushroom { get; init; } = 4;

    public void Validate()
    {
        ReadOnlySpan<double> probabilities = [UndergroundRedChance, UndergroundBrownChance, SurfaceRedChance, SurfaceBrownChance, AdjacentBonus];
        foreach (var probability in probabilities)
            if (!double.IsFinite(probability) || probability is < 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(probability), "Chances dos cogumelos devem estar entre 0 e 1.");
        if (RenewalIntervalSeconds is < 1 or > 86400 || MinimumPerChunk < 0 || MaximumPerChunk < MinimumPerChunk || MaximumPerChunk > 8192)
            throw new ArgumentException("Intervalo ou limites de cogumelos por chunk inválidos.");
        if (!double.IsFinite(FoodPerMushroom) || FoodPerMushroom is <= 0 or > 10000)
            throw new ArgumentOutOfRangeException(nameof(FoodPerMushroom));
    }

    public double Chance(ushort type, bool surface, bool adjacent)
    {
        var chance = type switch
        {
            ResourceIds.RedMushroom => surface ? SurfaceRedChance : UndergroundRedChance,
            ResourceIds.BrownMushroom => surface ? SurfaceBrownChance : UndergroundBrownChance,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        return Math.Min(1, chance + (adjacent ? AdjacentBonus : 0));
    }
}
