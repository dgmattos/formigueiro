using AntWorld.Domain;
using AntWorld.Simulation;
using AntWorld.Infrastructure;
using Xunit;
namespace AntWorld.Persistence.Tests;

public sealed class SurfaceSpeciesPersistenceTests
{
    [Fact]
    public void NewSpeciesAndConfigurableSurfaceRulesResumeExactly()
    {
        var state = SimulationFactory.Create(new(42, 96, 64) { Nests = new(SurfaceDepth: 1) }, [new(0, .25), new(2, .75), new(3, .5)]);
        state.Species[3] = new() { SurfaceMovementMultiplier = 1.35, Castes = SpeciesDefinition.Surface.Castes };
        var codec = new BinarySnapshotCodec(); var bytes = codec.Encode(state); var restored = codec.Decode(bytes);
        Assert.Equal(BinarySnapshotCodec.Version, BitConverter.ToInt32(bytes, 8));
        Assert.Equal(1, restored.World.Configuration.Nests.SurfaceDepth);
        Assert.Equal(1.35, restored.Species[3].SurfaceMovementMultiplier);
        Assert.Equal(36, restored.Species[3].For(AntCaste.Soldier).Attack);
        Assert.Equal(bytes, codec.Encode(restored));
        new SimulationEngine(state).Run(100); new SimulationEngine(restored).Run(100);
        Assert.Equal(codec.Encode(state), codec.Encode(restored));
    }
    [Fact]
    public void RealFormatFiveFixturesKeepSavedStateAndResumeUnderCurrentRules()
    {
        // Produced by the previous v5 executable, not by a test implementation of the codec.
        var codec = new BinarySnapshotCodec();
        var initial = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "format5-tick20.antworld"));
        Assert.Equal(5, BitConverter.ToInt32(initial, 8));
        var state = codec.Decode(initial);
        Assert.Equal(20, state.Tick);
        Assert.All(state.Species.Values, species => Assert.Equal(1, species.SurfaceMovementMultiplier));
        Assert.False(state.Species.ContainsKey(3)); // Loading does not inject a colony or change species in a prior save.
        var copy=codec.Decode(codec.Encode(state));
        new SimulationEngine(state).Run(60); new SimulationEngine(copy).Run(60);
        Assert.Equal(codec.Encode(copy),codec.Encode(state));
        var oldLater = codec.Decode(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "format5-tick80.antworld")));
        Assert.Equal(80,oldLater.Tick); // Historical values load; changed biology intentionally changes future outcomes.

    }
}
