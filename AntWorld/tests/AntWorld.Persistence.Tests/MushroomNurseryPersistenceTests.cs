using System.Buffers.Binary;
using AntWorld.Domain;
using AntWorld.Infrastructure;
using AntWorld.Simulation;
using Xunit;
namespace AntWorld.Persistence.Tests;

public sealed class MushroomNurseryPersistenceTests
{
    [Fact] public void HarvestedMushroomsAndCarriedEggResumeExactlyAcrossRenewals()
    {
        var configuration = new WorldConfiguration(42, 64, 48)
        {
            Mushrooms = new() { RenewalIntervalSeconds = 1, FoodPerMushroom = 7, MaximumPerChunk = 8 }
        };
        var state = SimulationFactory.Create(configuration); state.Tick = 19;
        var mushrooms = state.World.Resources().Where(e => ResourceIds.IsMushroom(e.ResourceId) && e.Stack.Quantity > 0).ToArray();
        Assert.True(mushrooms.Length > 2);
        state.World.Extract(mushrooms[0].Position, mushrooms[0].ResourceId, 7);
        state.World.Extract(mushrooms[1].Position, mushrooms[1].ResourceId, 2);
        var colony = state.Colonies[0]; var egg = AntFactory.Create(state, colony, stage: LifeStage.Egg, motherId: colony.QueenId);
        state.Ants.Add(egg); var worker = state.Ants.First(a => a.ColonyId == colony.Id && a.Caste == AntCaste.Worker);
        worker.TargetId = egg.Id; worker.Destination = colony.BroodChamber;
        var index = new AgentSpatialIndex(); index.Rebuild(state);
        Assert.True(new CarryAction().Execute(new(state, worker, colony, index, new())));
        var codec = new BinarySnapshotCodec(); var original = codec.Encode(state); var restored = codec.Decode(original);
        Assert.Equal(original, codec.Encode(restored)); Assert.Equal(configuration.Mushrooms, restored.World.Configuration.Mushrooms);
        Assert.Equal(0, restored.World.ChunkManager.LoadedCount);
        Assert.Equal(worker.Id, restored.Ants.Single(a => a.Id == egg.Id).CarrierId);
        Assert.Equal(0, restored.World.GetResource(mushrooms[0].Position, mushrooms[0].ResourceId).Quantity);
        Assert.Equal(5, restored.World.GetResource(mushrooms[1].Position, mushrooms[1].ResourceId).Quantity);
        new SimulationEngine(state).Run(180); new SimulationEngine(restored).Run(180);
        Assert.Equal(codec.Encode(state), codec.Encode(restored));
        Assert.All(restored.World.Chunks(), region => Assert.InRange(restored.World.CountMushrooms(region), 0, 8));
    }
    [Fact] public void FormatThreeIsExplicitlyRejectedAfterCalendarAndGeneratorChanges()
    {
        var codec = new BinarySnapshotCodec(); var bytes = codec.Encode(SimulationFactory.Create(new(42, 64, 48)));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8, 4), 3);
        var error = Assert.Throws<InvalidDataException>(() => codec.Decode(bytes));
        Assert.Contains("formato 8", error.Message);
    }
}
