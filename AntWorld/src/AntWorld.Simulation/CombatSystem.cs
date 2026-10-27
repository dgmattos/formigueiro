using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class CombatSystem
{
    private readonly RecognitionService recognition=new();
    private readonly AgentSpatialIndex index=new();
    private readonly Ant[] nearby=new Ant[32];
    public bool CanAttack(SimulationState state,Ant attacker,Ant? target) => target is {IsAlive:true} && attacker.IsAlive && attacker.Energy>=.35 &&
        (AntCapabilities.Has(state,attacker,AntCapability.Attack)||AntCapabilities.Has(state,attacker,AntCapability.Defend)) &&
        state.Tick-attacker.LastAttackTick>=20 && state.World.Distance(attacker.Position,target.Position)<=1 && recognition.Recognize(attacker,target,state).PossibleThreat;
    public bool Attack(SimulationState state,Ant attacker,Ant? target)
    {
        if(!CanAttack(state,attacker,target)) return false;
        var offense=AntCapabilities.Definition(state,attacker).Attack*attacker.Genes.Strength*attacker.Efficiency(SkillType.Combat)*(.25+.75*attacker.Health/attacker.MaxHealth);
        var defense=target!.Stage==LifeStage.Adult ? AntCapabilities.Definition(state,target).Defense*target.Efficiency(SkillType.Defense)*(target.CurrentTask==AntTask.Defend?1.5:1):0;
        var damage=Math.Max(.1,offense*100/(100+defense))*(1-state.RainIntensity*.1);
        if(state.Relations.Get(attacker.ColonyId,target.ColonyId)==ColonyRelation.Unknown)
            state.Relations.Set(attacker.ColonyId,target.ColonyId,ColonyRelation.Hostile);
        target.Damage(damage); attacker.Energy-=.35; attacker.LastAttackTick=state.Tick; attacker.Movement=MovementState.Attacking;
        state.Pheromones[target.ColonyId].Deposit(PheromoneType.Alarm,state.World.Index(target.Position),3);
        return true;
    }
    public void Step(SimulationState state)
    {
        index.Rebuild(state);
        foreach(var ant in state.Ants)
        {
            var n=index.Nearby(state.World,ant.Position,nearby,1,ant.Id);
            for(var i=0;i<n;i++) if(Attack(state,ant,nearby[i])) break;
        }
    }
}
