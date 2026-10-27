using AntWorld.Domain;
using Xunit;
namespace AntWorld.Simulation.Tests;

public sealed class QueenChamberSelectionTests
{
    private static SimulationState Create()
    {
        var world=new WorldMap(new(42,64,48),surface:Enumerable.Repeat(11,64).ToArray());
        for(var x=0;x<64;x++)world.SetTerrain(new(x,11),TerrainKind.Soil);
        var state=new SimulationState(world,DeterministicRandom.FromSeed(42));
        var colony=new Colony(1,0,new(8,10),Genome.Ember){Layout=new(1)};
        state.Colonies.Add(colony);state.Pheromones[1]=new(world.CellCount);
        var queen=AntFactory.Create(state,colony,AntCaste.Queen);colony.Queen=queen;colony.QueenId=queen.Id;state.Ants.Add(queen);
        foreach(var(id,x)in new[]{(1,8),(2,20),(3,40),(4,56)})
            colony.Layout.AddChamber(new(id,NestChamberType.General,new(x,10),1,1,NestStructureStatus.Completed){Access=new(x,10),Cells=[new(x,10)]});
        colony.Layout.AddEntrance(new(1,new(0,10),1,NestStructureStatus.Completed));
        return state;
    }
    [Fact]
    public void SelectsGreatestWalkingDistanceAcrossCircularSeamWithoutTeleporting()
    {
        var state=Create();var colony=state.Colonies[0];var position=colony.Queen!.Position;
        new QueenChamberSelection().Step(state,colony);
        Assert.Equal(3,colony.Layout!.QueenChamberId); // 24 steps from exit; x=56 is only eight steps across wrap.
        Assert.Equal(position,colony.Queen.Position);
        var index=new AgentSpatialIndex();index.Rebuild(state);var movement=new MovementSystem();var decision=new DecisionSystem(new());
        var context=new AntContext(state,colony.Queen,colony,index,movement);
        decision.Decide(context);Assert.Equal(AntTask.Move,colony.Queen.CurrentTask);Assert.Equal(colony.Nest,colony.Queen.Destination);
        OrganicNestTests.WalkTo(context,colony.Nest);Assert.Equal(new CellPosition(40,10),colony.Queen.Position);
    }
    [Fact]
    public void UsesNearestOfMultipleExitsAndReevaluatesNewAndBlockedEntrances()
    {
        var state=Create();var colony=state.Colonies[0];var selection=new QueenChamberSelection();
        selection.Step(state,colony);Assert.Equal(3,colony.Layout!.QueenChamberId);
        colony.Layout.AddEntrance(new(2,new(40,10),3,NestStructureStatus.Completed));
        selection.Step(state,colony);Assert.Equal(2,colony.Layout.QueenChamberId);
        colony.Layout.Entrances[1]=colony.Layout.Entrances[1] with{Safe=false};
        selection.Step(state,colony);Assert.Equal(3,colony.Layout.QueenChamberId);
    }
    [Fact]
    public void RejectsUnfinishedFloodedAndDisconnectedRoomsAndKeepsCurrentOnTies()
    {
        var state=Create();var colony=state.Colonies[0];var layout=colony.Layout!;var selection=new QueenChamberSelection();
        layout.Chambers[2]=layout.Chambers[2] with{Status=NestStructureStatus.Planned};
        state.World.AddWater(layout.Chambers[1].Access,200);
        selection.Step(state,colony);Assert.Equal(1,layout.QueenChamberId); // x8/x56 tie: retain current.
        layout.QueenChamberId=4;selection.Step(state,colony);Assert.Equal(4,layout.QueenChamberId);
        layout.AddChamber(new(5,NestChamberType.General,new(32,30),1,1,NestStructureStatus.Completed){Access=new(32,30),Cells=[new(32,30)]});
        state.World.SetTerrain(new(32,31),TerrainKind.Soil);
        selection.Step(state,colony);Assert.Equal(4,layout.QueenChamberId);
        layout.Entrances[0]=layout.Entrances[0] with{Safe=false};
        selection.Step(state,colony);Assert.Equal(4,layout.QueenChamberId);
    }
}
