using AntWorld.Domain;
using Xunit;
namespace AntWorld.Simulation.Tests;

public sealed class ForagingNavigationTests
{
    private static SimulationState Maze()
    {
        var s=SimulationFactory.Create(new(42,64,48),[new(0,.125){Workers=1,Soldiers=0,Larvae=0,InitialFood=0}]);
        for(var y=0;y<s.World.Height;y++) for(var x=0;x<s.World.Width;x++) s.World.SetTerrain(new(x,y),TerrainKind.Soil);
        foreach(var e in s.World.Resources().Where(e=>ResourceIds.IsEdible(e.ResourceId)).ToArray())s.World.SetResource(e.Position,e.ResourceId,default);
        for(var x=3;x<=35;x++)s.World.SetTerrain(new(x,12),TerrainKind.Tunnel);
        for(var y=12;y<=24;y++)s.World.SetTerrain(new(15,y),TerrainKind.Tunnel);
        for(var y=6;y<=12;y++)s.World.SetTerrain(new(25,y),TerrainKind.Tunnel);
        var colony=s.Colonies[0];
        var escort=AntFactory.Create(s,colony);escort.QueenEscort=true;s.Ants.Insert(1,escort);
        colony.RelocateChamber(new(8,12));colony.Queen!.StoredSperm=0;
        foreach(var a in s.Ants) {a.Position=a.PreviousPosition=colony.Nest;a.Energy=100;}
        s.Ants.Last().WorkerRole=WorkerRole.Collector;
        return s;
    }
    private static AntContext Context(SimulationState s,Ant a)
    {var index=new AgentSpatialIndex();index.Rebuild(s);return new(s,a,s.Colonies[0],index,new());}

    [Fact]
    public void EveryOrthogonalSideSupportsMovementButDiagonalsAndExcavatedFlagsDoNot()
    {
        var world=new WorldMap(new(42,64,48));var p=new CellPosition(0,20);
        world.SetTerrain(p,TerrainKind.Tunnel);
        Assert.False(world.CanWalk(p));Assert.Equal(SupportSide.None,world.ContactSide(p));
        world.SetTerrain(new(63,19),TerrainKind.Rock);Assert.False(world.CanWalk(p));
        foreach(var side in new[]{SupportSide.Down,SupportSide.Left,SupportSide.Up,SupportSide.Right})
        {
            var offset=SurfaceOrientation.Offset(side);var support=new CellPosition(p.X+offset.X,p.Y+offset.Y);
            world.SetTerrain(support,TerrainKind.Rock);Assert.True(world.CanWalk(p));Assert.Equal(side,world.ContactSide(p));
            var angle=SurfaceOrientation.Angle(side);
            Assert.Equal(offset.X,-Math.Sin(angle),8);Assert.Equal(offset.Y,Math.Cos(angle),8);
            world.SetTerrain(support,TerrainKind.Air);
        }
        Assert.Equal(0,world.ChunkManager.LoadedCount);
    }

    [Fact]
    public void PartialExcavationDoesNotOpenARouteAndLosingLastSupportCausesFall()
    {
        var s=Maze();var worker=s.Ants.Last();var p=new CellPosition(15,20);
        s.World.SetTerrain(p,TerrainKind.Soil);var rev=s.World.NavigationRevision;
        s.World.SetCell(p,s.World.GetCell(p,false) with {SolidFill=30});Assert.False(s.World.CanWalk(p));
        s.World.SetTerrain(p,TerrainKind.Tunnel);Assert.True(s.World.NavigationRevision>rev);
        worker.Position=p;s.World.SetTerrain(new(14,20),TerrainKind.Air);s.World.SetTerrain(new(16,20),TerrainKind.Air);
        Assert.False(s.World.CanWalk(p));worker.MovementCredit=2;
        Assert.False(Context(s,worker).Movement.Move(Context(s,worker)));Assert.Equal(p,worker.Position);
        SurfaceAttachmentSystem.Step(s.World,worker);
        Assert.Equal(new CellPosition(15,21),worker.Position);Assert.Equal(MovementState.Falling,worker.Movement);
        Assert.NotEqual(SupportSide.None,worker.Support);
    }

