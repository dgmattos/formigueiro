namespace AntWorld.Domain;

public sealed record Genome(double Aggression, double Strength, double Metabolism,
    double Exploration, double ThermalOptimum, double ColdTolerance, double Digging)
{
    public void Validate()
    {
        var values = new[] { Aggression, Strength, Metabolism, Exploration, ThermalOptimum, ColdTolerance, Digging };
        if (values.Any(v => !double.IsFinite(v)) || Aggression is < 0 or > 1 || Strength is < .2 or > 3 ||
            Metabolism is < .3 or > 3 || Exploration is < .1 or > 2 || ThermalOptimum is < -5 or > 40 ||
            ColdTolerance is < 3 or > 25 || Digging is < .1 or > 2)
            throw new ArgumentOutOfRangeException(nameof(Genome), "Genes fora dos limites fisiológicos.");
    }
    public static Genome Ember => new(.75, 1.65, 1.45, .65, 28, 10, 1.3);
    public static Genome Moss => new(.25, .8, .7, 1.4, 18, 15, .7);
}
