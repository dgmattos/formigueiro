using AntWorld.Domain;
namespace AntWorld.Simulation;
public static class AntFactory
{
    public static Ant Create(SimulationState state,Colony colony,AntCaste caste=AntCaste.Worker,LifeStage stage=LifeStage.Adult,Genome? genes=null,int? motherId=null)
    {
        var ant=new Ant(state.NextAntId++,colony.Id,colony.Nest,genes??colony.QueenGenome) { Caste=caste,Stage=stage,SpeciesId=colony.Species,
            LineageId=colony.Queen?.LineageId??colony.Id,MotherId=motherId,CurrentTask=AntTask.Rest };
        var maximum=stage==LifeStage.Adult?state.Species[colony.Species].For(caste).MaximumHealth:20;
        ant.RestoreHealth(maximum,maximum,.1); ant.Signature=colony.Signature.Individual(ant.Id);
        return ant;
    }
}
