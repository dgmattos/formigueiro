using AntWorld.Domain;
using Xunit;
namespace AntWorld.Simulation.Tests;

public sealed class NurseryCalendarTests
{
    private static SimulationState Create() => SimulationFactory.Create(new(42, 64, 48), [new(0, .25) { Workers = 2, Soldiers = 1, Larvae = 0 }]);
    private static AntContext Context(SimulationState state, Ant ant)
    {
        var index = new AgentSpatialIndex(); index.Rebuild(state);
        return new(state, ant, state.Colonies[0], index, new());
    }
    [Fact] public void WorkersAutonomouslyDeliverDisplacedEggToChamberNearestQueen()
    {
        var state = Create(); var colony = state.Colonies[0]; var queen = colony.Queen!;
        Assert.True(colony.BroodChamber.Y > queen.Position.Y); Assert.True(state.World.CanWalk(colony.BroodChamber));
        Assert.True(new LayEggAction().Execute(Context(state, queen)));
        var egg = state.Ants.Last(); Assert.Equal(queen.Position, egg.Position); queen.StoredSperm = 0;
        egg.Position=colony.BroodChamber;
        new NurseryChamberSelection().Step(state,colony);
        var count = state.Ants.Count; var engine = new SimulationEngine(state); var carried = false;
        for (var i = 0; i < 2000 && (egg.Position != colony.BroodChamber || egg.CarrierId.HasValue); i++)
        {
            engine.Step(); carried |= egg.CarrierId.HasValue;
            if (egg.CarrierId is {} id) Assert.Equal(state.Ants.Single(a => a.Id == id).Position, egg.Position);
        }
        Assert.True(carried); Assert.Equal(colony.BroodChamber, egg.Position); Assert.Null(egg.CarrierId);
        Assert.Equal(count, state.Ants.Count); Assert.Equal(LifeStage.Egg, egg.Stage);
    }
    [Fact] public void EggTransportChecksCapacityMembershipDestinationAndExclusiveClaim()
    {
        var state = Create(); var colony = state.Colonies[0];
        var egg = AntFactory.Create(state, colony, stage: LifeStage.Egg); state.Ants.Add(egg);
        var workers = state.Ants.Where(a => a.Caste == AntCaste.Worker && a.Stage == LifeStage.Adult).ToArray();
        var soldier = state.Ants.First(a => a.Caste == AntCaste.Soldier); soldier.TargetId = egg.Id;
        Assert.False(new CarryAction().Execute(Context(state, soldier)));
        var worker = workers[0]; worker.TargetId = egg.Id; worker.Destination = colony.Nest;
        Assert.False(new CarryAction().Execute(Context(state, worker)));
        worker.Destination = colony.BroodChamber;
        state.World.AddWater(colony.BroodChamber, 200); Assert.False(new CarryAction().Execute(Context(state, worker)));
        state.World.SetCell(colony.BroodChamber, state.World.GetCell(colony.BroodChamber, false) with { WaterAmount = 0 });
        var foreignColony = new Colony(999, 0, colony.Nest, colony.QueenGenome);
        var foreignEgg = AntFactory.Create(state, foreignColony, stage: LifeStage.Egg); state.Ants.Add(foreignEgg);
        worker.TargetId = foreignEgg.Id; Assert.False(new CarryAction().Execute(Context(state, worker))); worker.TargetId = egg.Id;
        Assert.True(new CarryAction().Execute(Context(state, worker)));
        workers[1].TargetId = egg.Id; Assert.False(new CarryAction().Execute(Context(state, workers[1])));
        Assert.Equal(worker.Id, egg.CarrierId); Assert.Equal(egg.Id, worker.CarriedAntId);
    }
    [Fact] public void DeadCarrierReleasesEggAndEggNeverWalksByItself()
    {
        var state = Create(); var colony = state.Colonies[0]; var worker = state.Ants.First(a => a.Caste == AntCaste.Worker);
        var egg = AntFactory.Create(state, colony, stage: LifeStage.Egg); state.Ants.Add(egg); worker.TargetId = egg.Id;
        Assert.True(new CarryAction().Execute(Context(state, worker))); worker.Damage(worker.MaxHealth);
        new SimulationEngine(state).Step(); Assert.Null(egg.CarrierId);
        egg.MovementCredit = 2; Assert.False(new ExploreAction().Execute(Context(state, egg)));
    }
    [Fact] public void CalendarHasFourSeasonsOfTwentyEightDaysAndThreeHourDays()
    {
        var state = Create(); Assert.Equal(216000, SimulationState.TicksPerDay);
        Assert.Equal(1, state.DayOfMonth); Assert.Equal(1, state.Month); Assert.Equal(1, state.Year);
        for (var month = 0; month < 4; month++)
        {
            state.Tick = (long)month * 28 * SimulationState.TicksPerDay;
            Assert.Equal(month, state.Season); Assert.Equal(month + 1, state.Month); Assert.Equal(1, state.DayOfMonth);
            state.Tick += 28L * SimulationState.TicksPerDay - 1; Assert.Equal(28, state.DayOfMonth); Assert.Equal(month, state.Season);
        }
        state.Tick++; Assert.Equal(2, state.Year); Assert.Equal(1, state.Month); Assert.Equal(0, state.Season);
        state.Tick = SimulationState.TicksPerDay / 2; Assert.Equal(.5, state.DayFraction); Assert.False(state.IsNight);
    }
}
