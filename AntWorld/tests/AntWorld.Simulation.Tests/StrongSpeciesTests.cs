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
    [Fact] public void AllSpeciesExcavateAtEightyPercentOfThePreviousRate()
    {
        Assert.Equal(.16,ExcavationRules.GlobalSpeedMultiplier);
        Assert.Equal(62.5,ExcavationRules.ActionIntervalTicks);
        foreach(var species in new[]{0,1,2,3})
        {
            var state=SimulationFactory.Create(new(42,64,48),[new(species,.25){Workers=1,Soldiers=0,Larvae=0}]);
            var worker=state.Ants.First(a=>a.Caste==AntCaste.Worker);worker.CurrentTask=AntTask.Dig;
            var target=new CellPosition(worker.Position.X+1,worker.Position.Y);worker.Destination=target;
            var behavior=new AntBehaviorSystem(new());var context=Context(state,worker);var completed=0;
            for(var tick=1;tick<=1000;tick++)
            {
                state.Tick=tick;worker.Energy=100;state.World.SetTerrain(target,TerrainKind.Rock);
                if(behavior.Step(context))completed++;
            }
            Assert.Equal(16,completed); // 80% of the previous 20 actions; originally 100 in this interval.
        }
    }
    [Fact] public void NestDepthUsesFiveBlocksForNormalAndSeventyFivePercentForStrong()
    {
        var state = SimulationFactory.Create(new(42, 96, 96), [new(0, .25), new(2, .75)]);
        var normal = state.Colonies[0]; var strong = state.Colonies[1];
        Assert.Equal(state.World.Surface[normal.ShaftX] + 5, normal.Nest.Y);
        Assert.Equal(state.World.Surface[strong.ShaftX] + (int)Math.Round((state.World.Height - 1 - state.World.Surface[strong.ShaftX]) * .75), strong.Nest.Y);
        Assert.True(state.World.CanWalk(strong.BroodChamber)); Assert.True(strong.BroodChamber.Y < state.World.Height - 1);
        Assert.True(new NestNavigation().Distances(state.World,strong).ContainsKey(state.World.Index(ExcavationSpoil.Exit(state.World,strong))));
        Assert.All(state.Ants.Where(a => a.ColonyId == strong.Id), a => Assert.Equal(2, a.SpeciesId));
    }
}
