using AntWorld.Domain;
using AntWorld.Infrastructure;
using AntWorld.Simulation;
using Xunit;
namespace AntWorld.Persistence.Tests;
public sealed class SurvivalPersistenceTests
{
    [Fact]
    public void RatesAndNewRulesResumeWithoutLosingState()
    {
        var state=SimulationFactory.Create(new(42,64,48),survival:new(EnergyPerDay:22,MinimumEggIntervalDays:.5,FoodHydration:.4));
        var codec=new BinarySnapshotCodec();var bytes=codec.Encode(state);var restored=codec.Decode(bytes);
        Assert.Equal(7,BitConverter.ToInt32(bytes,8));Assert.Equal(state.Survival,restored.Survival);
        new SimulationEngine(state).Run(100);new SimulationEngine(restored).Run(100);Assert.Equal(codec.Encode(state),codec.Encode(restored));
    }
    [Fact]
    public void VersionSixKeepsIndividualsAndGetsExplicitCurrentSurvivalDefaults()
    {
        var bytes=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","format6-tick20.antworld"));
        Assert.Equal(6,BitConverter.ToInt32(bytes,8));var state=new BinarySnapshotCodec().Decode(bytes);
        Assert.Equal(20,state.Tick);Assert.Equal(3,state.Colonies.Count);Assert.True(state.Ants.Count>=81);
        Assert.Equal(new SurvivalSettings(),state.Survival);Assert.Equal(1.2,state.Species[3].SurfaceMovementMultiplier);
    }
}
