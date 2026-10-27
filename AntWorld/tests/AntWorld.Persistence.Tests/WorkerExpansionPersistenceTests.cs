using AntWorld.Domain;
using AntWorld.Infrastructure;
using AntWorld.Simulation;
using Xunit;
namespace AntWorld.Persistence.Tests;

public sealed class WorkerExpansionPersistenceTests
{
    [Fact]
    public void RolesFloorsCarriedSpoilAndSurfacePilesResumeExactly()
    {
        var state=SimulationFactory.Create(new(42,64,48),[new(0,.25){Workers=8,Soldiers=0,Larvae=0}]);
        var colony=state.Colonies[0];var worker=state.Ants.First(a=>a.Caste==AntCaste.Worker);
        var next=new CellPosition(colony.Nest.X,colony.BroodChamber.Y+4);
        for(var y=colony.BroodChamber.Y;y<=next.Y;y++)state.World.SetTerrain(new(next.X,y),TerrainKind.Tunnel);
        for(var dx=-4;dx<=4;dx++)state.World.SetTerrain(new(next.X+dx,next.Y),TerrainKind.Tunnel);
        colony.AdvanceFloor(); // Already completed physical rooms; queen must still walk to the new chamber.
        worker.WorkerRole=WorkerRole.Digger;worker.CargoResourceId=ExcavationSpoil.ResourceId(state.World);worker.Carrying=1;
        worker.CurrentTask=AntTask.Carry;worker.Destination=ExcavationSpoil.Exit(state.World,colony);
        ExcavationSpoil.Drop(state.World,worker.Destination.Value,3);
        var codec=new BinarySnapshotCodec();var bytes=codec.Encode(state);var restored=codec.Decode(bytes);
        Assert.Equal(BinarySnapshotCodec.Version,BitConverter.ToInt32(bytes,8));Assert.Equal(bytes,codec.Encode(restored));
        Assert.Equal(1,restored.Colonies[0].ExpansionLevel);Assert.Equal(colony.Nest,restored.Colonies[0].Nest);
        Assert.Equal(WorkerRole.Digger,restored.Ants.Single(a=>a.Id==worker.Id).WorkerRole);
        Assert.Equal(3,restored.World.GetResource(worker.Destination.Value,worker.CargoResourceId).Quantity);
        new SimulationEngine(state).Run(500);new SimulationEngine(restored).Run(500);
        Assert.Equal(codec.Encode(state),codec.Encode(restored));
    }
    [Fact]
    public void ActualVersionSevenSaveMigratesRolesWithoutConsumingRandomState()
    {
        var bytes=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","format7-tick20.antworld"));
        var codec=new BinarySnapshotCodec();var a=codec.Decode(bytes);var b=codec.Decode(bytes);
        Assert.Equal(7,BitConverter.ToInt32(bytes,8));Assert.Equal(20,a.Tick);
        Assert.Equal(84,a.Ants.Count);Assert.All(a.Colonies,c=>Assert.Equal(0,c.ExpansionLevel));
        Assert.All(a.Ants.Where(x=>x.Caste==AntCaste.Worker && x.Stage==LifeStage.Adult),x=>Assert.NotEqual(WorkerRole.None,x.WorkerRole));
        Assert.Contains(a.Ants,x=>x.WorkerRole==WorkerRole.Collector);Assert.Contains(a.Ants,x=>x.WorkerRole==WorkerRole.Digger);
        Assert.Equal(a.Random.State,b.Random.State);Assert.Equal(codec.Encode(a),codec.Encode(b));
        new SimulationEngine(a).Run(100);new SimulationEngine(b).Run(100);Assert.Equal(codec.Encode(a),codec.Encode(b));
    }
}
