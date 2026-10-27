using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class DigAction() : AntAction(AntTask.Dig,AntCapability.Dig,.4,SkillType.Digging)
{
    private readonly ExcavationAccessGuard accessGuard=new();
    protected override bool IsValid(AntContext c) => c.Ant.Carrying == 0 && c.Ant.CarriedAntId is null && AntCapabilities.Capacity(c.State,c.Ant) >= 1 && c.Ant.Destination is {} p && c.State.World.Contains(p) && c.State.World.Distance(c.Ant.Position,p)==1 &&
        c.State.World.GetCell(p,false).SolidFill>0 && c.State.World.GetCell(p,false).WaterAmount<200 && OwnsJob(c,p) && SafeToFinish(c,p);
    private static bool OwnsJob(AntContext c,CellPosition p)
    {
        var job=c.Colony.Layout?.Construction.Jobs.FirstOrDefault(j=>j.Target==p);
        return job is null || job.AssignedAntId==c.Ant.Id;
    }
    private bool SafeToFinish(AntContext c,CellPosition p) =>
        c.Colony.Layout?.Construction.Jobs.Any(j=>j.Target==p)!=true ||
        c.State.World.GetCell(p,false).SolidFill>Math.Max(1,(int)ExcavationRules.Work(c,c.State.World.GetCell(p,false))) || accessGuard.CanRemove(c.State,c.Colony,p);
    protected override double EnergyCost(AntContext c) => c.Ant.Destination is {} p && c.State.World.Contains(p)
        ? ExcavationRules.EnergyCost(c.State.Species[c.Ant.SpeciesId], c.State.World.GetCell(p, false)) : ExcavationRules.SoilEnergyCost;
    protected override bool Apply(AntContext c)
    {
        var p=c.Ant.Destination!.Value; var cell=c.State.World.GetCell(p,false);
        var power=ExcavationRules.Work(c,cell);
        var fill=(byte)Math.Max(0,cell.SolidFill-Math.Max(1,(int)power));
        c.State.World.SetCell(p,fill==0 ? new WorldCell { Temperature=cell.Temperature,WaterAmount=cell.WaterAmount,Flags=CellFlags.Excavated } : cell with { SolidFill=fill });
        OrganicNestConstruction.RecordDig(c,p,fill==0);
        if (fill == 0 && c.State.Random.NextDouble() < .05)
        {
            c.Ant.Carrying = 1;
            c.Ant.CargoResourceId = ExcavationSpoil.ResourceId(c.State.World);
            c.Ant.Returning = true;
        }
        // Deposits remain at their coordinates and keep their finite budgets; excavation does not extract them.
        c.Ant.Movement=MovementState.Digging; return true;
    }
}
