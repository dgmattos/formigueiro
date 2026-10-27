using AntWorld.Domain;
using Xunit;
namespace AntWorld.Simulation.Tests;
public sealed class StrongSpeciesTests
{
    private static AntContext Context(SimulationState state, Ant ant)
    { var index = new AgentSpatialIndex(); index.Rebuild(state); return new(state, ant, state.Colonies.First(c => c.Id == ant.ColonyId), index, new()); }
    [Fact] public void SpeciesModifiersMatchSoilMixedAndSolidRockRequirements()
    {
        var soil = new WorldCell { SolidFill = 255 }; var mixed = soil with { RockFraction = 128 }; var rock = soil with { RockFraction = 255 };
        var normal = new SpeciesDefinition(); var strong = SpeciesDefinition.Strong;
        Assert.Equal(1.25, ExcavationRules.SpeedFactor(strong, soil) / ExcavationRules.SpeedFactor(normal, soil));
        Assert.Equal(1.4, ExcavationRules.SpeedFactor(strong, mixed) / ExcavationRules.SpeedFactor(normal, mixed));
        Assert.Equal(.4, ExcavationRules.SpeedFactor(normal, rock) / ExcavationRules.SpeedFactor(normal, soil));
        Assert.Equal(.8, ExcavationRules.SpeedFactor(strong, rock) / ExcavationRules.SpeedFactor(strong, soil));
        Assert.Equal(1.6, ExcavationRules.EnergyCost(normal, rock) / ExcavationRules.EnergyCost(normal, soil), 8);
        Assert.Equal(1.2, ExcavationRules.EnergyCost(strong, rock) / ExcavationRules.EnergyCost(strong, soil), 8);
    }
    [Fact] public void BothSpeciesActuallyExcavateRockAndPayItsCostBeforeRemoval()
    {
        var state = SimulationFactory.Create(new(42, 96, 96), [new(0, .25), new(2, .75)]);
        foreach (var colony in state.Colonies)
        {
            var worker = state.Ants.First(a => a.ColonyId == colony.Id && a.Caste == AntCaste.Worker);
            var target = new CellPosition(worker.Position.X + 1, worker.Position.Y);
            state.World.SetTerrain(target, TerrainKind.Rock); worker.Destination = target;
            state.World.SetCell(target, state.World.GetCell(target, false) with { SolidFill = 1 });
            var expectedCost = ExcavationRules.EnergyCost(state.Species[worker.SpeciesId], state.World.GetCell(target, false));
            var energy = worker.Energy;
            Assert.True(new DigAction().Execute(Context(state, worker)));
            Assert.Equal(energy - expectedCost, worker.Energy, 8);
            Assert.Equal(0, state.World.GetCell(target, false).SolidFill);
        }
    }
    [Fact] public void NestDepthUsesFiveBlocksForNormalAndSeventyFivePercentForStrong()
    {
        var state = SimulationFactory.Create(new(42, 96, 96), [new(0, .25), new(2, .75)]);
        var normal = state.Colonies[0]; var strong = state.Colonies[1];
        Assert.Equal(state.World.Surface[normal.Nest.X] + 5, normal.Nest.Y);
        Assert.Equal(state.World.Surface[strong.Nest.X] + (int)Math.Round((state.World.Height - 1 - state.World.Surface[strong.Nest.X]) * .75), strong.Nest.Y);
        Assert.True(state.World.CanWalk(strong.BroodChamber)); Assert.True(strong.BroodChamber.Y < state.World.Height - 1);
        for (var y = state.World.Surface[strong.Nest.X] - 1; y <= strong.BroodChamber.Y; y++) Assert.True(state.World.CanWalk(new(strong.Nest.X, y)));
        Assert.All(state.Ants.Where(a => a.ColonyId == strong.Id), a => Assert.Equal(2, a.SpeciesId));
    }
}
