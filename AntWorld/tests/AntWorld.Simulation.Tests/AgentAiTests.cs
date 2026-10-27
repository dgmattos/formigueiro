using AntWorld.Domain;
using AntWorld.Simulation;
using Xunit;
namespace AntWorld.Simulation.Tests;

public sealed class AgentAiTests
{
    private static SimulationState Create() => SimulationFactory.Create(new(42,64,48));
    private static Ant Worker(SimulationState s,int colony=1) => s.Ants.First(a=>a.ColonyId==colony&&a.Caste==AntCaste.Worker&&a.Stage==LifeStage.Adult);
    private static AntContext Context(SimulationState s,Ant a)
    { var index=new AgentSpatialIndex();index.Rebuild(s);return new(s,a,s.Colonies.First(c=>c.Id==a.ColonyId),index,new()); }

    [Fact] public void HealthMaximumDoesNotHealAndQueenHasNoAgeDeath()
    {
        var s=Create();var queen=s.Colonies[0].Queen!;
        queen.Damage(30);queen.ChangeMaximumHealth(300);Assert.Equal(220,queen.Health);
        queen.Recover(200);Assert.Equal(300,queen.Health);queen.ChangeMaximumHealth(100);Assert.Equal(100,queen.Health);
        queen.Age=long.MaxValue/2;
        Assert.False(new NoAgeLimitPolicy().ShouldDie(queen,AntCapabilities.Definition(s,queen),s,10000));
        queen.Damage(100);Assert.False(queen.IsAlive);
    }
    [Fact] public void CasteAndStageCapabilitiesAreEnforced()
    {
        var s=Create();var queen=s.Colonies[0].Queen!;var worker=Worker(s);var larva=s.Ants.First(a=>a.Stage==LifeStage.Larva);
        Assert.False(AntCapabilities.Has(s,queen,AntCapability.Collect));Assert.False(AntCapabilities.Has(s,queen,AntCapability.Dig));
        Assert.True(AntCapabilities.Has(s,worker,AntCapability.Collect));Assert.False(AntCapabilities.Has(s,larva,AntCapability.Move));
        Assert.Equal(BiologicalSex.Female,queen.Sex);
        var male=AntFactory.Create(s,s.Colonies[0],AntCaste.MaleReproductive);Assert.Equal(BiologicalSex.Male,male.Sex);
    }
    [Fact] public void UtilityChoosesFoodForCriticalEnergy()
    {
        var s=Create();var a=Worker(s);a.Energy=1;a.CurrentTask=AntTask.Explore;
        new DecisionSystem(new()).Decide(Context(s,a));Assert.Equal(AntTask.Feed,a.CurrentTask);
        Assert.True(new FeedAction().Execute(Context(s,a)));Assert.True(a.Energy>1);
    }
    [Fact] public void QueenSelectsReproductionAndLarvaeDoNotNavigate()
    {
        var s=Create();var queen=s.Colonies[0].Queen!;queen.Energy=100;
        new DecisionSystem(new()).Decide(Context(s,queen));Assert.Equal(AntTask.LayEgg,queen.CurrentTask);
        var larva=s.Ants.First(a=>a.Stage==LifeStage.Larva);var position=larva.Position;larva.MovementCredit=2;
        Assert.False(new ExploreAction().Execute(Context(s,larva)));Assert.Equal(position,larva.Position);
    }
    [Fact] public void RecognitionUsesKinshipChemistryAndMembershipIndependently()
    {
        var s=Create();var a=Worker(s);var b=Worker(s,2);var service=new RecognitionService();
        b.MotherId=a.MotherId;var related=service.Recognize(a,b,s);
        Assert.Equal(FamiliarityLevel.CloseRelative,related.Familiarity);Assert.False(related.KnownMembership);
        b.MotherId=null;b.LineageId=999;b.SpeciesId=a.SpeciesId;
        Assert.Equal(FamiliarityLevel.SameSpecies,service.Recognize(a,b,s).Familiarity);
        var nestmate=s.Colonies[0].Queen!;Assert.True(service.Recognize(a,nestmate,s).KnownMembership);
    }
    [Fact] public void ColonySignaturesAreSimilarAndForeignProfilesDiffer()
    {
        var s=Create();var a=Worker(s);var same=s.Colonies[0].Queen!;var foreign=Worker(s,2);
        Assert.True(a.Signature.Similarity(same.Signature)>.9);Assert.True(a.Signature.Similarity(foreign.Signature)<.8);
        var signature=a.Signature;
        for(var i=0;i<64;i++) signature=signature.Assimilate(foreign.Signature,i);
        Assert.Equal(foreign.Signature,signature);
    }
    [Fact] public void NeutralColoniesCoexistAndNestmatesNeverAttack()
    {
        var s=Create();var a=Worker(s);var b=Worker(s,2);b.Position=a.Position;var combat=new CombatSystem();
        Assert.False(combat.Attack(s,a,b));Assert.False(combat.Attack(s,a,s.Colonies[0].Queen));
        var health=b.Health;s.Relations.Set(1,2,ColonyRelation.Allied);combat.Step(s);Assert.Equal(health,b.Health);
    }
    [Fact] public void HostilityCrossesSpeciesAndAttackCooldownIsEnforced()
    {
        var s=Create();var a=Worker(s);var b=Worker(s,2);b.SpeciesId=a.SpeciesId;b.Position=a.Position;s.Relations.Set(1,2,ColonyRelation.Hostile);
        var combat=new CombatSystem();var before=b.Health;Assert.True(combat.Attack(s,a,b));Assert.True(b.Health<before);
        var after=b.Health;Assert.False(combat.Attack(s,a,b));Assert.Equal(after,b.Health);
        s.Tick+=20;Assert.True(combat.Attack(s,a,b));
    }
    [Fact] public void SpatialRecognitionCrossesCircularSeam()
    {
        var s=Create();var a=Worker(s);var b=Worker(s,2);a.Position=new(0,20);b.Position=new(63,20);
        var index=new AgentSpatialIndex();index.Rebuild(s);var results=new Ant[100];var count=index.Nearby(s.World,a.Position,results,1);
        Assert.Contains(b,results.Take(count));s.Relations.Set(1,2,ColonyRelation.Hostile);
        Assert.True(new RecognitionService().Recognize(a,b,s).PossibleThreat);
    }
    [Fact] public void SchedulerSpreadsDecisionsAndDoesNotStarveOverBudget()
    {
        var s=Create();s.Performance=s.Performance with {MaxDetailedAgents=20};var scheduler=new AgentScheduler();var seen=new HashSet<int>();var perTick=new List<int>();
        for(var tick=1;tick<=100;tick++) {s.Tick=tick;scheduler.Decide(s,a=>seen.Add(a.Id));perTick.Add(scheduler.DecisionsLastTick);}
        Assert.Equal(s.Ants.Count(a=>a.Stage==LifeStage.Adult),seen.Count);Assert.All(perTick,n=>Assert.InRange(n,0,2));Assert.True(perTick.Count(n=>n>0)>10);
    }
    [Fact] public void RenderBudgetDoesNotRemoveOrStopIndividuals()
    {
        var s=Create();s.Performance=s.Performance with {MaxRenderedAgents=3};var count=s.Ants.Count;var selected=new List<Ant>();
        Assert.Equal(3,AgentRenderSelection.Select(s,new(0,0),new(64,48),selected));Assert.Equal(count,s.Ants.Count);
        var unrendered=s.Ants.Last();new SimulationEngine(s).Run(20);Assert.Equal(20,unrendered.Age);Assert.Contains(unrendered,s.Ants);
    }
    [Fact] public void PheromoneLayersEvaporateIndependently()
    {
        var field=new PheromoneField(4096);foreach(var type in Enum.GetValues<PheromoneType>()) field.Deposit(type,10,10);
        field.Evaporate(20,0);Assert.InRange(field[10],0,10);Assert.True(field.Get(PheromoneType.Alarm,10)<field[10]);
        var before=field[10];field.Evaporate(20,0);Assert.Equal(before,field[10]);
    }
    [Fact] public void FoodTrailBiasesMovementAndSolidBlocksRemainImpassable()
    {
        var s=Create();var a=Worker(s);var p=new CellPosition(0,20);
        for(var dy=-1;dy<=1;dy++)for(var dx=-1;dx<=1;dx++)s.World.SetTerrain(new(p.X+dx,p.Y+dy),TerrainKind.Rock);
        foreach(var x in new[]{63,0,1})s.World.SetTerrain(new(x,20),TerrainKind.Tunnel);
        s.Pheromones[a.ColonyId].Deposit(PheromoneType.FoodTrail,s.World.Index(new(63,20)),15);
        var c=Context(s,a);var left=0;var right=0;
        for(var i=0;i<200;i++)
        {a.Position=a.PreviousPosition=p;a.MovementCredit=1;a.Destination=null;Assert.True(c.Movement.Move(c,true));if(a.Position.X==63)left++;else if(a.Position.X==1)right++;else Assert.Fail("Atravessou sólido.");}
        Assert.True(left>right*2,$"left={left},right={right}");
    }
    [Fact] public void InjuredWorkerFleesHostileAgent()
    {
        var s=Create();var a=Worker(s);var enemy=Worker(s,2);enemy.Position=a.Position;s.Relations.Set(1,2,ColonyRelation.Hostile);a.Health=2;a.Energy=20;
        new DecisionSystem(new()).Decide(Context(s,a));Assert.Equal(AntTask.Flee,a.CurrentTask);
    }
    [Fact] public void ValidCollectDepositConservesResourcesAndInvalidActionsGiveNoExperience()
    {
        var s=Create();var a=Worker(s);s.World.SetResource(a.Position,ResourceIds.Food,new(5,5,10000));var before=s.Colonies[0].Food+5;
        Assert.True(new CollectAction().Execute(Context(s,a)));Assert.Equal(4,a.Carrying);
        var resourcePosition=a.Position;OrganicNestTests.WalkTo(Context(s,a),s.Colonies[0].FoodStorage);
        Assert.True(new DepositAction().Execute(Context(s,a)));Assert.Equal(before,s.Colonies[0].Food+s.World.GetResource(resourcePosition,ResourceIds.Food).Quantity);
        var queen=s.Colonies[0].Queen!;Assert.False(new CollectAction().Execute(Context(s,queen)));Assert.Equal(0,queen.Skills[(int)SkillType.ResourceDetection].Level);
    }
    [Fact] public void SkillsAreBoundedAndExperienceIsNotInherited()
    {
        var s=Create();var queen=s.Colonies[0].Queen!;queen.Learn(SkillType.Reproduction,1e7);Assert.Equal(20,queen.Skills[(int)SkillType.Reproduction].Level);
        Assert.True(new LayEggAction().Execute(Context(s,queen)));var egg=s.Ants.Last();Assert.Equal(LifeStage.Egg,egg.Stage);Assert.Equal(queen.Id,egg.MotherId);
        Assert.All(egg.Skills,skill=>Assert.Equal(0,skill.Experience));Assert.All(egg.Skills,skill=>Assert.Equal(0,skill.Level));
    }
    [Fact] public void BroodCanBeFedAndCarriedWithoutAutonomousMovement()
    {
        var s=Create();var a=Worker(s);var larva=s.Ants.First(x=>x.ColonyId==a.ColonyId&&x.Stage==LifeStage.Larva);larva.Nutrition=.1;a.TargetId=larva.Id;
        Assert.True(new FeedLarvaAction().Execute(Context(s,a)));Assert.True(larva.Nutrition>.1);
        Assert.True(new CarryAction().Execute(Context(s,a)));Assert.Equal(a.Id,larva.CarrierId);
        OrganicNestTests.WalkTo(Context(s,a),s.Colonies[0].BroodChamber);Assert.True(new CarryAction().Execute(Context(s,a)));Assert.Null(larva.CarrierId);Assert.Null(a.CarriedAntId);
    }
    [Fact] public void DiggingChangesChunksButPreservesFiniteDeposits()
    {
        var s=Create();var a=Worker(s);var p=new CellPosition(a.Position.X,a.Position.Y+1);s.World.SetTerrain(p,TerrainKind.Soil);a.Destination=p;
        var old=s.World.GetCell(p,false).SolidFill;var revision=s.World.TerrainRevision;
        Assert.True(new DigAction().Execute(Context(s,a)));Assert.True(s.World.GetCell(p,false).SolidFill<old);Assert.True(s.World.TerrainRevision>revision);
        s.World.SetTerrain(p,TerrainKind.Rock);Assert.True(new DigAction().Execute(Context(s,a)));
    }
    [Fact] public void DevelopmentUsesTimeNutritionAndTemperature()
    {
        var s=Create();s.Species[0]=new() {EggDays=1.0/SimulationState.TicksPerDay,LarvaDays=1.0/SimulationState.TicksPerDay,PupaDays=1.0/SimulationState.TicksPerDay};
        var egg=AntFactory.Create(s,s.Colonies[0],stage:LifeStage.Egg);var biology=new AntBiologySystem();
        for(var i=0;i<20;i++)biology.Step(s,egg);Assert.Equal(LifeStage.Adult,egg.Stage);
        var starving=AntFactory.Create(s,s.Colonies[0],stage:LifeStage.Larva);starving.Nutrition=0;biology.Step(s,starving);Assert.Equal(0,starving.DevelopmentTicks);
    }
    [Fact] public void PlaybackSpeedsScaleTimeAndPauseFreezesEverything()
    {
        var playback=new PlaybackControl();Assert.Equal(1,playback.Clock.Speed);
        var counts=new List<int>();foreach(var speed in new[]{1,50,100}) {playback.Select(speed);var count=0;playback.Clock.Advance(1,()=>count++,3000);counts.Add(count);}
        Assert.Equal(new[]{20,1000,2000},counts);
        playback.TogglePause();playback.Select(50);Assert.True(playback.IsPaused);var ticks=0;playback.Clock.Advance(10,()=>ticks++);Assert.Equal(0,ticks);
        playback.TogglePause();Assert.Equal(50,playback.Clock.Speed);
    }
    [Fact] public void CollectivePrioritiesReactToFoodAndBrood()
    {
        var s=Create();var index=new AgentSpatialIndex();index.Rebuild(s);var system=new ColonyAiSystem();var c=s.Colonies[0];
        c.Food=10000;system.Step(s,index);var before=c.Priorities.FoodCollection;c.Food=0;system.Step(s,index);
        Assert.True(c.Priorities.FoodCollection>before);Assert.True(c.Priorities.BroodCare>0);
    }
    [Fact] public void EmergenciesCanBypassNormalDecisionInterval()
    {
        var s=Create();var a=Worker(s);a.Health=1;a.LastDecisionTick=5;s.Tick=6;
        var seen=new HashSet<int>();new AgentScheduler().Decide(s,ant=>seen.Add(ant.Id));Assert.Contains(a.Id,seen);
    }
    [Fact] public void PopulationLimitRejectsBirthWithoutKillingOrSpendingFood()
    {
        var s=Create();s.Performance=s.Performance with {MaximumPopulation=s.Ants.Count};var queen=s.Colonies[0].Queen!;
        var food=s.Colonies[0].Food;var count=s.Ants.Count;
        Assert.False(new LayEggAction().Execute(Context(s,queen)));Assert.Equal(food,s.Colonies[0].Food);Assert.Equal(count,s.Ants.Count);
    }
    [Fact] public void ZeroMaximumHealthDiesWithoutInvalidPhysiology()
    {
        var s=Create();var a=Worker(s);a.ChangeMaximumHealth(0);new SimulationEngine(s).Step();Assert.DoesNotContain(a,s.Ants);
    }
    [Fact] public void CarriedLarvaIsReleasedWhenItBecomesPupa()
    {
        var s=Create();s.Species[0]=new(){LarvaDays=1.0/SimulationState.TicksPerDay};var a=Worker(s);var larva=s.Ants.First(x=>x.ColonyId==1&&x.Stage==LifeStage.Larva);
        a.TargetId=larva.Id;Assert.True(new CarryAction().Execute(Context(s,a)));new SimulationEngine(s).Run(5);
        Assert.Null(larva.CarrierId);Assert.Null(a.CarriedAntId);Assert.Equal(LifeStage.Pupa,larva.Stage);
    }
    [Fact] public void UnknownMarkedTerritoryCanEscalateButNeutralRelationPreventsIt()
    {
        var s=Create();var defender=s.Ants.First(a=>a.ColonyId==1&&a.Caste==AntCaste.Soldier);var foreign=Worker(s,2);foreign.Position=defender.Position;
        s.Pheromones[1].Deposit(PheromoneType.Territory,s.World.Index(foreign.Position),3);var recognition=new RecognitionService();
        Assert.False(recognition.Recognize(defender,foreign,s).PossibleThreat);
        s.Relations.Set(1,2,ColonyRelation.Unknown);Assert.True(recognition.Recognize(defender,foreign,s).PossibleThreat);
        Assert.True(new CombatSystem().Attack(s,defender,foreign));Assert.Equal(ColonyRelation.Hostile,s.Relations.Get(1,2));
    }
    [Fact] public void RecognitionDoesNotAllocatePerInteraction()
    {
        var s=Create();var a=Worker(s);var b=Worker(s,2);var service=new RecognitionService();s.Relations.Set(1,2,ColonyRelation.Unknown);
        for(var i=0;i<100;i++)service.Recognize(a,b,s);
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<10000;i++)service.Recognize(a,b,s);
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
    }
    [Fact] public void HysteresisPreservesValidTaskUntilEmergencyDominates()
    {
        var s=Create();s.World.ClearFood(true);var a=Worker(s);var colony=s.Colonies[0];colony.Priorities=new(.1,0,0,0,.1);
        foreach(var entry in s.World.Resources().Where(e=>ResourceIds.IsEdible(e.ResourceId)).ToArray())s.World.SetResource(entry.Position,entry.ResourceId,default);
        foreach(var larva in s.Ants.Where(ant=>ant.Stage==LifeStage.Larva)) {larva.Energy=larva.MaxEnergy;larva.Position=s.Colonies.Single(c=>c.Id==larva.ColonyId).BroodChamber;}
        a.WorkerRole=WorkerRole.Collector; a.Health=a.MaxHealth*.8;a.Energy=100;var decision=new DecisionSystem(new());var context=Context(s,a);
        a.CurrentTask=AntTask.RecoverHealth;decision.Decide(context);Assert.Equal(AntTask.RecoverHealth,a.CurrentTask);
        a.CurrentTask=AntTask.Explore;decision.Decide(context);Assert.Equal(AntTask.Explore,a.CurrentTask);
        a.Energy=1;decision.Decide(context);Assert.Equal(AntTask.Feed,a.CurrentTask);
    }
}
