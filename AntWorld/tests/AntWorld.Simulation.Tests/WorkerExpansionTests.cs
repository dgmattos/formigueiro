using AntWorld.Domain;
using Xunit;
namespace AntWorld.Simulation.Tests;

public sealed class WorkerExpansionTests
{
    private static SimulationState Create(int workers=8) => SimulationFactory.Create(new(42,64,48),
        [new(0,.25) {Workers=workers,Soldiers=0,Larvae=0,InitialFood=1000}]);
    private static AntContext Context(SimulationState s,Ant a,MovementSystem? movement=null)
    { var index=new AgentSpatialIndex();index.Rebuild(s);return new(s,a,s.Colonies[0],index,movement??new()); }

    [Fact]
    public void RolesAreRandomDeterministicAndAssignedOnlyToAdultWorkers()
    {
        var a=Create(100);var b=Create(100);
        Assert.Equal(a.Ants.Select(x=>x.WorkerRole),b.Ants.Select(x=>x.WorkerRole));
        var workers=a.Ants.Where(x=>x.Caste==AntCaste.Worker).ToArray();
        Assert.InRange(workers.Count(x=>x.WorkerRole==WorkerRole.Collector),25,75);
        Assert.All(workers,x=>Assert.NotEqual(WorkerRole.None,x.WorkerRole));
        Assert.Equal(WorkerRole.None,a.Colonies[0].Queen!.WorkerRole);
        var brood=AntFactory.Create(a,a.Colonies[0],stage:LifeStage.Pupa);
        Assert.Equal(WorkerRole.None,brood.WorkerRole);
        a.Species[0]=new() {PupaDays=1.0/SimulationState.TicksPerDay};
        var biology=new AntBiologySystem();
        for(var i=0;i<20 && brood.Stage!=LifeStage.Adult;i++) biology.Step(a,brood);
        Assert.Equal(LifeStage.Adult,brood.Stage);Assert.NotEqual(WorkerRole.None,brood.WorkerRole);
        var role=brood.WorkerRole;AntFactory.AssignWorkerRole(a,brood);Assert.Equal(role,brood.WorkerRole);
    }

    [Fact]
    public void CollectorSeeksFoodWhileDiggerSelectsExpansionAndBothCareForEggs()
    {
        var s=Create();var worker=s.Ants.First(a=>a.Caste==AntCaste.Worker);worker.Energy=100;
        s.World.SetResource(worker.Position,ResourceIds.Food,new(10,10,10000));
        var decision=new DecisionSystem(new());var c=Context(s,worker);
        worker.WorkerRole=WorkerRole.Collector;decision.Decide(c);Assert.Equal(AntTask.Collect,worker.CurrentTask);
        worker.WorkerRole=WorkerRole.Digger;decision.Decide(c);Assert.Equal(AntTask.Move,worker.CurrentTask);
        var egg=AntFactory.Create(s,s.Colonies[0],stage:LifeStage.Egg);s.Ants.Add(egg);
        foreach(var role in new[]{WorkerRole.Collector,WorkerRole.Digger})
        {worker.WorkerRole=role;decision.Decide(Context(s,worker));Assert.Equal(AntTask.Carry,worker.CurrentTask);}
    }

