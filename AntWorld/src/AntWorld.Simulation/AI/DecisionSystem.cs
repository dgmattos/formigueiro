using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class DecisionSystem(ActionCatalog actions)
{
    private readonly RecognitionService recognition=new();
    private readonly WaterSearch waterSearch = new();
    private readonly Ant[] nearby=new Ant[48];
    public void Decide(AntContext context)
    {
        var ant=context.Ant; var state=context.State; var world=state.World; var colony=context.Colony;
        var profile=state.Species[ant.SpeciesId].Behaviors[(int)ant.Caste];
        var priorities=colony.Priorities;
        var best=AntTask.Rest; var score=-1.0; int? target=null; CellPosition? destination=null;
        void Consider(AntTask task,double utility,int? candidateTarget=null,CellPosition? candidateDestination=null,bool emergency=false)
        {
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
        var flood=world.GetCell(ant.Position,false).WaterAmount>=500;
        Consider(AntTask.Rest,.05+(1-ant.Energy/100)*(ant.Energy<30 && ant.Nutrition>.1 ? 3 : .3));
        Consider(AntTask.Feed,Math.Max(Math.Max(1-ant.Energy/100,1-ant.Nutrition),1-ant.Hydration)*(ant.Energy<40 || ant.Nutrition<.5 || ant.Hydration<.4 ? 4 : 3));
        Consider(AntTask.Drink,(1-ant.Hydration)*4);
        Consider(AntTask.RecoverHealth,(1-ant.Health/ant.MaxHealth)*1.5);
        Consider(AntTask.LayEgg,profile.Reproduction*.8);
        Consider(AntTask.Deposit,ant.Carrying>0?2:0);
        if(ant.Carrying>0) Consider(AntTask.Carry,1.8,null,colony.Nest);
        if(ant.CarriedAntId is {} carriedId) Consider(AntTask.Carry,2.8*profile.BroodCare,carriedId,colony.BroodChamber);
        if (ant.Hydration < .5 && !WaterSources.TryAccessible(world,ant.Position,out _) && waterSearch.NextStep(world,ant.Position) is {} waterStep)
            Consider(AntTask.Move,1.4+(1-ant.Hydration)*2,null,waterStep);
        if(colony.Food>0 && ant.Position!=colony.Nest && (ant.Energy<40 || ant.Nutrition<.5 || ant.Hydration<.4))
            Consider(AntTask.Move,1.2+(1-ant.Energy/100),null,colony.Nest);
        if(flood) Consider(AntTask.Flee,10,null,FindDry(world,ant.Position),true);
        Consider(AntTask.Collect,(.7+priorities.FoodCollection)*profile.Foraging);
        if(ant.Carrying==0 && ant.CarriedAntId is null)
        {
            var food=FindFood(world,ant.Position);
            Consider(AntTask.Explore,(.25+priorities.Exploration+priorities.FoodCollection*.3)*profile.Exploration,null,food);
        }
        // Continue a valid excavation instead of switching cells at every evaluation.
        if(ant.CurrentTask==AntTask.Dig && ant.CarriedAntId is null) Consider(AntTask.Dig,.25*profile.Excavation+priorities.Excavation,null,ant.Destination);
        Span<CellPosition> cells=stackalloc CellPosition[4];
        var cellCount=world.ChunkManager.GetNeighbors(ant.Position,cells);
        for(var i=0;i<cellCount;i++)
        {
            if(world.Distance(ant.Position,colony.Nest)<8 && ant.CarriedAntId is null)
                Consider(AntTask.Dig,(.02+priorities.Excavation*.4)*profile.Excavation*(.2+(ant.Id%11)/10.0),null,cells[i]);
            if(ant.CarriedAntId is null) Consider(AntTask.Build,.3*profile.Excavation,null,cells[i]);
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
            var territory=world.Distance(other.Position,colony.Nest)<8?1: .3;
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
