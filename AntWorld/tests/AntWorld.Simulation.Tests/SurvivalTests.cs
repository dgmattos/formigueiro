using AntWorld.Domain;
using Xunit;
namespace AntWorld.Simulation.Tests;

public sealed class SurvivalTests
{
    private static SimulationState Create() => SimulationFactory.Create(new(42,64,48), [new(0,.25)]);
    private static AntContext Context(SimulationState s, Ant ant)
    { var index=new AgentSpatialIndex(); index.Rebuild(s); return new(s,ant,s.Colonies[0],index,new()); }
    [Fact]
    public void EggReservesSupportRealIncubationButDamageCanStillKillBrood()
    {
        var state=Create(); var colony=state.Colonies[0];
        var genes=Genome.Ember with { ThermalOptimum=BiomeProfile.For(state.World.Biomes[colony.Nest.X]).Temperature+2, ColdTolerance=25 };
        var egg=AntFactory.Create(state,colony,stage:LifeStage.Egg,genes:genes);
        var biology=new AntBiologySystem();
        while(egg.IsAlive && egg.Stage==LifeStage.Egg && state.Tick<4L*SimulationState.TicksPerDay)
        { state.Tick++; biology.Step(state,egg); }
        Assert.True(egg.IsAlive); Assert.Equal(LifeStage.Larva,egg.Stage);
        Assert.True(egg.Age>=2L*SimulationState.TicksPerDay);
        Assert.InRange(egg.Energy,39.99,40.01); Assert.InRange(egg.Nutrition,.749,.751); Assert.True(egg.Hydration>.6);
        egg.Damage(egg.Health,DamageCause.Combat); Assert.False(egg.IsAlive);
    }
    [Fact]
    public void ResourceDeprivationStillCausesDeath()
    {
        var state=Create(); var ant=state.Ants.First(a=>a.Caste==AntCaste.Worker);
        ant.Energy=ant.Nutrition=ant.Hydration=0;
        var biology=new AntBiologySystem();
        for(var tick=0; tick<SimulationState.TicksPerDay && ant.IsAlive; tick++) {state.Tick++; biology.Step(state,ant);}
        Assert.False(ant.IsAlive);
        Assert.True(ant.FatalDamageCauses.HasFlag(DamageCause.Dehydration));
        Assert.True(ant.FatalDamageCauses.HasFlag(DamageCause.Starvation));
    }
    [Fact]
    public void FullEnergyRestDoesNotWasteNutritionAndTiredWorkersActuallyRest()
    {
        var s=Create();var ant=s.Ants.First(a=>a.Caste==AntCaste.Worker);var c=Context(s,ant);
        ant.Energy=100;var nutrition=ant.Nutrition;
        for(var i=0;i<100;i++) Assert.True(new RestAction().Execute(c));
        Assert.Equal(nutrition,ant.Nutrition);
        ant.Energy=99.9;Assert.True(new RestAction().Execute(c));Assert.Equal(100,ant.Energy);Assert.Equal(nutrition-.1/500,ant.Nutrition,10);
        s.World.ClearFood(true);s.Colonies[0].Food=0;ant.Energy=10;
        new DecisionSystem(new()).Decide(c);Assert.Equal(AntTask.Rest,ant.CurrentTask);
    }
    [Fact]
    public void ReproductionHasBiologicalIntervalAndPreservesFoodReserve()
    {
        var s=Create();var queen=s.Colonies[0].Queen!;var colony=s.Colonies[0];var c=Context(s,queen);var action=new LayEggAction();
        Assert.True(action.Execute(c));var food=colony.Food;
        s.Tick=480;Assert.False(action.Execute(c));Assert.Equal(food,colony.Food);
        s.Tick=(long)(s.Survival.MinimumEggIntervalDays*SimulationState.TicksPerDay);
        Assert.True(action.Execute(c));
        s.Tick*=2;colony.Food=s.Ants.Count*s.Survival.FoodReservePerIndividual+s.Species[0].EggCost-.1;food=colony.Food;
        Assert.False(action.Execute(c));Assert.Equal(food,colony.Food);
    }
    [Fact]
    public void WorkersFindWaterAcrossSeamAndDrinkWithoutCreatingWater()
    {
        var s=Create();var ant=s.Ants.First(a=>a.Caste==AntCaste.Worker);s.World.ClearFood(true);s.Colonies[0].Food=0;
        for(var x=60;x<69;x++)for(var y=19;y<=21;y++) s.World.SetTerrain(new(x,y),y==20?TerrainKind.Tunnel:TerrainKind.Rock);
        var water=new CellPosition(2,20);s.World.SetCell(water,s.World.GetCell(water,false) with {WaterAmount=10});
        ant.Position=new(62,20);ant.Hydration=.2;ant.Energy=100;var c=Context(s,ant);
        Assert.Equal(new CellPosition(63,20),new WaterSearch().NextStep(s.World,ant.Position));
        var decision=new DecisionSystem(new());decision.Decide(c);
        Assert.Equal(AntTask.Move,ant.CurrentTask);Assert.Equal(new CellPosition(63,20),ant.Destination);
        ant.Position=new(1,20);decision.Decide(c);Assert.Equal(AntTask.Drink,ant.CurrentTask);
        Assert.True(new DrinkAction().Execute(c));Assert.Equal(1,ant.Hydration,8);
        Assert.Equal(2,s.World.GetCell(water,false).WaterAmount);
        Assert.False(new DrinkAction().Execute(c));
        s.World.SetTerrain(water,TerrainKind.Rock);s.World.SetCell(water,s.World.GetCell(water,false) with {WaterAmount=10});
        ant.Hydration=.2;Assert.False(new DrinkAction().Execute(c));Assert.Equal(0,s.World.ChunkManager.LoadedCount);
    }
    [Fact]
    public void FoodHydratesAdultsAndNursesRespondToThirstEvenWithFullNutrition()
    {
        var s=Create();var ant=s.Ants.First(a=>a.Caste==AntCaste.Worker);var colony=s.Colonies[0];var c=Context(s,ant);
        ant.Energy=100;ant.Nutrition=1;ant.Hydration=.1;var food=colony.Food;
        Assert.True(new FeedAction().Execute(c));Assert.Equal(.35,ant.Hydration,8);Assert.Equal(food-.5,colony.Food);
        var larva=s.Ants.First(a=>a.Stage==LifeStage.Larva);larva.Nutrition=1;larva.Energy=100;larva.Hydration=.1;ant.TargetId=larva.Id;
        food=colony.Food;Assert.True(new FeedLarvaAction().Execute(c));Assert.Equal(.35,larva.Hydration,8);Assert.Equal(food-.5,colony.Food);
        colony.Food=0;ant.Carrying=0;ant.Energy=0;
        s.World.SetResource(ant.Position,ResourceIds.Food,new(2,2,10000));
        Assert.True(new FeedAction().Execute(c));Assert.Equal(1.5,s.World.GetResource(ant.Position,ResourceIds.Food).Quantity);Assert.Equal(10,ant.Energy);
    }
    [Fact]
    public void SettingsUseDaysAndValidateRates()
    {
        var preset=SimulationPreset.Parse("""{"survival":{"energyPerDay":25,"minimumEggIntervalDays":0.5}}""");
        var state=SimulationFactory.Create(preset.World,preset.Colonies,preset.Survival);
        Assert.Equal(25,state.Survival.EnergyPerDay);Assert.Equal(.5,state.Survival.MinimumEggIntervalDays);
        Assert.Throws<ArgumentException>(()=>SimulationPreset.Parse("""{"survival":{"hydrationPerDay":-1}}"""));
        Assert.Throws<ArgumentException>(()=>SimulationPreset.Parse("""{"survival":null}"""));
    }
}