    [Fact]
    public void WorkersExcavateBothSidesAndLowerFloorThenRelocateQueenAndEgg()
    {
        var s=Create();var colony=s.Colonies[0];var initial=colony.Nest;var nursery=colony.BroodChamber;
        // A known solid site makes every new opening attributable to workers, not procedural caves.
        for(var y=nursery.Y;y<s.World.Height;y++) for(var dx=-5;dx<=5;dx++)
            s.World.SetTerrain(new(initial.X+dx,y),TerrainKind.Soil);
        for(var dx=-2;dx<=2;dx++)s.World.SetTerrain(new(initial.X+dx,nursery.Y),TerrainKind.Tunnel);
        foreach(var a in s.Ants.Where(a=>a.Caste==AntCaste.Worker))a.WorkerRole=WorkerRole.Digger;
        colony.Queen!.StoredSperm=0;
        var egg=AntFactory.Create(s,colony,stage:LifeStage.Egg);s.Ants.Add(egg);
        var engine=new SimulationEngine(s);var transported=false;
        for(var tick=0;tick<12000;tick++)
        {
            engine.Step();transported|=egg.CarrierId.HasValue;
            if(colony.ExpansionLevel>=1 && colony.Queen.Position==colony.Nest && egg.Position==colony.BroodChamber && egg.CarrierId is null)break;
        }
        Assert.True(colony.ExpansionLevel>=1,$"No floor at tick {s.Tick}; tasks: {string.Join(',',s.Ants.Select(a=>$"{a.Id}:{a.CurrentTask}@{a.Position} E={a.Energy:F1}"))}");
        Assert.True(transported);Assert.Equal(colony.Nest,colony.Queen.Position);
        Assert.Equal(colony.BroodChamber,egg.Position);Assert.Null(egg.CarrierId);
        Assert.Equal(Colony.BroodChamberDepth,egg.Position.Y-colony.Queen.Position.Y);
        Assert.Equal(0,s.World.GetCell(new(initial.X-4,nursery.Y),false).SolidFill);
        Assert.Equal(0,s.World.GetCell(new(initial.X+4,nursery.Y),false).SolidFill);
        Assert.True(s.World.GetCell(new(initial.X,nursery.Y+4),false).Flags.HasFlag(CellFlags.Excavated));
        Assert.True(s.Ants.Where(a=>a.WorkerRole==WorkerRole.Digger).Sum(a=>a.Skills[(int)SkillType.Digging].Experience)>0);
    }

    [Fact]
    public void ExcavationChanceRunsOnceOnCompletionAndSpoilIsDepositedOutsideWithoutBecomingFood()
    {
        var s=Create();var ant=s.Ants.First(a=>a.Caste==AntCaste.Worker);var colony=s.Colonies[0];
        var c=Context(s,ant);var action=new DigAction();var target=new CellPosition(ant.Position.X,ant.Position.Y+1);
        s.World.SetTerrain(target,TerrainKind.Soil);ant.Destination=target;var rng=s.Random.State;
        Assert.True(action.Execute(c));Assert.Equal(rng,s.Random.State);Assert.Equal(0,ant.Carrying);
        var produced=0;
        for(var trial=0;trial<2000;trial++)
        {
            s.World.SetTerrain(target,TerrainKind.Soil);s.World.SetCell(target,s.World.GetCell(target,false) with {SolidFill=1});
            ant.Energy=100;ant.Carrying=0;ant.CargoResourceId=ResourceIds.Food;
            var replay=new DeterministicRandom(s.Random.State);var expected=replay.NextDouble()<.05;
            Assert.True(action.Execute(c));Assert.Equal(expected,ant.Carrying==1);Assert.Equal(replay.State,s.Random.State);
            if(!expected)continue;
            produced++;Assert.True(ExcavationSpoil.Is(s.World,ant.CargoResourceId));
            Assert.False(action.Execute(c));Assert.False(new DepositAction().Execute(c));
            var food=colony.Food;ant.Position=ExcavationSpoil.Exit(s.World,colony);
            Assert.True(new DepositAction().Execute(c));Assert.Equal(food,colony.Food);
            Assert.Equal(produced,s.World.GetResource(ant.Position,ExcavationSpoil.ResourceId(s.World)).Quantity);
            Assert.False(new DepositAction().Execute(c));ant.Position=colony.Nest;
        }
        Assert.InRange(produced,60,140);
    }

    [Fact]
    public void DeadCarrierDropsSpoilAndAnotherWorkerRetrievesIt()
    {
        var s=Create();var ants=s.Ants.Where(a=>a.Caste==AntCaste.Worker).ToArray();var dead=ants[0];var worker=ants[1];
        dead.Carrying=1;dead.CargoResourceId=ExcavationSpoil.ResourceId(s.World);dead.Damage(dead.Health);
        new SimulationEngine(s).Step();Assert.DoesNotContain(dead,s.Ants);
        worker.Position=dead.Position;worker.WorkerRole=WorkerRole.Digger;
        Assert.Equal(1,s.World.GetResource(dead.Position,dead.CargoResourceId).Quantity);
        Assert.True(new CollectAction().Execute(Context(s,worker)));Assert.Equal(1,worker.Carrying);
        Assert.Equal(0,s.World.GetResource(dead.Position,dead.CargoResourceId).Quantity);
    }

