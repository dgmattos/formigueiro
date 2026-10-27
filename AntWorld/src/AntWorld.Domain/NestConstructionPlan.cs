namespace AntWorld.Domain;
public sealed class NestConstructionPlan
{
    public List<NestConstructionJob> Jobs { get; } = [];
    public long LastPlanningTick { get; set; }
    public int Revision { get; set; }
    public bool HasPending => Jobs.Any(j=>j.Status is not (NestStructureStatus.Completed or NestStructureStatus.Cancelled));
}
