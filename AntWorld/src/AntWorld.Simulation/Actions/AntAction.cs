using AntWorld.Domain;
namespace AntWorld.Simulation;
public abstract class AntAction(AntTask task,AntCapability capability,double energyCost,SkillType? skill=null)
{
    public AntTask Task { get; }=task;
    public bool CanExecute(AntContext c) => AntCapabilities.Has(c.State,c.Ant,capability) && c.Ant.CarrierId is null &&
        c.Ant.Energy>=EnergyCost(c) && (c.State.World.GetCell(c.Ant.Position,false).WaterAmount<500 || Task is AntTask.Flee or AntTask.Move) && IsValid(c);
    protected virtual double EnergyCost(AntContext context) => energyCost;
    protected abstract bool IsValid(AntContext context);
    protected abstract bool Apply(AntContext context);
    public bool Execute(AntContext c)
    {
        if(!CanExecute(c)) return false;
        var cost = EnergyCost(c);
        if(!Apply(c)) return false;
        c.Ant.Energy-=cost;
        if(skill is {} type) c.Ant.Learn(type,1);
        c.Ant.CurrentTask=Task;
        return true;
    }
}
