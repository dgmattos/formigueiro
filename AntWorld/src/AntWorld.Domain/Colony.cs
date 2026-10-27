namespace AntWorld.Domain;

public sealed class Colony(int id, int species, CellPosition nest, Genome queenGenome, int generation = 0)
{
    public int Id { get; } = id;
    public int Species { get; } = species;
    public CellPosition Nest { get; } = nest;
    public const int BroodChamberDepth = 4;
    public CellPosition BroodChamber => new(Nest.X, Nest.Y + BroodChamberDepth);
    public Genome QueenGenome { get; } = queenGenome;
    public int Generation { get; } = generation;
    public double Food { get; set; } = 150;
    private double queenHealth = 250;
    public Ant? Queen { get; set; }
    public int QueenId { get; set; }
    public ChemicalSignature Signature { get; set; }
    public ColonyPriorities Priorities { get; set; }
    public double QueenHealth { get => Queen?.Health ?? queenHealth; set { queenHealth = value; if (Queen is not null) Queen.Health = value; } }
    public int ReproductionProgress { get; set; }
    public int Births { get; set; }
    public int Deaths { get; set; }
    public bool IsAlive => QueenHealth > 0;
}
