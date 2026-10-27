using AntWorld.Domain;
using AntWorld.Genetics;
namespace AntWorld.Simulation;
public sealed class LayEggAction() : AntAction(AntTask.LayEgg,AntCapability.Reproduce,2,SkillType.Reproduction)
{
    private readonly GenomeInheritance inheritance=new();
    protected override bool IsValid(AntContext c)
    {
        var species=c.State.Species[c.Ant.SpeciesId];
        return c.Ant.StoredSperm>0 && c.Ant.Position==c.Colony.Nest && c.Colony.Food>=species.EggCost && c.Ant.Nutrition>.5 &&
            c.State.Tick-c.Ant.LastEggTick>=species.EggIntervalTicks && PopulationAdmission.CanAdd(c.State);
    }
    protected override bool Apply(AntContext c)
    {
        var choice=c.State.Random.NextDouble();
        var caste=choice<.02 && c.Colony.Food>220 ? AntCaste.Queen : choice<.17 ? AntCaste.Soldier : AntCaste.Worker;
        var egg=AntFactory.Create(c.State,c.Colony,caste,LifeStage.Egg,inheritance.Inherit(c.Ant.Genes,c.State.Random),c.Ant.Id);
        c.State.Ants.Add(egg); c.Colony.Food-=c.State.Species[c.Ant.SpeciesId].EggCost;
        c.Ant.LastEggTick=c.State.Tick; c.Ant.StoredSperm--; c.Colony.Births++; return true;
    }
}
