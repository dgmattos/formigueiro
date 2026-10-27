using AntWorld.Domain;
using AntWorld.Infrastructure;
using AntWorld.Simulation;
using Xunit;
namespace AntWorld.Persistence.Tests;

public sealed class MotionPersistenceTests
{
    [Fact]
    public void SpeedAndFractionalMovementResumeExactly()
    {
        var state=SimulationFactory.Create(new(42,128,64));var ant=state.Ants.First(a=>a.Caste==AntCaste.Worker);
        ant.MovementSpeed=.123;ant.MovementCredit=.456;ant.QueenEscort=true;ant.EscortReturning=true;ant.PatrolCursor=42;
        var codec=new BinarySnapshotCodec();var bytes=codec.Encode(state);var copy=codec.Decode(bytes);
        Assert.True(copy.Ants.Single(a=>a.Id==ant.Id).QueenEscort);Assert.True(copy.Ants.Single(a=>a.Id==ant.Id).EscortReturning);Assert.Equal(42,copy.Ants.Single(a=>a.Id==ant.Id).PatrolCursor);
        Assert.Equal(.123,copy.Ants.Single(a=>a.Id==ant.Id).MovementSpeed);Assert.Equal(bytes,codec.Encode(copy));
        new SimulationEngine(state).Run(200);new SimulationEngine(copy).Run(200);Assert.Equal(codec.Encode(state),codec.Encode(copy));
        ant.MovementSpeed=double.NaN;Assert.Throws<InvalidDataException>(()=>codec.Encode(state));
    }
    [Fact]
    public void VersionTenLoadsAtRestWithoutMovingIndividuals()
    {
        var bytes=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","format10-motion.antworld"));
        Assert.Equal(10,BitConverter.ToInt32(bytes,8));var codec=new BinarySnapshotCodec();var state=codec.Decode(bytes);
        Assert.All(state.Ants,a=>Assert.Equal(0,a.MovementSpeed));
        var current=codec.Encode(state);Assert.Equal(current,codec.Encode(codec.Decode(current)));
    }
    [Fact]
    public void VersionElevenLoadsWithoutInventingEscorts()
    {
        var bytes=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","format11-escort.antworld"));
        Assert.Equal(11,BitConverter.ToInt32(bytes,8));var codec=new BinarySnapshotCodec();var state=codec.Decode(bytes);
        Assert.All(state.Ants,a=>{Assert.False(a.QueenEscort);Assert.Equal(0,a.PatrolCursor);});
        Assert.Equal(codec.Encode(state),codec.Encode(codec.Decode(codec.Encode(state))));
    }
    [Fact]
    public void DeliveryMemoryAndLocalFoodResumeAndVersionTwelveDefaultsToInspectionOnNextTrip()
    {
        var bytes=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","format12-delivery.antworld"));
        Assert.Equal(12,BitConverter.ToInt32(bytes,8));var codec=new BinarySnapshotCodec();var state=codec.Decode(bytes);
        Assert.All(state.Ants,a=>Assert.Equal(FoodDeliveryStage.None,a.FoodDelivery));
        var ant=state.Ants.First(a=>a.Caste==AntCaste.Worker);ant.WorkerRole=WorkerRole.Collector;ant.Carrying=4;ant.FoodDelivery=FoodDeliveryStage.Nursery;
        LocalProvisions.Deposit(state.World,state.Colonies[0].BroodChamber,7);
        var encoded=codec.Encode(state);var copy=codec.Decode(encoded);Assert.Equal(encoded,codec.Encode(copy));
        Assert.Equal(FoodDeliveryStage.Nursery,copy.Ants.Single(a=>a.Id==ant.Id).FoodDelivery);
        Assert.Equal(7,LocalProvisions.Amount(copy.World,copy.Colonies[0].BroodChamber));
        new SimulationEngine(state).Run(50);new SimulationEngine(copy).Run(50);Assert.Equal(codec.Encode(state),codec.Encode(copy));
    }
}
