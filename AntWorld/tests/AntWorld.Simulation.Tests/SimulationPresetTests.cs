using AntWorld.Domain;
using AntWorld.Simulation;
using Xunit;
namespace AntWorld.Simulation.Tests;

public sealed class SimulationPresetTests
{
    [Fact]
    public void JsonChangesWorldAndSpawnsEachColonyIndependently()
    {
        var preset = SimulationPreset.Parse("""
        {
          "world": { "seed": 99, "width": 96, "height": 64, "chunkSize": 8, "maximumLoadedChunks": 2 },
          "colonies": [
            { "speciesId": 1, "nestPositionRatio": 0.0, "queens": 1, "workers": 2, "soldiers": 1, "larvae": 3, "initialFood": 12 },
            { "speciesId": 0, "nestPositionRatio": 0.5, "queens": 1, "workers": 0, "soldiers": 0, "larvae": 0, "initialFood": 0 },
            { "speciesId": 1, "nestPositionRatio": 0.75, "queens": 1, "workers": 4, "soldiers": 0, "larvae": 0, "initialFood": 30 }
          ]
        }
        """);
        var state = SimulationFactory.Create(preset.World, preset.Colonies);
        Assert.Equal(99, state.World.Configuration.Seed);
        Assert.Equal(96, state.World.Width);
        Assert.Equal(64, state.World.Height);
        Assert.Equal(8, state.World.Configuration.ChunkSize);
        Assert.Equal(2, state.World.Configuration.MaximumLoadedChunks);
        Assert.Equal(3, state.Colonies.Count);
        Assert.Equal(13, state.Ants.Count);
        for (var i = 0; i < preset.Colonies.Count; i++)
        {
            var expected = preset.Colonies[i]; var colony = state.Colonies[i];
            var ants = state.Ants.Where(a => a.ColonyId == colony.Id).ToArray();
            Assert.Equal(expected.SpeciesId, colony.Species);
            Assert.Equal((int)(state.World.Width * expected.NestPositionRatio), colony.Nest.X);
            Assert.Equal(expected.InitialFood, colony.Food);
            Assert.Equal(expected.Queens, ants.Count(a => a.Caste == AntCaste.Queen));
            Assert.Equal(expected.Workers, ants.Count(a => a.Stage == LifeStage.Adult && a.Caste == AntCaste.Worker));
            Assert.Equal(expected.Soldiers, ants.Count(a => a.Stage == LifeStage.Adult && a.Caste == AntCaste.Soldier));
            Assert.Equal(expected.Larvae, ants.Count(a => a.Stage == LifeStage.Larva));
            Assert.All(ants, a => Assert.True(state.World.CanWalk(a.Position)));
        }
    }

    [Theory]
    [InlineData("{\"world\":{\"width\":1}}")]
    [InlineData("{\"colonies\":[]}")]
    [InlineData("{\"colonies\":null}")]
    [InlineData("{\"colonies\":[null]}")]
    [InlineData("{\"colonies\":[{\"speciesId\":4,\"nestPositionRatio\":0.25}]}")]
    [InlineData("{\"colonies\":[{\"speciesId\":0,\"nestPositionRatio\":1}]}")]
    [InlineData("{\"colonies\":[{\"speciesId\":0,\"nestPositionRatio\":0.25,\"workers\":-1}]}")]
    [InlineData("{\"colonies\":[{\"speciesId\":0,\"nestPositionRatio\":0.25,\"queens\":2}]}")]
    [InlineData("{\"colonies\":[{\"speciesId\":0,\"nestPositionRatio\":0.25,\"workers\":2147483647}]}")]
    [InlineData("{\"colonies\":[{\"speciesId\":0,\"nestPositionRatio\":0.25},{\"speciesId\":1,\"nestPositionRatio\":0.2501}]}")]
    [InlineData("{\"world\":{\"widht\":100}}")]
    public void InvalidPresetsAreRejectedBeforeGeneratingWorld(string json) =>
        Assert.ThrowsAny<ArgumentException>(() => SimulationPreset.Parse(json));
}
