using AntWorld.Domain;
using Xunit;
namespace AntWorld.Simulation.Tests;

public sealed class CollectorDeliveryTests
{
    private static AntContext Create(double nutrition=.5,double queenNutrition=.79)
    {
        var world=new WorldMap(new(42,64,48),surface:Enumerable.Repeat(11,64).ToArray());
        for(var x=0;x<64;x++)world.SetTerrain(new(x,11),TerrainKind.Soil);
        var state=new SimulationState(world,DeterministicRandom.FromSeed(42));
        var colony=new Colony(1,0,new(8,10),Genome.Ember){Layout=new(1),Food=0};
        state.Colonies.Add(colony);state.Pheromones[1]=new(world.CellCount);
        foreach(var(id,x)in new[]{(1,8),(2,20),(3,30)})colony.Layout.AddChamber(new(id,NestChamberType.General,new(x,10),1,1,NestStructureStatus.Completed){Access=new(x,10),Cells=[new(x,10)]});
        var queen=AntFactory.Create(state,colony,AntCaste.Queen);colony.Queen=queen;colony.QueenId=queen.Id;queen.Nutrition=queenNutrition;state.Ants.Add(queen);
        var worker=AntFactory.Create(state,colony);worker.WorkerRole=WorkerRole.Collector;worker.Position=new(3,10);worker.Nutrition=nutrition;worker.Energy=100;worker.Carrying=4;state.Ants.Add(worker);
        var index=new AgentSpatialIndex();index.Rebuild(state);return new(state,worker,colony,index,new());
    }
    [Fact]
    public void SatisfiedCollectorInspectsOnlyFromAnAdjacentCellAndPrioritizesTransport()
    {
        var c=Create();var decision=new DecisionSystem(new());decision.Decide(c);
        Assert.Equal(FoodDeliveryStage.InspectQueen,c.Ant.FoodDelivery);Assert.Equal(AntTask.Carry,c.Ant.CurrentTask);
        Assert.Equal(1,c.State.World.Distance(c.Ant.Destination!.Value,c.Colony.Queen!.Position));
        Assert.False(new DepositAction().CanExecute(c));
        c.Ant.Position=c.Colony.Queen.Position;CollectorFoodDelivery.Prepare(c);Assert.Equal(FoodDeliveryStage.InspectQueen,c.Ant.FoodDelivery);
        c.Ant.Position=new(7,10);decision.Decide(c);Assert.Equal(FoodDeliveryStage.Queen,c.Ant.FoodDelivery);Assert.Equal(AntTask.Deposit,c.Ant.CurrentTask);
        Assert.True(new DepositAction().Execute(c));Assert.Equal(0,c.Ant.Carrying);Assert.Equal(4,LocalProvisions.Amount(c.State.World,c.Colony.Queen.Position));Assert.Equal(0,c.Colony.Food);
        Assert.False(new DepositAction().Execute(c)); // Cannot duplicate a delivery.
        var queenContext=c with{Ant=c.Colony.Queen};var before=c.Colony.Queen.Nutrition;
        Assert.True(new FeedAction().Execute(queenContext));Assert.True(c.Colony.Queen.Nutrition>before);Assert.Equal(3.5,LocalProvisions.Amount(c.State.World,c.Colony.Queen.Position));
    }
    [Fact]
    public void ExactlyEightyPercentRoutesToNurseryAndDoesNotReadQueenRemotely()
    {
        var c=Create(1,.8);CollectorFoodDelivery.Prepare(c);Assert.Equal(FoodDeliveryStage.InspectQueen,c.Ant.FoodDelivery);
        c.Ant.Position=new(7,10);CollectorFoodDelivery.Prepare(c);Assert.Equal(FoodDeliveryStage.Nursery,c.Ant.FoodDelivery);
        c.Ant.Position=new(15,10);c.Colony.Queen!.Nutrition=.1;CollectorFoodDelivery.Prepare(c);Assert.Equal(FoodDeliveryStage.Nursery,c.Ant.FoodDelivery);
        var destination=CollectorFoodDelivery.Destination(c);Assert.NotNull(destination);OrganicNestTests.WalkTo(c,destination!.Value);
        Assert.True(new DepositAction().Execute(c));Assert.Equal(4,LocalProvisions.Amount(c.State.World,c.Colony.BroodChamber));
        var larva=AntFactory.Create(c.State,c.Colony,stage:LifeStage.Larva);larva.Position=c.Colony.BroodChamber;larva.Nutrition=.1;c.State.Ants.Add(larva);c.Index.Rebuild(c.State);c.Ant.TargetId=larva.Id;
        Assert.True(new FeedLarvaAction().Execute(c));Assert.True(larva.Nutrition>.1);Assert.Equal(3.5,LocalProvisions.Amount(c.State.World,larva.Position));
        c.Ant.Position=larva.Position;Assert.False(new CollectAction().CanExecute(c));Assert.Equal(0,FoodSources.AmountAt(c.State.World,larva.Position));
    }
    [Fact]
    public void HungryCollectorFeedsBeforeDeliveringAndNonCollectorsKeepTheirWork()
    {
        var c=Create(.499);new DecisionSystem(new()).Decide(c);Assert.Equal(FoodDeliveryStage.None,c.Ant.FoodDelivery);Assert.Equal(AntTask.Feed,c.Ant.CurrentTask);
        c.Ant.Nutrition=.5;new DecisionSystem(new()).Decide(c);Assert.Equal(AntTask.Carry,c.Ant.CurrentTask);
        c.Ant.WorkerRole=WorkerRole.Digger;CollectorFoodDelivery.Prepare(c);Assert.Equal(FoodDeliveryStage.None,c.Ant.FoodDelivery);
    }
    [Fact]
    public void InspectionWrapsAcrossWorldSeamAndBlockedApproachesDoNotAuthorizeDeposit()
    {
        var c=Create();c.Colony.Queen!.Position=new(0,10);c.Ant.Position=new(63,10);CollectorFoodDelivery.Prepare(c);Assert.Equal(FoodDeliveryStage.Queen,c.Ant.FoodDelivery);
        Assert.True(CollectorFoodDelivery.CanDeposit(c));
        c.Ant.FoodDelivery=FoodDeliveryStage.InspectQueen;c.Ant.Position=new(4,10);
        foreach(var p in new[]{new CellPosition(63,10),new(1,10),new(0,9)})c.State.World.SetTerrain(p,TerrainKind.Soil);
        Assert.Null(CollectorFoodDelivery.Destination(c));Assert.False(new DepositAction().CanExecute(c));
    }
}
