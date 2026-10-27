using AntWorld.Domain;
namespace AntWorld.Simulation;
public static class AntCapabilities
{
    public static CasteDefinition Definition(SimulationState state,Ant ant) => state.Species[ant.SpeciesId].For(ant.Caste);
    public static bool Has(SimulationState state,Ant ant,AntCapability capability) => ant.IsAlive && ant.Stage==LifeStage.Adult && (Definition(state,ant).Capabilities&capability)==capability;
    public static AntAttributes Attributes(SimulationState state,Ant ant)
    {
        var definition=Definition(state,ant);
        if(ant.Stage!=LifeStage.Adult) return new(0,0,0,0,0,0,1/ant.Genes.Metabolism);
        return new(definition.Attack*ant.Genes.Strength,definition.Defense,definition.Speed*(.6+ant.Genes.Exploration*.4),
            2+ant.Genes.Exploration*3,Capacity(state,ant),ant.Genes.Strength*ant.Genes.Digging*ant.Efficiency(SkillType.Digging),1/ant.Genes.Metabolism);
    }
    public static double Capacity(SimulationState state,Ant ant) => Definition(state,ant).CarryCapacity*ant.Genes.Strength*ant.Efficiency(SkillType.Carrying)*(.3+.7*ant.Health/ant.MaxHealth);
}
