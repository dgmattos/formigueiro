using AntWorld.Domain;
using Xunit;
namespace AntWorld.Simulation.Tests;

public sealed class GlobalMovementSpeedTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void EveryMobileCasteUsesEightyPercentSpeedWithoutLosingSpeciesModifiers(int species)
    {
        var state=SimulationFactory.Create(new(42,96,64),[new(species,.5){Workers=0,Soldiers=0,Larvae=0}]);
        var world=state.World;var position=new CellPosition(0,10);
        for(var y=0;y<=10;y++)world.SetTerrain(new(0,y),TerrainKind.Air);
        world.SetTerrain(new(0,11),TerrainKind.Soil);world.SetCell(position,world.GetCell(position,false) with{WaterAmount=0});
        var movement=new MovementSystem();
        foreach(var caste in new[]{AntCaste.Queen,AntCaste.Worker,AntCaste.Soldier,AntCaste.MaleReproductive})
        {
            var ant=AntFactory.Create(state,state.Colonies[0],caste);ant.Position=position;ant.Energy=100;ant.Health=ant.MaxHealth;
            var previousSpeed=AntCapabilities.Attributes(state,ant).Speed*state.Species[species].SurfaceMovementMultiplier*ant.Efficiency(SkillType.Navigation);
            Assert.Equal(previousSpeed*.8,movement.EffectiveSpeed(state,ant),10);
            ant.Stage=LifeStage.Larva;Assert.Equal(0,movement.EffectiveSpeed(state,ant));
        }
    }
    [Fact]
    public void ExcavationUsesAlternatingIntegerIntervalsAndKeepsAgentPhases()
    {
        foreach(var id in new[]{1,2,125,10000})
        {
            var ticks=Enumerable.Range(1,1000).Where(t=>ExcavationRules.IsActionTick(t,id)).ToArray();
            Assert.Equal(16,ticks.Length);
            Assert.All(ticks.Zip(ticks.Skip(1),(a,b)=>b-a),gap=>Assert.InRange(gap,62,63));
        }
    }
}
