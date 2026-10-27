namespace AntWorld.Domain;
public sealed class NestConstructionJob(int id,CellPosition target)
{
    public int Id { get; } = id;
    public CellPosition Target { get; } = target;
    public CellPosition? Support { get; set; }
    public int? AssignedAntId { get; set; }
    public NestStructureStatus Status { get; set; } = NestStructureStatus.WaitingDependencies;
    public long LastProgressTick { get; set; }
    public int WorkActions { get; set; }
    public string? BlockedReason { get; set; }
}
