namespace AntWorld.Domain;

public sealed record BiomeProfile(double Temperature, double Humidity, double Fertility,
    double RainChance, double ResourceDensity, double Regeneration)
{
    // Profiles are immutable and shared: sampling a cell must not allocate a biome object.
    private static readonly BiomeProfile[] Profiles =
    [
        new(23, .72, .9, .45, .75, .035),
        new(21, .5, .8, .28, .6, .03),
        new(34, .12, .18, .04, .2, .01),
        new(27, .95, .85, .65, .7, .028),
        new(5, .4, .3, .18, .32, .014)
    ];
    public static BiomeProfile For(BiomeKind kind) => (uint)kind < Profiles.Length
        ? Profiles[(int)kind] : throw new ArgumentOutOfRangeException(nameof(kind));
}
