namespace AntWorld.Domain;
public sealed record NestChamber(int Id, NestChamberType Type, CellPosition Center, int RadiusX, int RadiusY,
    NestStructureStatus Status = NestStructureStatus.Planned)
{
    public CellPosition Access { get; init; } = new(Center.X,Center.Y+RadiusY);
    public IReadOnlyList<CellPosition> Cells { get; init; } = [];
    public int Capacity { get; init; } = 1;
}
