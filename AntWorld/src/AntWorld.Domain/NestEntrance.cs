namespace AntWorld.Domain;
public sealed record NestEntrance(int Id,CellPosition Position,int ChamberId,NestStructureStatus Status=NestStructureStatus.Planned)
{
    public IReadOnlyList<CellPosition> Path { get; init; } = [];
    public bool Safe { get; init; } = true;
}
