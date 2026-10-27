using AntWorld.Domain;
using AntWorld.WorldGeneration;
using Xunit;
namespace AntWorld.Simulation.Tests;

public sealed class OrganicNestTests
{
    internal static void WalkTo(AntContext c,CellPosition destination)
    {
        for(var fall=0;fall<c.State.World.Height&&!c.State.World.CanWalk(c.Ant.Position);fall++)SurfaceAttachmentSystem.Step(c.State.World,c.Ant);
        c.Ant.Destination=destination;
        var distance=c.Movement.DistanceTo(c.State.World,c.Ant.Position,destination);Assert.True(distance>=0,$"No route {c.Ant.Position} -> {destination}");
        for(var i=0;i<=distance&&c.Ant.Position!=destination;i++)
        {c.Ant.MovementCredit=1;Assert.True(c.Movement.Move(c));Assert.True(c.State.World.CanWalk(c.Ant.Position));}
        Assert.Equal(destination,c.Ant.Position);
    }
    [Theory]
    [InlineData(0)] [InlineData(2)] [InlineData(3)]
    public void FoundationIsDeterministicConnectedAndHasDistinctFunctionalRooms(int species)
    {
        var a=SimulationFactory.Create(new(10000,512,128),[new(species,.5)]);
        var b=SimulationFactory.Create(new(10000,512,128),[new(species,.5)]);
        var colony=a.Colonies[0];var layout=colony.Layout!;
        Assert.Equal(4,layout.Chambers.Count);Assert.True(layout.IsConnected(true));
        Assert.Equal(layout.Chambers.Select(c=>c.Access),b.Colonies[0].Layout!.Chambers.Select(c=>c.Access));
        Assert.NotEqual(colony.Nest,colony.FoodStorage);Assert.True(colony.BroodChamber.Y>colony.Nest.Y);
        var reachable=new NestNavigation().Distances(a.World,colony.Nest);
        Assert.All(layout.Chambers,c=>{Assert.All(c.Cells,p=>Assert.True(a.World.IsOpen(p)));Assert.Contains(a.World.Index(c.Access),reachable.Keys);});
        Assert.All(layout.Entrances.Where(e=>e.Safe),e=>Assert.Contains(a.World.Index(e.Position),reachable.Keys));
        Assert.InRange(layout.Entrances.Count(e=>e.Safe),2,5);Assert.Equal(0,a.World.ChunkManager.LoadedCount);
    }
    [Fact]
    public void FoundationRestoresMushroomBoundsAndRejectedFoundationIsAtomic()
    {
        var state=SimulationFactory.Create(new(42,128,64),[new(0,.5)]);
        foreach(var region in state.World.Chunks())
        {
            var sites=Enumerable.Range(0,region.Width*region.Height).Count(i=>MushroomHabitat.Allows(state.World,region.Position(i)));
            Assert.InRange(state.World.CountMushrooms(region),Math.Min(sites*2,state.World.Configuration.Mushrooms.MinimumPerChunk),state.World.Configuration.Mushrooms.MaximumPerChunk);
        }
        var world=new WorldGenerator().Generate(new(42,128,64));var revision=world.TerrainRevision;
        var changes=world.ChunkManager.ResourceChanges.ToArray();
        Assert.Throws<InvalidOperationException>(()=>NestFoundation.Create(world,1,64,0,founders:[new(0,0)]));
        Assert.Equal(revision,world.TerrainRevision);Assert.Equal(changes,world.ChunkManager.ResourceChanges.ToArray());
    }
    [Fact]
    public void PlanningDoesNotCarveAndLoopsAreUniqueAndConnected()
    {
        var state=SimulationFactory.Create(new(10001,512,128),[new(0,.5)]);var layout=state.Colonies[0].Layout!;
        var revision=state.World.TerrainRevision;var rng=state.Random.State;var planner=new OrganicNestPlanner();
        while(layout.Chambers.Count<12)Assert.True(planner.TryExpand(state.World,layout,0));
        Assert.Equal(revision,state.World.TerrainRevision);Assert.Equal(rng,state.Random.State);
        Assert.True(layout.IsConnected());Assert.InRange(layout.AlternativeRouteRatio(),.2,.3);
        Assert.Equal(4,layout.Chambers.Count(c=>c.Status==NestStructureStatus.Completed));
        Assert.Equal(layout.Tunnels.Count,layout.Tunnels.Select(t=>(Math.Min(t.FromChamberId,t.ToChamberId),Math.Max(t.FromChamberId,t.ToChamberId))).Distinct().Count());
        Assert.True(layout.Chambers.Select(c=>(c.RadiusX,c.RadiusY)).Distinct().Count()>=3);
    }
    [Fact]
    public void ExpansionUsesExclusiveBoundedJobsAndActualDigging()
    {
        var s=SimulationFactory.Create(new WorldConfiguration(42,128,64){NestLayout=new(MaximumChambers:5)},[new(0,.5){Workers=32,Larvae=0,Soldiers=0,InitialFood=10000}]);
        var colony=s.Colonies[0];var layout=colony.Layout!;var planner=new OrganicNestPlanner();
        Assert.True(planner.TryExpand(s.World,layout,0,NestChamberType.Brood));OrganicNestConstruction.Enqueue(s.World,layout);
        var construction=new OrganicNestConstruction();var index=new AgentSpatialIndex();index.Rebuild(s);var movement=new MovementSystem();
        var workers=s.Ants.Where(a=>a.Caste==AntCaste.Worker).ToArray();foreach(var ant in workers)ant.WorkerRole=WorkerRole.Digger;
        var targetRoom=layout.Chambers[^1];var initialFood=colony.Food;var actions=0;
        var requiredActions=layout.Construction.Jobs.Sum(job=>
        {
            var cell=s.World.GetCell(job.Target,false);
            var minimumWork=workers.Min(a=>Math.Max(1,(int)ExcavationRules.Work(new(s,a,colony,index,movement),cell)));
            return (int)Math.Ceiling(cell.SolidFill/(double)minimumWork);
        });
        var workload=requiredActions*4; // Allows dependencies and supply trips; no ecological multi-day run.
        for(var step=0;step<workload&&layout.Construction.HasPending;step++)
        {
            s.Tick+=ExcavationRules.ActionIntervalTicks;construction.Step(s,colony);
            Assert.InRange(layout.Construction.Jobs.Count(j=>j.Status is NestStructureStatus.Available or NestStructureStatus.InProgress),0,6);
            foreach(var ant in workers)
            {
                var c=new AntContext(s,ant,colony,index,movement);
                if(ant.Carrying>0){WalkTo(c,ExcavationSpoil.Exit(s.World,colony));Assert.True(new DepositAction().Execute(c));}
                if(ant.Energy<10){WalkTo(c,colony.FoodStorage);while(ant.Energy<70)Assert.True(new FeedAction().Execute(c));}
                if(!OrganicNestConstruction.FindJob(c,out var target,out var access))continue;
                WalkTo(c,access);ant.Destination=target;if(new DigAction().Execute(c))actions++;
            }
        }
        construction.Step(s,colony);
        Assert.True(actions>0);Assert.True(colony.Food<=initialFood);
        Assert.Equal(NestStructureStatus.Completed,layout.Chambers.Single(c=>c.Id==targetRoom.Id).Status);
        Assert.All(layout.Tunnels,t=>Assert.Equal(NestStructureStatus.Completed,t.Status));
        Assert.Contains(layout.Tunnels,t=>t.IsAlternative);
        Assert.True(workers.Sum(a=>a.Skills[(int)SkillType.Digging].Experience)>0);
        Assert.True(movement.DistancesFrom(s.World,colony.Nest).ContainsKey(s.World.Index(targetRoom.Access)));
        Assert.Equal(layout.Construction.Jobs.Count,layout.Construction.Jobs.Select(j=>j.Target).Distinct().Count());
    }
    [Fact]
    public void BlockedTunnelReplansWithoutCarvingOrDuplicatingJobs()
    {
        var s=SimulationFactory.Create(new(42,128,64),[new(0,.5)]);var colony=s.Colonies[0];var layout=colony.Layout!;
        Assert.True(new OrganicNestPlanner().TryExpand(s.World,layout,0));OrganicNestConstruction.Enqueue(s.World,layout);
        var tunnel=layout.Tunnels.First(t=>t.Status!=NestStructureStatus.Completed);
        var reserved=layout.Chambers.SelectMany(c=>c.Cells).Concat(layout.Tunnels.Where(t=>t!=tunnel).SelectMany(t=>t.Cells)).ToHashSet();
        var blocked=tunnel.Cells.First(p=>s.World.CanDig(p)&&!reserved.Contains(p));s.World.AddWater(blocked,200);
        var revision=s.World.TerrainRevision;var random=s.Random.State;s.Tick=s.World.Configuration.NestLayout.PlanningIntervalTicks;
        new OrganicNestConstruction().Step(s,colony);
        Assert.DoesNotContain(blocked,layout.Tunnels.Single(t=>t.Id==tunnel.Id).Cells);
        Assert.Equal(revision,s.World.TerrainRevision);Assert.Equal(random,s.Random.State);
        Assert.Equal(NestStructureStatus.Cancelled,layout.Construction.Jobs.Single(j=>j.Target==blocked).Status);
        Assert.Equal(layout.Construction.Jobs.Count,layout.Construction.Jobs.Select(j=>j.Target).Distinct().Count());
        Assert.Contains(layout.Diagnostics,message=>message.Contains("replanejado"));
    }
    [Fact]
    public void FloodedJobIsBlockedAndNoTwoWorkersClaimTheSameFront()
    {
        var s=SimulationFactory.Create(new(42,128,64),[new(0,.5){Workers=8}]);var colony=s.Colonies[0];var layout=colony.Layout!;
        Assert.True(new OrganicNestPlanner().TryExpand(s.World,layout,0));OrganicNestConstruction.Enqueue(s.World,layout);
        var construction=new OrganicNestConstruction();s.Tick=20;construction.Step(s,colony);
        var front=layout.Construction.Jobs.First(j=>j.Status==NestStructureStatus.Available);s.World.AddWater(front.Target,200);construction.Step(s,colony);
        Assert.Equal(NestStructureStatus.Blocked,front.Status);Assert.NotNull(front.BlockedReason);
        var index=new AgentSpatialIndex();index.Rebuild(s);var targets=new HashSet<CellPosition>();
        foreach(var worker in s.Ants.Where(a=>a.Caste==AntCaste.Worker))
            if(OrganicNestConstruction.FindJob(new(s,worker,colony,index,new()),out var target,out _))Assert.True(targets.Add(target));
    }
}
