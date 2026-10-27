using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class HealthBasedLongevityPolicy : ILongevityPolicy
{
    public bool ShouldDie(Ant ant,CasteDefinition definition,SimulationState state,double elapsedSeconds)
    {
        if(definition.LongevityDays<=0) return false;
        var geneticPotential=.7+ant.Genes.ColdTolerance/30;
        var relativeAge=ant.Age/(SimulationState.TicksPerDay*definition.LongevityDays*geneticPotential);
        if(relativeAge<.5) return false;
        var condition=(ant.Health/ant.MaxHealth+ant.Nutrition+ant.Hydration+ant.BiologicalCondition)/4;
        var rate=Math.Pow(relativeAge-.5,3)*(2-condition)/240;
        return state.Random.NextDouble()<1-Math.Exp(-rate*elapsedSeconds);
    }
}
