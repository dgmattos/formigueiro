namespace AntWorld.Domain;
public sealed record NestTunnel(int Id,int FromChamberId,int ToChamberId,IReadOnlyList<CellPosition> Path,
    NestStructureStatus Status=NestStructureStatus.Planned)
{
    public IReadOnlyList<CellPosition> Cells { get; init; } = Path;
    public bool IsAlternative { get; init; }
    public int Width { get; init; } = 1;
    public double EstimatedCost { get; init; }
}
