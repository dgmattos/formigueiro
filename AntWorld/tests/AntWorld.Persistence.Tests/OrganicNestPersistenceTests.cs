using AntWorld.Domain;
using AntWorld.Infrastructure;
using AntWorld.Simulation;
using AntWorld.WorldGeneration;
using Xunit;
namespace AntWorld.Persistence.Tests;

public sealed class OrganicNestPersistenceTests
{
    [Fact]
    public void QueensSelectedDestinationPersistsAndMovementResumesDeterministically()
    {
        var state=SimulationFactory.Create(new(42,128,64),[new(0,.5)]);var colony=state.Colonies[0];
        var origin=colony.Queen!.Position;new QueenChamberSelection().Step(state,colony);
        Assert.Equal(origin,colony.Queen.Position);
        var selected=colony.Layout!.QueenChamberId;var codec=new BinarySnapshotCodec();var bytes=codec.Encode(state);var copy=codec.Decode(bytes);
        Assert.Equal(selected,copy.Colonies[0].Layout!.QueenChamberId);Assert.Equal(colony.Nest,copy.Colonies[0].Nest);
        Assert.Equal(bytes,codec.Encode(copy));
        new SimulationEngine(state).Run(100);new SimulationEngine(copy).Run(100);
        Assert.Equal(codec.Encode(state),codec.Encode(copy));
    }
    [Fact]
    public void PartialWorkAndLeasesResumeWithIdenticalStateAndRandomSequence()
    {
        var s=SimulationFactory.Create(new(42,128,64),[new(0,.5){Workers=16,Larvae=0,Soldiers=0,InitialFood=1000}]);
        var colony=s.Colonies[0];var layout=colony.Layout!;
        Assert.True(new OrganicNestPlanner().TryExpand(s.World,layout,0));OrganicNestConstruction.Enqueue(s.World,layout);
        foreach(var a in s.Ants.Where(a=>a.Caste==AntCaste.Worker))a.WorkerRole=WorkerRole.Digger;
        colony.Queen!.StoredSperm=0;var engine=new SimulationEngine(s);
        for(var i=0;i<3000&&!layout.Construction.Jobs.Any(j=>j.WorkActions>0&&s.World.CanDig(j.Target));i++)engine.Step();
        Assert.Contains(layout.Construction.Jobs,j=>j.WorkActions>0&&s.World.CanDig(j.Target));
        var codec=new BinarySnapshotCodec();var bytes=codec.Encode(s);var restored=codec.Decode(bytes);
        Assert.Equal(bytes,codec.Encode(restored));Assert.NotNull(restored.Colonies[0].Layout);
        engine.Run(500);new SimulationEngine(restored).Run(500);
        Assert.Equal(codec.Encode(s),codec.Encode(restored));
    }
    [Fact]
    public void FormatNineMigrationPreservesTerrainAgentsAndSupportedLegacyRooms()
    {
        var bytes=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","format9-tick20.antworld"));
        Assert.Equal(9,BitConverter.ToInt32(bytes,8));var codec=new BinarySnapshotCodec();var state=codec.Decode(bytes);
        Assert.Equal(20,state.Tick);Assert.Equal(84,state.Ants.Count);
        Assert.Contains(state.Colonies,c=>c.Layout is not null);
        var current=codec.Encode(state);Assert.Equal(BinarySnapshotCodec.Version,BitConverter.ToInt32(current,8));
        Assert.Equal(current,codec.Encode(codec.Decode(current)));
    }
    [Fact]
    public void ExplicitResourceDeltaEqualToBaselineSurvivesReplay()
    {
        var state=SimulationFactory.Create(new(42,128,64),[new(0,.5)]);
        var resource=state.World.Resources().First(e=>e.ResourceId==ResourceIds.Food&&e.Stack.Quantity>0);
        var original=state.World.GetResource(resource.Position,resource.ResourceId);
        state.World.SetResource(resource.Position,resource.ResourceId,default);
        state.World.SetResource(resource.Position,resource.ResourceId,original);
        var codec=new BinarySnapshotCodec();var bytes=codec.Encode(state);
        Assert.Equal(bytes,codec.Encode(codec.Decode(bytes)));
    }
    [Fact]
    public void DuplicateJobsAndUnbuiltCompletedRoomsCannotBeSaved()
    {
        var state=SimulationFactory.Create(new(42,128,64),[new(0,.5)]);var layout=state.Colonies[0].Layout!;
        var target=new CellPosition(20,40);layout.Construction.Jobs.Add(new(1,target));layout.Construction.Jobs.Add(new(2,target));
        Assert.Throws<InvalidDataException>(()=>new BinarySnapshotCodec().Encode(state));layout.Construction.Jobs.Clear();
        state.World.SetTerrain(layout.Chambers[0].Cells[0],TerrainKind.Rock);
        Assert.Throws<InvalidDataException>(()=>new BinarySnapshotCodec().Encode(state));
    }
}
