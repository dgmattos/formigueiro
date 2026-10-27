using AntWorld.Domain;
using AntWorld.Infrastructure;
using AntWorld.Simulation;
using Xunit;
namespace AntWorld.Persistence.Tests;

public sealed class ForagingPersistenceTests
{
    [Fact]
    public void SharedExplorationMemoryContactAndShaftResumeExactly()
    {
        var s=SimulationFactory.Create(new(42,64,48),[new(0,.25){Workers=5,Soldiers=0,Larvae=0}]);
        foreach(var a in s.Ants.Where(a=>a.Caste==AntCaste.Worker))a.WorkerRole=WorkerRole.Collector;
        new SimulationEngine(s).Run(500);
        Assert.True(s.Colonies[0].Foraging.Visits.Count>5);
        var colony=s.Colonies[0];var index=s.World.Index(colony.Nest);
        colony.Foraging.Observe(index,s.Tick,true,true);
        var codec=new BinarySnapshotCodec();var bytes=codec.Encode(s);var restored=codec.Decode(bytes);
        Assert.Equal(9,BitConverter.ToInt32(bytes,8));Assert.Equal(bytes,codec.Encode(restored));
        Assert.Equal(colony.ShaftX,restored.Colonies[0].ShaftX);
        Assert.Equal(colony.Foraging.Visits.OrderBy(p=>p.Key),restored.Colonies[0].Foraging.Visits.OrderBy(p=>p.Key));
        Assert.Contains(index,restored.Colonies[0].Foraging.Food);
        Assert.Equal(s.Ants.Select(a=>a.Support),restored.Ants.Select(a=>a.Support));
        new SimulationEngine(s).Run(1000);new SimulationEngine(restored).Run(1000);
        Assert.Equal(codec.Encode(s),codec.Encode(restored));
    }
    [Fact]
    public void ActualVersionEightSaveKeepsShaftAndIndividualsAndAdaptsUnsupportedChamber()
    {
        var bytes=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","format8-tick20.antworld"));
        Assert.Equal(8,BitConverter.ToInt32(bytes,8));var codec=new BinarySnapshotCodec();var s=codec.Decode(bytes);
        Assert.Equal(84,s.Ants.Count);Assert.Equal(20,s.Tick);
        Assert.Equal(new[]{60,180,120},s.Colonies.Select(c=>c.ShaftX));
        Assert.All(s.Colonies,c=>{Assert.True(s.World.CanWalk(c.Nest));Assert.Empty(c.Foraging.Visits);});
        new SimulationEngine(s).Run(300);SnapshotValidator.Validate(s);
    }
    [Fact]
    public void ExplorationMemoryIsBoundedAndInvalidIndicesAreRejected()
    {
        var memory=new ForagingMemory();
        for(var i=0;i<ForagingMemory.MaximumCells+10;i++)memory.Observe(i,0,false,false);
        Assert.Equal(ForagingMemory.MaximumCells,memory.Visits.Count);
        var s=SimulationFactory.Create(new(42,64,48));s.Colonies[0].Foraging.Observe(s.World.CellCount,0,false,false);
        Assert.Throws<InvalidDataException>(()=>new BinarySnapshotCodec().Encode(s));
    }
}
