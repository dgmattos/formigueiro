using System.Numerics;
namespace AntWorld.Domain;
public readonly record struct ChemicalSignature(ulong PatternA, ulong PatternB)
{
    public double Similarity(ChemicalSignature other) => 1 - (BitOperations.PopCount(PatternA ^ other.PatternA) + BitOperations.PopCount(PatternB ^ other.PatternB)) / 128.0;
    public static ChemicalSignature ForColony(int seed, int colony, int species)
    {
        var random = DeterministicRandom.FromSeed(seed, (uint)(colony * 31 + species + 700));
        var a = random.State; random.NextDouble();
        return new(a, random.State);
    }
    public ChemicalSignature Individual(int id) => this with { PatternA = PatternA ^ (1UL << (id % 64)), PatternB = PatternB ^ (1UL << ((id / 64) % 64)) };
    public ChemicalSignature Assimilate(ChemicalSignature reference, int bit)
    {
        var mask = 1UL << (bit & 63);
        return new((PatternA & ~mask) | (reference.PatternA & mask), (PatternB & ~mask) | (reference.PatternB & mask));
    }
}
