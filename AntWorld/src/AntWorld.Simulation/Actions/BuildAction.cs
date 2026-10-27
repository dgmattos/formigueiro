using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class BuildAction() : AntAction(AntTask.Build,AntCapability.Build,.4,SkillType.Construction)
{
    protected override bool IsValid(AntContext c) => c.Ant.Carrying>=1 && c.Ant.CargoResourceId==ResourceIds.OrganicMatter &&
        c.Ant.Destination is {} p && c.State.World.Contains(p) && c.State.World.Distance(c.Ant.Position,p)==1 && c.State.World.GetCell(p,false) is { SolidFill:>=96 and <255,RockFraction:<170 };
    protected override bool Apply(AntContext c)
    {
        var p=c.Ant.Destination!.Value; var cell=c.State.World.GetCell(p,false);
        c.State.World.SetCell(p,cell with { SolidFill=(byte)Math.Min(255,cell.SolidFill+16) }); c.Ant.Carrying--; return true;
    }
}
