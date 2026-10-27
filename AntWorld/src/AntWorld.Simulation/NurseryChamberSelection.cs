using AntWorld.Domain;
namespace AntWorld.Simulation;

public sealed class NurseryChamberSelection
{
    private readonly NestNavigation navigation=new();
    public void Step(SimulationState state,Colony colony)
    {
        if(colony.Layout is not {} layout || colony.Queen is not {IsAlive:true} queen)return;
        var distances=navigation.Distances(state.World,queen.Position);
        var selected=layout.BroodChamberId;var nearest=int.MaxValue;
        foreach(var room in layout.Chambers)
        {
            if(room.Status!=NestStructureStatus.Completed || state.World.GetCell(room.Access,false).WaterAmount>=200 ||
                !distances.TryGetValue(state.World.Index(room.Access),out var distance))continue;
            if(distance<nearest || distance==nearest && room.Id==layout.BroodChamberId){selected=room.Id;nearest=distance;}
        }
        if(selected!=layout.BroodChamberId){layout.BroodChamberId=selected;layout.Touch();}
    }
}
