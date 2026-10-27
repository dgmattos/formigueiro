using AntWorld.Domain;
using AntWorld.Simulation;
using AntWorld.Infrastructure;
using Xunit;
namespace AntWorld.Persistence.Tests;
public sealed class VegetationPersistenceTests
{
    [Fact] public void VegetationDeltasSpeciesAndDeepNestsSurviveResume()
    {
        var state = SimulationFactory.Create(new(12345, 512, 128, 32), [new(0, .25), new(2, .75)]);
        var tree = state.World.Vegetation.All.First(v => v.Definition.IsTree); state.World.Vegetation.Damage(tree.Id, 100);
        var other = state.World.Vegetation.All.First(v => v.Id != tree.Id); state.World.Vegetation.Set(other with { Removed = true });
        var codec = new BinarySnapshotCodec(); var bytes = codec.Encode(state); var restored = codec.Decode(bytes);
        Assert.Equal(bytes, codec.Encode(restored)); Assert.Equal(state.World.Vegetation.All, restored.World.Vegetation.All);
        Assert.True(restored.World.Vegetation.Get(other.Id).Removed); Assert.Equal(VegetationKind.TreeStump, restored.World.Vegetation.Get(tree.Id).Kind);
        Assert.Equal(.8, restored.Species[2].SolidRockSpeedFactor); Assert.Equal(state.World.Configuration.Nests, restored.World.Configuration.Nests);
        new SimulationEngine(state).Run(80); new SimulationEngine(restored).Run(80);
        Assert.Equal(codec.Encode(state), codec.Encode(restored)); Assert.Equal(0, restored.World.ChunkManager.LoadedCount);
    }
}
