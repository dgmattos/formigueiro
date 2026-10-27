using AntWorld.Domain;
namespace AntWorld.WorldGeneration;

public sealed class BiomeDistributor
{
    public BiomeKind[] Distribute(WorldConfiguration configuration)
    {
        var random = DeterministicRandom.FromSeed(configuration.Seed, 1);
        var kinds = Enum.GetValues<BiomeKind>();
        for (var i = kinds.Length - 1; i > 0; i--)
        {
            var other = random.NextInt(i + 1); (kinds[i], kinds[other]) = (kinds[other], kinds[i]);
        }
        return Enumerable.Range(0, configuration.Width).Select(x => kinds[x * kinds.Length / configuration.Width]).ToArray();
    }
}
