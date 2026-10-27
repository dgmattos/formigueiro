namespace AntWorld.Domain;

public static class MaterialCatalog
{
    public const ushort Loam = 1, Sand = 2, Clay = 3, Granite = 4, Ice = 5;
    private static readonly IReadOnlyDictionary<ushort, MaterialDefinition> Definitions =
        new Dictionary<ushort, MaterialDefinition>
        {
            [Loam] = new(Loam, "Terra", MaterialFamily.Soil, 150, 130, .9),
            [Sand] = new(Sand, "Areia", MaterialFamily.Soil, 110, 230, .35),
            [Clay] = new(Clay, "Argila", MaterialFamily.Soil, 190, 35, 1),
            [Granite] = new(Granite, "Granito", MaterialFamily.Rock, 25, 12, .15),
            [Ice] = new(Ice, "Gelo", MaterialFamily.Rock, 0, 0, 0)
        };
    public static MaterialDefinition Get(ushort id) => Definitions.TryGetValue(id, out var value)
        ? value : throw new ArgumentOutOfRangeException(nameof(id), "Material desconhecido.");
}
