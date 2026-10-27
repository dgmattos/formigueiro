using AntWorld.Domain;
using Xunit;
namespace AntWorld.Simulation.Tests;

public sealed class InspectionTests
{
    [Fact]
    public void IndividualReportsHungerAsInverseNutritionAndAgeInSimulationDays()
    {
        var ant=new Ant(1,1,new(4,5),Genome.Ember){Caste=AntCaste.Worker,WorkerRole=WorkerRole.Collector,Nutrition=.75,Age=SimulationState.TicksPerDay+1200,QueenEscort=true};
        var details=InspectionDetails.Individual(ant);
        Assert.Contains("Fome: 25,0%",details);Assert.Contains("Nutrição: 75,0%",details);
        Assert.Contains("Idade: 1 dias, 0h 1min",details);Assert.Contains("Casta: Operária",details);Assert.Contains("Escolta da rainha: Sim",details);
        ant.Health=0;Assert.Contains("Estado: Morto",InspectionDetails.Individual(ant));
        ant.Stage=LifeStage.Larva;Assert.Contains("Fase: Larva",InspectionDetails.Individual(ant));
        Assert.Contains("Casta: Operária",InspectionDetails.Individual(ant));
    }
    [Fact]
    public void BlockShowsFreeWaterSeparatelyAndNormalizesCircularCoordinatesWithoutLoadingChunks()
    {
        var world=new WorldMap(new(42,64,48));var position=new CellPosition(63,10);
        world.SetTerrain(position,TerrainKind.Air);world.AddWater(position,25);
        world.SetResource(position,ResourceIds.Food,new(3,4,10000));
        var before=world.GetCell(position,false);var loaded=world.ChunkManager.LoadedCount;
        var details=InspectionDetails.Block(world,new(-1,10));
        Assert.Contains("Posição: 63, 10",details);Assert.Contains("Tipo: Ar",details);
        Assert.Contains("Umidade: 0%",details);Assert.Contains("Água livre: 25",details);Assert.Contains("Alimento: 3,0 / 4,0",details);
        Assert.Equal(before,world.GetCell(position,false));Assert.Equal(loaded,world.ChunkManager.LoadedCount);
        world.AddWater(position,10);Assert.Contains("Água livre: 35",InspectionDetails.Block(world,position));
    }
}
