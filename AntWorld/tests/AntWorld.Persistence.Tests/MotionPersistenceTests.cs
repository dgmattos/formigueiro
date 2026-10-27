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
        ant.MovementSpeed=.123;ant.MovementCredit=.456;
        var codec=new BinarySnapshotCodec();var bytes=codec.Encode(state);var copy=codec.Decode(bytes);
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
}
