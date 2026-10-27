namespace AntWorld.Domain;

public sealed record ColonySpawnSettings(int SpeciesId, double NestPositionRatio,
    int Queens = 1, int Workers = 21, int Soldiers = 3, int Larvae = 2, double InitialFood = 150)
{
    public long Population => (long)Queens + Workers + Soldiers + Larvae;

    public void Validate()
    {
        if (SpeciesId is not (0 or 1 or 2))
            throw new ArgumentException("colony.speciesId deve ser 0 (Normal/Brasa), 1 (Musgo) ou 2 (Forte).");
        if (!double.IsFinite(NestPositionRatio) || NestPositionRatio is < 0 or >= 1)
            throw new ArgumentException("colony.nestPositionRatio deve estar entre 0 (inclusive) e 1 (exclusive).");
        if (Queens != 1)
            throw new ArgumentException("Cada colônia inicial deve possuir exatamente uma rainha (queens: 1).");
        if (Workers < 0 || Soldiers < 0 || Larvae < 0)
            throw new ArgumentException("As populações iniciais não podem ser negativas.");
        if (!double.IsFinite(InitialFood) || InitialFood is < 0 or > 1e12)
            throw new ArgumentException("colony.initialFood deve estar entre 0 e 1.000.000.000.000.");
    }
}
