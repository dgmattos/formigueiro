using AntWorld.Domain;
using AntWorld.Simulation;
using AntWorld.Infrastructure;
using Xunit;
namespace AntWorld.Persistence.Tests;
public sealed class AiPersistenceTests
{
    [Fact] public void ConfigurableSpawnPreservesDefaultAndResumesCustomPopulation()
    {
        var codec=new BinarySnapshotCodec();
        var defaults=SimulationPreset.Parse("{}");
        Assert.Equal(codec.Encode(SimulationFactory.Create(new())),codec.Encode(SimulationFactory.Create(defaults.World,defaults.Colonies)));
        var preset=SimulationPreset.Parse("""{"world":{"seed":17,"width":64,"height":48,"chunkSize":8},"colonies":[{"speciesId":1,"nestPositionRatio":0.0,"queens":1,"workers":3,"soldiers":0,"larvae":1,"initialFood":25}]}""");
        var state=SimulationFactory.Create(preset.World,preset.Colonies);
        var restored=codec.Decode(codec.Encode(state));
        new SimulationEngine(state).Run(100);new SimulationEngine(restored).Run(100);
        Assert.Equal(codec.Encode(state),codec.Encode(restored));
    }
    [Fact] public void SocialBehaviorAndConfigurationRoundTripDeterministically()
    {
        var s=SimulationFactory.Create(new(42,96,48));var ant=s.Ants.First(a=>a.Caste==AntCaste.Worker);
        ant.FatherId=987654;ant.LineageId=876543;ant.Learn(SkillType.Digging,7);ant.CurrentTask=AntTask.Rest;
        s.Relations.Set(1,2,ColonyRelation.Hostile);s.Performance=s.Performance with {MaxDetailedAgents=30,MaxRenderedAgents=7};
        s.Pheromones[1].Deposit(PheromoneType.Alarm,0,5);s.Pheromones[1].Deposit(PheromoneType.Territory,1,3);
        var codec=new BinarySnapshotCodec();var original=codec.Encode(s);var restored=codec.Decode(original);Assert.Equal(original,codec.Encode(restored));
        var other=restored.Ants.Single(a=>a.Id==ant.Id);Assert.Equal(ant.Identity,other.Identity);Assert.Equal(ant.Signature,other.Signature);Assert.Equal(ant.Skills,other.Skills);
        var a=new SimulationEngine(s);var b=new SimulationEngine(restored);a.Run(500);b.Run(500);Assert.Equal(codec.Encode(s),codec.Encode(restored));
    }
    [Fact] public void PausePreservesCompleteSnapshotIncludingBiologyAndAi()
    {
        var s=SimulationFactory.Create(new(42,64,48));var engine=new SimulationEngine(s);var clock=new PlaybackControl();var codec=new BinarySnapshotCodec();
        engine.Run(30);var before=codec.Encode(s);clock.TogglePause();clock.Clock.Advance(100,engine.Step);Assert.Equal(before,codec.Encode(s));
    }
}
