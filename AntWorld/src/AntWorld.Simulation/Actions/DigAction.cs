using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class DigAction() : AntAction(AntTask.Dig,AntCapability.Dig,.4,SkillType.Digging)
{
    protected override bool IsValid(AntContext c) => c.Ant.Destination is {} p && c.State.World.Contains(p) && c.State.World.Distance(c.Ant.Position,p)==1 &&
        c.State.World.GetCell(p,false).SolidFill>0 && c.State.World.GetCell(p,false).WaterAmount<200;
    protected override double EnergyCost(AntContext c) => c.Ant.Destination is {} p && c.State.World.Contains(p)
        ? ExcavationRules.EnergyCost(c.State.Species[c.Ant.SpeciesId], c.State.World.GetCell(p, false)) : ExcavationRules.SoilEnergyCost;
    protected override bool Apply(AntContext c)
    {
        var p=c.Ant.Destination!.Value; var cell=c.State.World.GetCell(p,false);
        var power=ExcavationRules.Work(c,cell);
        var fill=(byte)Math.Max(0,cell.SolidFill-Math.Max(1,(int)power));
        c.State.World.SetCell(p,fill==0 ? new WorldCell { Temperature=cell.Temperature,WaterAmount=cell.WaterAmount,Flags=CellFlags.Excavated } : cell with { SolidFill=fill });
        // Deposits remain at their coordinates and keep their finite budgets; excavation does not extract them.
        c.Ant.Movement=MovementState.Digging; return true;
    }
}
