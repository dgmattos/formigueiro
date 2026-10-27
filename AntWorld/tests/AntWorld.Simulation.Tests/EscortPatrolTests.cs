using AntWorld.Domain;
using Xunit;
namespace AntWorld.Simulation.Tests;

public sealed class EscortPatrolTests
{
    private static SimulationState Create()
    {
        var world=new WorldMap(new(42,64,48),surface:Enumerable.Repeat(11,64).ToArray());
        for(var x=0;x<64;x++)world.SetTerrain(new(x,11),TerrainKind.Soil);
        var state=new SimulationState(world,DeterministicRandom.FromSeed(42));
        var colony=new Colony(1,0,new(8,10),Genome.Ember){Layout=new(1)};
        state.Colonies.Add(colony);state.Pheromones[1]=new(world.CellCount);
        var queen=Add(state,AntCaste.Queen);colony.Queen=queen;colony.QueenId=queen.Id;
        foreach(var(id,x)in new[]{(1,8),(2,20),(3,40),(4,56)})
            colony.Layout.AddChamber(new(id,NestChamberType.General,new(x,10),1,1,NestStructureStatus.Completed){Access=new(x,10),Cells=[new(x,10)]});
        return state;
    }
    private static Ant Add(SimulationState state,AntCaste caste,LifeStage stage=LifeStage.Adult)
    {var ant=AntFactory.Create(state,state.Colonies[0],caste,stage);state.Ants.Add(ant);return ant;}
    private static AntContext Context(SimulationState state,Ant ant)
    {var index=new AgentSpatialIndex();index.Rebuild(state);return new(state,ant,state.Colonies[0],index,new());}

    [Fact]
    public void AssignsOnlyTwoSoldiersAndOneWorkerAndReplacesDeadMembers()
    {
        var state=Create();for(var i=0;i<5;i++){Add(state,AntCaste.Worker);Add(state,AntCaste.Soldier);}
        var larva=Add(state,AntCaste.Worker,LifeStage.Larva);var system=new QueenEscortSystem();system.Step(state);
        Assert.Equal(3,state.Ants.Count(a=>a.QueenEscort));Assert.False(larva.QueenEscort);
        Assert.Equal(2,state.Ants.Count(a=>a.QueenEscort&&a.Caste==AntCaste.Soldier));
        var survivors=state.Ants.Where(a=>a.QueenEscort).ToArray();system.Step(state);
        Assert.All(survivors,a=>Assert.True(a.QueenEscort));
        foreach(var caste in new[]{AntCaste.Soldier,AntCaste.Worker})
        {
            var lost=state.Ants.First(a=>a.QueenEscort&&a.Caste==caste);lost.Health=0;system.Step(state);
            Assert.False(lost.QueenEscort);Assert.Equal(3,state.Ants.Count(a=>a.QueenEscort));
        }
        state.Colonies[0].Queen!.Health=0;system.Step(state);Assert.DoesNotContain(state.Ants,a=>a.QueenEscort);
    }
    [Fact]
    public void EngineReplacesADeadEscortBeforeTheNextCollectiveCycle()
    {
        var state=Create();var first=Add(state,AntCaste.Soldier);Add(state,AntCaste.Soldier);var reserve=Add(state,AntCaste.Soldier);
        new QueenEscortSystem().Step(state);Assert.False(reserve.QueenEscort);
        state.Tick=2;first.Health=0;new SimulationEngine(state).Step();
        Assert.DoesNotContain(first,state.Ants);Assert.True(reserve.QueenEscort);
        Assert.Equal(2,state.Ants.Count(a=>a.QueenEscort));
    }
    [Fact]
    public void VacanciesWaitForEligibleAdultsAndAreFilledOnBirth()
    {
        var state=Create();var larva=Add(state,AntCaste.Worker,LifeStage.Larva);var system=new QueenEscortSystem();
        system.Step(state);Assert.DoesNotContain(state.Ants,a=>a.QueenEscort);
        larva.Stage=LifeStage.Adult;system.Step(state);Assert.True(larva.QueenEscort);
        Assert.Single(state.Ants,a=>a.QueenEscort);
    }
    [Fact]
    public void LeashWrapsAndMovementCannotLeaveItWhileFreeSoldiersCan()
    {
        var state=Create();var guard=Add(state,AntCaste.Soldier);guard.QueenEscort=true;
        var context=Context(state,guard);
        Assert.True(QueenEscortSystem.Allows(context,new(63,10)));Assert.False(QueenEscortSystem.Allows(context,new(24,10)));
        guard.Position=new(23,10);guard.Destination=new(24,10);guard.MovementCredit=2;
        Assert.False(context.Movement.Move(context));Assert.Equal(new CellPosition(23,10),guard.Position);
        guard.QueenEscort=false;Assert.True(context.Movement.Move(context));Assert.Equal(new CellPosition(24,10),guard.Position);
    }
    [Fact]
    public void EscortReturnsToMovingQueenThroughSupportedCells()
    {
        var state=Create();var guard=Add(state,AntCaste.Soldier);guard.QueenEscort=true;guard.Position=new(28,10);
        var context=Context(state,guard);new DecisionSystem(new()).Decide(context);
        Assert.Equal(state.Colonies[0].Queen!.Position,guard.Destination);Assert.True(guard.EscortReturning);
        guard.MovementCredit=2;Assert.True(context.Movement.Move(context));Assert.Equal(new CellPosition(27,10),guard.Position);
    }
    [Fact]
    public void FreeSoldierPatrolVisitsEveryCompletedChamber()
    {
        var state=Create();var soldier=Add(state,AntCaste.Soldier);var context=Context(state,soldier);var patrol=new NestPatrol();
        var visited=new HashSet<CellPosition>{soldier.Position};
        for(var i=0;i<8;i++)
        {
            var destination=patrol.Destination(context);Assert.NotNull(destination);
            OrganicNestTests.WalkTo(context,destination!.Value);visited.Add(soldier.Position);
        }
        Assert.All(context.Colony.Layout!.Chambers,c=>Assert.Contains(c.Access,visited));
        new DecisionSystem(new()).Decide(context);Assert.Equal(AntTask.Move,soldier.CurrentTask);Assert.NotNull(soldier.Destination);
    }
    [Fact]
    public void EggsAndLarvaeUseNearestSafeChamberWithoutTeleporting()
    {
        var state=Create();var colony=state.Colonies[0];var selection=new NurseryChamberSelection();
        var egg=Add(state,AntCaste.Worker,LifeStage.Egg);var larva=Add(state,AntCaste.Worker,LifeStage.Larva);
        colony.Queen!.Position=new(19,10);selection.Step(state,colony);
        Assert.Equal(2,colony.Layout!.BroodChamberId);Assert.Equal(new CellPosition(8,10),egg.Position);
        Assert.True(BroodTransport.NeedsRelocation(state.World,colony,egg));Assert.True(BroodTransport.NeedsRelocation(state.World,colony,larva));
        state.World.AddWater(new(20,10),200);selection.Step(state,colony);Assert.Equal(1,colony.Layout.BroodChamberId);
    }
}
