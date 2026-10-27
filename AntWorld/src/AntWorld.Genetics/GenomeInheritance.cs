using AntWorld.Domain;
namespace AntWorld.Genetics;

public sealed class GenomeInheritance
{
    // Mutations have no knowledge of species, environment, winner or fitness score.
    public Genome Inherit(Genome parent, DeterministicRandom random, double mutationRate = .12)
    {
        parent.Validate();
        if (!double.IsFinite(mutationRate) || mutationRate is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(mutationRate));
        double Mutate(double value, double min, double max)
        {
            if (random.NextDouble() >= mutationRate) return value;
            return Math.Clamp(value + (random.NextDouble() * 2 - 1) * (max - min) * .04, min, max);
        }
        return new(Mutate(parent.Aggression, 0, 1), Mutate(parent.Strength, .2, 3),
            Mutate(parent.Metabolism, .3, 3), Mutate(parent.Exploration, .1, 2),
            Mutate(parent.ThermalOptimum, -5, 40), Mutate(parent.ColdTolerance, 3, 25), Mutate(parent.Digging, .1, 2));
    }
}
