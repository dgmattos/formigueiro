using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class DecisionSystem(ActionCatalog actions)
{
    private readonly RecognitionService recognition=new();
    private readonly WaterSearch waterSearch = new();
    private readonly ForagingSystem foraging = new();
    private readonly NestPatrol patrol=new();
    private readonly NurseryCareTargets nurseryTargets=new();
    private readonly Ant[] nearby=new Ant[48];
    public void Decide(AntContext context)
    {
        var ant=context.Ant; var state=context.State; var world=state.World; var colony=context.Colony;
        var profile=state.Species[ant.SpeciesId].Behaviors[(int)ant.Caste];
        var priorities=colony.Priorities;
        var digger = ant.WorkerRole == WorkerRole.Digger;
        var collector = ant.Caste == AntCaste.Worker && ant.WorkerRole != WorkerRole.Digger;
        var escortReturning=QueenEscortSystem.MustReturn(context);
        var spoil = ant.Carrying > 0 && ExcavationSpoil.Is(world, ant.CargoResourceId);
        var best=AntTask.Rest; var score=-1.0; int? target=null; CellPosition? destination=null;
        void Consider(AntTask task,double utility,int? candidateTarget=null,CellPosition? candidateDestination=null,bool emergency=false)
        {
            if(candidateDestination is {} requested && !QueenEscortSystem.Allows(context,requested))return;
            var oldTarget=ant.TargetId; var oldDestination=ant.Destination;
            ant.TargetId=candidateTarget; ant.Destination=candidateDestination;
            var valid=actions.Get(task).CanExecute(context);
            ant.TargetId=oldTarget; ant.Destination=oldDestination;
            if(!valid || utility<=0) return;
            // Stable individual variation and a small continuation bonus prevent synchronized task thrashing.
            utility*=.95+(ant.Id%17)/170.0;
            if(task==ant.CurrentTask && !emergency) utility+=.12;
            if(utility<=score) return;
            best=task; score=utility; target=candidateTarget; destination=candidateDestination;
        }
        if(escortReturning && colony.Queen is {} escortQueen)
            Consider(AntTask.Move,8,null,escortQueen.Position,true);
        if((ant.Caste==AntCaste.Soldier || ant.QueenEscort) && ant.Carrying==0 && ant.CarriedAntId is null && patrol.Destination(context) is {} patrolPoint)
            Consider(AntTask.Move,.9,null,patrolPoint);
        var flood=world.GetCell(ant.Position,false).WaterAmount>=500;
        Consider(AntTask.Rest,.05+(1-ant.Energy/100)*(ant.Energy<30 && ant.Nutrition>.1 ? 3 : .3));
        Consider(AntTask.Feed,Math.Max(Math.Max(1-ant.Energy/100,1-ant.Nutrition),1-ant.Hydration)*(ant.Energy<40 || ant.Nutrition<.5 || ant.Hydration<.4 ? 4 : 3));
        Consider(AntTask.Drink,(1-ant.Hydration)*4);
        Consider(AntTask.RecoverHealth,(1-ant.Health/ant.MaxHealth)*1.5);
        Consider(AntTask.LayEgg,profile.Reproduction*.8);
        Consider(AntTask.Deposit,ant.Carrying>0 ? (spoil ? 3.2 : 3.4) : 0);
        if(ant.Carrying>0) Consider(AntTask.Carry,spoil ? 3 : 3.2,null,spoil ? NestDestinations.Exit(context) : colony.FoodStorage);
        if (ant.Id == colony.QueenId && ant.Position != colony.Nest)
            Consider(AntTask.Move,3,null,colony.Nest);
        if(ant.CarriedAntId is {} carriedId) Consider(AntTask.Carry,2.8*profile.BroodCare,carriedId,colony.BroodChamber);
        if (ant.Hydration < .5 && !WaterSources.TryAccessible(world,ant.Position,out _) && waterSearch.NextStep(world,ant.Position) is {} waterStep)
            Consider(AntTask.Move,1.4+(1-ant.Hydration)*2,null,waterStep);
        if(colony.Food>0 && ant.Position!=colony.Nest && (ant.Energy<40 || ant.Nutrition<.5 || ant.Hydration<.4))
            Consider(AntTask.Move,1.2+(1-ant.Energy/100),null,colony.Nest);
        if(flood) Consider(AntTask.Flee,10,null,FindDry(world,ant.Position),true);
        if (collector && ant.Carrying == 0 && QueenEscortSystem.Allows(context,colony.FoodStorage)) Consider(AntTask.Collect,(.7+priorities.FoodCollection)*profile.Foraging*ForagingSystem.HarvestQuality(context));
        if(collector && ant.Carrying==0 && ant.CarriedAntId is null)
        {
            var food=ant.QueenEscort ? FindFood(world,ant.Position) : foraging.Destination(context);
            if (food.HasValue && food != ant.Position || ant.Caste != AntCaste.Worker)
                Consider(AntTask.Explore,(.25+priorities.Exploration+priorities.FoodCollection*.3)*profile.Exploration,null,food);
        }
        if (digger && ant.Carrying == 0 && ant.CarriedAntId is null)
        {
            if (ColonyExpansionSystem.FindJob(context, out var digTarget, out var access))
            {
                var utility = 1.3 + priorities.Excavation * profile.Excavation;
                Consider(AntTask.Dig, utility, null, digTarget);
                if (ant.Position != access) Consider(AntTask.Move, utility, null, access);
            }
            if (DroppedSpoil.Find(world, ant.Position, out var dropped))
            {
                if (ant.Position == dropped) Consider(AntTask.Collect, 2.4);
                else Consider(AntTask.Move, 2.4, null, dropped);
            }
        }
        if(ant.Caste==AntCaste.Worker && ant.Carrying==0 && ant.CarriedAntId is null)
        {
            var displaced=nurseryTargets.Nearest(context);
            if(displaced is not null)
            {
                Consider(AntTask.Carry,2.6,displaced.Id,colony.BroodChamber);
                if(world.Distance(ant.Position,displaced.Position)>1)Consider(AntTask.Move,2.6,displaced.Id,displaced.Position);
            }
        }
        var count=context.Index.Nearby(world,ant.Position,nearby,4,ant.Id+(int)(state.Tick%10000));
        for(var i=0;i<count;i++)
        {
            var other=nearby[i]; if(other.Id==ant.Id || !other.IsAlive) continue;
            var relation=recognition.Recognize(ant,other,state);
            var distance=world.Distance(ant.Position,other.Position);
            if(relation.KnownMembership && other.Stage==LifeStage.Larva)
            {
                if(ant.CarriedAntId is null)
                {
                    var care=Math.Max(Math.Max(1-other.Nutrition,1-other.Energy/100),1-other.Hydration)*4*profile.BroodCare;
                    Consider(AntTask.FeedLarva,care,other.Id);
                    if(distance>1 && other.CarrierId is null && (ant.Carrying>0 || colony.Food>0 && other.Position==colony.BroodChamber))
                        Consider(AntTask.Move,care/(1+distance*.1),other.Id,other.Position);
                }
            }
            if(relation.KnownMembership && ant.Carrying==0 && ant.CarriedAntId is null && profile.BroodCare>0 && BroodTransport.NeedsRelocation(world,colony,other))
            {
                var care=(2.4+priorities.BroodCare*.2)*profile.BroodCare;
                Consider(AntTask.Carry,care,other.Id,colony.BroodChamber);
                if(distance>1 && AntCapabilities.Has(state,ant,AntCapability.BroodCare) && context.Movement.CanReachBroodChamber(context))
                    Consider(AntTask.Move,care/(1+distance*.1),other.Id,other.Position);
            }
            if(!relation.PossibleThreat) continue;
            var territory=world.Distance(other.Position,colony.Nest)<8 || colony.Layout?.Entrances.Any(e=>e.Safe&&world.Distance(other.Position,e.Position)<8)==true ? 1 : .3;
            var danger=(1-ant.Health/ant.MaxHealth)+(1-ant.Energy/100);
            state.Pheromones[ant.ColonyId].Deposit(PheromoneType.Alarm,world.Index(ant.Position),.2);
            Consider(AntTask.Flee,1+danger*3,other.Id,null,true);
            var aggression=(.2+ant.Genes.Aggression)*profile.Defense*(1+territory+priorities.Defense);
            Consider(AntTask.Attack,aggression/(1+danger),other.Id);
            Consider(AntTask.Defend,aggression/(1+danger),other.Id);
            if(distance>1) Consider(AntTask.Move,aggression*.8/(1+danger),other.Id,other.Position);
        }
        ant.CurrentTask=best; ant.TargetId=target; ant.Destination=destination; ant.CurrentUtility=Math.Max(0,score);
    }
    private static CellPosition? FindFood(WorldMap world,CellPosition origin)
    {
        CellPosition? best=null; var score=0.0;
        for(var dy=-4;dy<=4;dy++) for(var dx=-4;dx<=4;dx++)
        {
            var p=new CellPosition(origin.X+dx,origin.Y+dy); if(!world.Contains(p)) continue;
            var amount=FoodSources.AmountAt(world,p)/(1+Math.Abs(dx)+Math.Abs(dy));
            if(amount>score && world.CanWalk(p)) { score=amount; best=world.Normalize(p); }
        }
        return best;
    }
    private static CellPosition? FindDry(WorldMap world,CellPosition origin)
    {
        Span<CellPosition> cells=stackalloc CellPosition[8]; var count=world.GetWalkableNeighbors(origin,cells);
        for(var i=0;i<count;i++) if(world.GetCell(cells[i],false).WaterAmount<200) return cells[i];
        return null;
    }
}
