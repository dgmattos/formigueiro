using AntWorld.Domain;
namespace AntWorld.Simulation;
// Executes the selected action; utility selection and physiology are separate systems.
public sealed class AntBehaviorSystem(ActionCatalog actions)
{
    public bool Step(AntContext context)
    {
        var ant=context.Ant; var state=context.State;
        if(ant.Stage!=LifeStage.Adult || !ant.IsAlive || ant.CarrierId.HasValue) return false;
        var moving=ant.CurrentTask is AntTask.Move or AntTask.Explore or AntTask.Carry or AntTask.Flee;
        if(moving)
        {
            if(state.Tick%(SimulationState.TicksPerSecond/state.Performance.MovementTicksPerSecond)!=0) return false;
        }
        else if((state.Tick+ant.Id)%(ant.CurrentTask == AntTask.Dig ? ExcavationRules.ActionIntervalTicks : 10)!=0) return false;
        return actions.Get(ant.CurrentTask).Execute(context);
    }
}
