using AntWorld.Domain;
namespace AntWorld.Simulation;

public sealed class QueenEscortSystem
{
    public const int ReturnRadius = 10;
    public const int MaximumRadius = 15;

    public void Step(SimulationState state)
    {
        foreach(var colony in state.Colonies)
        {
            var members=state.Ants.Where(a=>a.ColonyId==colony.Id).OrderByDescending(a=>a.QueenEscort).ThenBy(a=>a.Id);
            var soldiers=0;var workers=0;
            foreach(var ant in members)
            {
                var eligible=colony.Queen is {IsAlive:true} && ant.IsAlive && ant.Stage==LifeStage.Adult;
                var assigned=eligible && (ant.Caste==AntCaste.Soldier && soldiers++<2 || ant.Caste==AntCaste.Worker && workers++<1);
                if(assigned!=ant.QueenEscort){ant.LastDecisionTick=-1;ant.EscortReturning=false;}
                ant.QueenEscort=assigned;
            }
        }
    }

    public static bool Allows(AntContext context,CellPosition position)
    {
        if(!context.Ant.QueenEscort || context.Colony.Queen is not {IsAlive:true} queen)return true;
        return context.State.World.Distance(position,queen.Position)<=MaximumRadius;
    }

    public static bool MustReturn(AntContext context)
    {
        var ant=context.Ant;
        if(!ant.QueenEscort || context.Colony.Queen is not {IsAlive:true} queen)return ant.EscortReturning=false;
        var distance=context.State.World.Distance(ant.Position,queen.Position);
        if(distance>ReturnRadius)ant.EscortReturning=true;
        else if(distance<=ReturnRadius/2)ant.EscortReturning=false;
        return ant.EscortReturning;
    }
}