    [Fact]
    public void CollectorSearchesBeyondLocalRadiusAndDeliversMushroomsToQueen()
    {
        var s=Maze();var colony=s.Colonies[0];var worker=s.Ants.Last();var mushroom=new CellPosition(35,13);
        s.World.SetResource(mushroom,ResourceIds.RedMushroom,new(4,4,10000));
        Assert.True(s.World.Distance(worker.Position,new(35,12))>4);
        // The nursery must be reachable for the new delivery rule when the queen is satiated.
        for(var y=13;y<=16;y++)s.World.SetTerrain(new(8,y),TerrainKind.Tunnel);
        var engine=new SimulationEngine(s);var delivered=false;var visited=new HashSet<CellPosition>();
        for(var i=0;i<10000;i++)
        {
            engine.Step();visited.Add(worker.Position);
            Assert.True(s.World.CanWalk(worker.Position));
            if(engine.Events.Any(e=>e.AntId==worker.Id && e.Action==AntTask.Deposit)) {delivered=true;break;}
        }
        Assert.True(delivered,$"task={worker.CurrentTask} pos={worker.Position} target={worker.Destination} visited={visited.Count}");
        Assert.Equal(0,worker.Carrying);
        var deposited=LocalProvisions.Amount(s.World,colony.Queen!.Position)+LocalProvisions.Amount(s.World,colony.BroodChamber);
        Assert.InRange(deposited,.01,4);
        Assert.True(s.World.Distance(worker.Position,colony.Queen.Position)==1 || s.World.Distance(worker.Position,colony.BroodChamber)==1);Assert.Equal(0,s.World.GetResource(mushroom,ResourceIds.RedMushroom).Quantity);
        Assert.Contains(new CellPosition(35,12),visited);Assert.True(visited.Count>25);
        Assert.True(colony.Foraging.Visits.Count>25);
    }

    [Fact]
    public void EmptyBranchesAreExploredInsteadOfOscillatingAtTheNest()
    {
        var s=Maze();var worker=s.Ants.Last();var engine=new SimulationEngine(s);var visited=new HashSet<CellPosition>();
        for(var i=0;i<8000;i++){engine.Step();visited.Add(worker.Position);}
        foreach(var end in new[]{new CellPosition(3,12),new CellPosition(35,12),new CellPosition(15,24),new CellPosition(25,6)})
            Assert.Contains(end,visited);
        Assert.True(visited.Count>=48,$"Visited {visited.Count} cells");
        Assert.Equal(0,s.World.ChunkManager.LoadedCount);
    }

    [Fact]
    public void ForagerPrefersUsefulLoadOverNearbyCrumbsConsideringReturnTrip()
    {
        var s=Maze();var worker=s.Ants.Last();worker.Position=worker.PreviousPosition=new(10,12);
        s.World.SetResource(worker.Position,ResourceIds.Food,new(.1,.1,10000));
        s.World.SetResource(new(20,13),ResourceIds.RedMushroom,new(4,4,10000));
        s.Colonies[0].Foraging.Observe(s.World.Index(new(20,12)),0,false,true);
        var context=Context(s,worker);new DecisionSystem(new()).Decide(context);
        Assert.Equal(AntTask.Explore,worker.CurrentTask);Assert.Equal(new CellPosition(20,12),worker.Destination);
        // When both are at the same position, harvest the useful batch rather than the first resource ID.
        worker.Position=new(20,12);s.World.SetResource(worker.Position,ResourceIds.Food,new(.1,.1,10000));
        Assert.True(new CollectAction().Execute(context));Assert.Equal(4,worker.Carrying);
        Assert.Equal(ResourceIds.RedMushroom,worker.CargoResourceId);
        Assert.Equal(.1,s.World.GetResource(worker.Position,ResourceIds.Food).Quantity);
    }

    [Fact]
    public void NavigationProgressesAcrossSeamDespiteFoodBehindCarrier()
    {
        var s=Maze();var worker=s.Ants.Last();var world=s.World;
        for(var x=0;x<world.Width;x++)world.SetTerrain(new(x,12),TerrainKind.Tunnel);
        worker.Position=worker.PreviousPosition=new(60,12);worker.Carrying=4;worker.CargoResourceId=ResourceIds.RedMushroom;
        worker.Destination=s.Colonies[0].Nest;world.SetResource(new(59,13),ResourceIds.BrownMushroom,new(4,4,10000));
        s.Pheromones[1].Deposit(PheromoneType.FoodTrail,world.Index(new(59,12)),15);
        var context=Context(s,worker);var distances=new NestNavigation().Distances(world,s.Colonies[0]);
        for(var i=0;i<12;i++)
        {
            var before=distances[world.Index(worker.Position)];worker.MovementCredit=2;
            Assert.True(context.Movement.Move(context,true));Assert.Equal(before-1,distances[world.Index(worker.Position)]);
        }
        Assert.Equal(s.Colonies[0].Nest,worker.Position);
    }
}