    [Fact]
    public void SpoilHaulerNavigatesAcrossSeamAndChunksToDeepNestsSurfaceExit()
    {
        var s=SimulationFactory.Create(new(42,64,48),[new(2,0){Workers=1,Soldiers=0,Larvae=0,InitialFood=1000}]);
        var colony=s.Colonies[0];colony.Queen!.StoredSperm=0;
        var worker=s.Ants.First(a=>a.Caste==AntCaste.Worker);worker.WorkerRole=WorkerRole.Digger;
        worker.Position=new(63,colony.Nest.Y);worker.PreviousPosition=worker.Position;
        worker.CargoResourceId=ExcavationSpoil.ResourceId(s.World);worker.Carrying=1;worker.Energy=100;
        var id=worker.CargoResourceId;var exit=ExcavationSpoil.Exit(s.World,colony);var engine=new SimulationEngine(s);
        for(var i=0;i<5000 && s.World.GetResource(exit,id).Quantity<1;i++)engine.Step();
        Assert.Equal(1,s.World.GetResource(exit,id).Quantity);Assert.Equal(0,worker.Carrying);
        Assert.Equal(0,s.Pheromones[colony.Id].Entries(PheromoneType.FoodTrail).Sum(p=>p.Value));
        Assert.Equal(0,s.World.ChunkManager.LoadedCount);
    }

    [Fact]
    public void SpoilSearchAndExpansionRespectBottomBoundary()
    {
        var s=Create();var colony=s.Colonies[0];var bottom=new CellPosition(colony.Nest.X,s.World.Height-1);
        s.World.SetTerrain(bottom,TerrainKind.Tunnel);ExcavationSpoil.Drop(s.World,bottom,1);
        Assert.True(DroppedSpoil.Find(s.World,bottom,out var found));Assert.Equal(bottom,found);
        while(colony.BroodChamber.Y+Colony.BroodChamberDepth<s.World.Height)colony.AdvanceFloor();
        colony.Queen!.Position=colony.Nest;
        new ColonyExpansionSystem().Step(s);
        Assert.True(colony.BroodChamber.Y<s.World.Height);
    }

    [Fact]
    public void SpoilCatalogUsesFreeIdentifierWithoutReplacingCustomResources()
    {
        var s=Create();
        s.World.ResourceCatalog.Register(new(7,"Custom","custom",true,0,new(31)));
        s.World.ResourceCatalog.Register(new(ushort.MaxValue,"Last","custom",true,0,new(31)));
        var id=ExcavationSpoil.ResourceId(s.World);
        Assert.Equal((ushort)8,id);Assert.Equal("Custom",s.World.ResourceCatalog.Get(7).Name);
        Assert.Equal(id,ExcavationSpoil.ResourceId(s.World));
    }

    [Fact]
    public void EmptyButDisconnectedCavernDoesNotBecomeNewNursery()
    {
        var s=Create();var colony=s.Colonies[0];var old=colony.Nest;var bottom=colony.BroodChamber.Y+4;
        for(var y=colony.BroodChamber.Y;y<=bottom;y++)
        for(var x=0;x<s.World.Width;x++)
            s.World.SetTerrain(new(x,y),y==bottom || y==colony.BroodChamber.Y ? TerrainKind.Tunnel : TerrainKind.Air);
        Assert.True(s.World.CanWalk(new(old.X,bottom)));
        Assert.False(s.World.CanWalk(new(old.X,bottom-2)));
        new ColonyExpansionSystem().Step(s);
        Assert.Equal(0,colony.ExpansionLevel);Assert.Equal(old,colony.Nest);
    }

    [Fact]
    public void EggEnergyStaysConstantAndLarvalMetabolismIsHalfItsPreviousRate()
    {
        var s=Create();var colony=s.Colonies[0];var adult=AntFactory.Create(s,colony);
        var larva=AntFactory.Create(s,colony,stage:LifeStage.Larva);var egg=AntFactory.Create(s,colony,stage:LifeStage.Egg);
        var biology=new AntBiologySystem();
        for(var i=0;i<1000;i++){s.Tick++;biology.Step(s,adult);biology.Step(s,larva);biology.Step(s,egg);}
        Assert.Equal(80,egg.Energy);
        Assert.Equal((80-adult.Energy)*.25,80-larva.Energy,8);
        Assert.Equal((1-adult.Nutrition)*.25,1-larva.Nutrition,8);
    }
}
