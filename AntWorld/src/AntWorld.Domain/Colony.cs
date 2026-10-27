namespace AntWorld.Domain;

public sealed class Colony(int id, int species, CellPosition nest, Genome queenGenome, int generation = 0)
{
    public int Id { get; } = id;
    public int Species { get; } = species;
    private CellPosition legacyNest = nest;
    public NestLayout? Layout { get; set; }
    public CellPosition Nest => Layout?.Designated(Layout.QueenChamberId)?.Access ?? legacyNest;
    public CellPosition FoodStorage => Layout?.Designated(Layout.FoodStorageChamberId)?.Access ?? Nest;
    public int ShaftX { get; set; } = nest.X;
    public ForagingMemory Foraging { get; } = new();
    public void RelocateChamber(CellPosition position) { Layout=null; legacyNest=position; }
    public int ExpansionLevel { get; private set; }
    public void AdvanceFloor()
    {
        legacyNest = BroodChamber; Layout=null;
        ExpansionLevel++;
    }
    public void RestoreExpansionLevel(int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        ExpansionLevel = level;
    }
    public const int BroodChamberDepth = 4;
    public CellPosition BroodChamber => Layout?.Designated(Layout.BroodChamberId)?.Access ?? new(Nest.X, Nest.Y + BroodChamberDepth);
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
