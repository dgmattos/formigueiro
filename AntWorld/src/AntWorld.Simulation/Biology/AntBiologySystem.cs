using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class AntBiologySystem
{
    private readonly NoAgeLimitPolicy unlimited=new();
    private readonly HealthBasedLongevityPolicy aging=new();
    public void Step(SimulationState state,Ant ant)
    {
        if(!ant.IsAlive) return;
        ant.Age++;
        var temperature=EnvironmentSystem.TemperatureAt(state,ant.Position);
        var stress=Math.Max(0,Math.Abs(temperature-ant.Genes.ThermalOptimum)-ant.Genes.ColdTolerance);
        ant.Energy-=.055*ant.Genes.Metabolism*(1+stress*.12)*SimulationState.TickSeconds;
        ant.Nutrition-=.0005*SimulationState.TickSeconds;
        ant.Hydration-=.0006*(1+stress*.05)*SimulationState.TickSeconds;
        ant.Damage((stress*.004+(ant.Energy==0?.8:0)+(ant.Nutrition==0?.3:0)+(ant.Hydration==0?.6:0))*SimulationState.TickSeconds);
        ant.BiologicalCondition=(ant.Nutrition+ant.Hydration+ant.Health/ant.MaxHealth)/3;
        if(ant.Stage!=LifeStage.Adult)
        {
            ant.CurrentTask=AntTask.Rest; ant.Movement=MovementState.Idle;
            var growth=ant.Nutrition*ant.Health/ant.MaxHealth*Math.Clamp(1-Math.Abs(temperature-ant.Genes.ThermalOptimum)/35,0,1);
            ant.DevelopmentTicks+=growth;
            var species=state.Species[ant.SpeciesId];
            var days=ant.Stage switch { LifeStage.Egg=>species.EggDays,LifeStage.Larva=>species.LarvaDays,_=>species.PupaDays };
            if(ant.DevelopmentTicks>=days*SimulationState.TicksPerDay)
            {
                ant.DevelopmentTicks-=days*SimulationState.TicksPerDay; ant.Stage++;
                if(ant.Stage==LifeStage.Adult) { var health=AntCapabilities.Definition(state,ant).MaximumHealth; ant.ChangeMaximumHealth(health); ant.Recover(health); }
            }
            return;
        }
        if(state.Tick%state.Performance.BiologicalIntervalTicks!=ant.Id%state.Performance.BiologicalIntervalTicks) return;
        var definition=AntCapabilities.Definition(state,ant);
        ILongevityPolicy policy=definition.LongevityDays==0 ? unlimited : aging;
        if(policy.ShouldDie(ant,definition,state,state.Performance.BiologicalIntervalTicks*SimulationState.TickSeconds)) ant.Damage(ant.Health);
    }
}
