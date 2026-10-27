using AntWorld.Domain;
using AntWorld.WorldGeneration;
using AntWorld.Genetics;
namespace AntWorld.Specifications;

public static class GenerationScenarios
{
    public static void Run()
    {
        var generator = new WorldGenerator();
        foreach (var seed in new[] { 0, 1, -17, int.MaxValue, 12345 })
        {
            var a = generator.Generate(new(seed));
            var b = generator.Generate(new(seed));
            Check.That(a.Terrain.SequenceEqual(b.Terrain) && a.Food.SequenceEqual(b.Food) && a.Biomes.SequenceEqual(b.Biomes), "Geração não determinística.");
            Check.That(a.Biomes.Distinct().Count() == 5, "Bioma ausente.");
            Check.That(a.Terrain.Contains(TerrainKind.Clay) && a.Terrain.Contains(TerrainKind.Rock), "Camadas ausentes.");
            Check.That(Enumerable.Range(0, a.Terrain.Length).Any(i => a.Terrain[i] == TerrainKind.Air && i / a.Width > a.Surface[i % a.Width] + 8), "Cavernas ausentes.");
            Check.That(a.Food.Sum() > 0, "Mundo sem recursos.");
            for (var x = 0; x < a.Width; x++)
            {
                Check.That(a.CanWalk(new(x, a.Surface[x] - 1)), "Superfície inacessível.");
                if (x > 0) Check.That(Math.Abs(a.Surface[x] - a.Surface[x - 1]) <= 1, "Superfície desconectada.");
            }
        }
        Check.That(!generator.Generate(new(1)).Terrain.SequenceEqual(generator.Generate(new(2)).Terrain), "Seed ignorada.");
        var inheritance = new GenomeInheritance();
        var random = DeterministicRandom.FromSeed(5);
        Check.That(inheritance.Inherit(Genome.Ember, random, 0) == Genome.Ember, "Herança sem mutação alterada.");
        var genes = Genome.Moss;
        for (var i = 0; i < 10000; i++) { genes = inheritance.Inherit(genes, random, 1); genes.Validate(); }
        Check.That(genes != Genome.Moss, "Mutação não ocorre.");
    }
}
