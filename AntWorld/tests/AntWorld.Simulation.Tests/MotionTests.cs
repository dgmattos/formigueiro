using AntWorld.Domain;
using Xunit;
namespace AntWorld.Simulation.Tests;

public sealed class MotionTests
{
    private static (SimulationState State,Ant Ant,AntContext Context) Create()
    {
        var state=SimulationFactory.Create(new(42,64,48),[new(0,.5){Workers=1,Soldiers=0,Larvae=0}]);
        var ant=state.Ants.First(a=>a.Caste==AntCaste.Worker);var world=state.World;
        for(var y=5;y<=11;y++)for(var x=3;x<=8;x++)world.SetTerrain(new(x,y),y==11||x==8?TerrainKind.Soil:TerrainKind.Air);
        ant.Position=ant.PreviousPosition=new(7,10);ant.Destination=new(6,10);ant.CurrentTask=AntTask.Move;
        var index=new AgentSpatialIndex();index.Rebuild(state);
        return(state,ant,new(state,ant,state.Colonies[0],index,new()));
    }
    [Fact]
    public void AcceleratesGraduallyAndRestDoesNotStoreABurstOfMovement()
    {
        var(s,ant,c)=Create();var maximum=c.Movement.EffectiveSpeed(s,ant);
        c.Movement.AdvanceKinematics(s,ant);Assert.InRange(ant.MovementSpeed,.0001,maximum*.11);
        for(var i=0;i<10;i++)c.Movement.AdvanceKinematics(s,ant);
        Assert.Equal(maximum,ant.MovementSpeed,10);
        ant.CurrentTask=AntTask.Rest;
        for(var i=0;i<10;i++)c.Movement.AdvanceKinematics(s,ant);
        Assert.Equal(0,ant.MovementSpeed);Assert.Equal(0,ant.MovementCredit);
    }
    [Fact]
    public void VerticalStepsRequireMoreTimeThanHorizontalSteps()
    {
        var(s,ant,c)=Create();ant.MovementCredit=1;
        Assert.True(c.Movement.Move(c));Assert.Equal(new CellPosition(6,10),ant.Position);
        ant.Position=ant.PreviousPosition=new(7,10);ant.Destination=new(7,9);ant.MovementCredit=1;
        Assert.False(c.Movement.Move(c));Assert.Equal(new CellPosition(7,10),ant.Position);
        ant.MovementCredit=1/MovementSystem.VerticalSpeedMultiplier;
        Assert.True(c.Movement.Move(c));Assert.Equal(new CellPosition(7,9),ant.Position);Assert.Equal(0,ant.MovementCredit,10);
    }
    [Fact]
    public void PresentationInterpolatesActualSegmentsAndFreezesAtZeroElapsedTime()
    {
        var(s,ant,_)=Create();var track=new AgentMotionTrack(s.World,ant);var start=track.X;
        ant.Position=new(6,10);track.Capture(s.World,ant,1);
        track.Advance(.5);Assert.InRange(track.X,start-1+.001,start-.001);
        var half=track.X;track.Advance(0);Assert.Equal(half,track.X);
        track.Advance(.5);Assert.Equal(6.5,track.X,10);Assert.Equal(0,track.PendingSegments);
        Assert.Equal(new CellPosition(6,10),ant.Position);
    }
    [Fact]
    public void PresentationWrapsByOneCellAndKeepsABoundedQueue()
    {
        var(s,ant,_)=Create();var w=s.World;
        for(var x=0;x<w.Width;x++){w.SetTerrain(new(x,3),TerrainKind.Air);w.SetTerrain(new(x,4),TerrainKind.Soil);}
        ant.Position=new(63,3);var track=new AgentMotionTrack(w,ant);var start=track.X;
        ant.Position=new(0,3);track.Capture(w,ant,1);track.Advance(.5);Assert.Equal(start+.5,track.X,10);
        for(var i=1;i<100;i++){ant.Position=new(i%64,3);track.Capture(w,ant,1);}
        Assert.InRange(track.PendingSegments,0,AgentMotionTrack.MaximumSegments);
    }
}
