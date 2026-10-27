using AntWorld.Domain;
namespace AntWorld.Simulation;

// Maximize distance to the nearest usable exit, not depth or distance to just one entrance.
public sealed class QueenChamberSelection
{
    private readonly NestNavigation navigation=new();

    public void Step(SimulationState state,Colony colony)
    {
        if(colony.Layout is not {} layout || colony.Queen is not {IsAlive:true} queen)return;
        var world=state.World;
        var exits=new List<IReadOnlyDictionary<int,int>>(layout.Entrances.Count);
        foreach(var entry in layout.Entrances)
        {
            if(entry.Status!=NestStructureStatus.Completed || !entry.Safe || !world.CanWalk(entry.Position) ||
                !world.IsOnExposedSurface(entry.Position) || world.GetCell(entry.Position,false).WaterAmount>=200)continue;
            exits.Add(navigation.Distances(world,entry.Position));
        }
        if(exits.Count==0)return;
        var reachable=navigation.Distances(world,queen.Position);
        var selected=layout.QueenChamberId;var best=-1;
        foreach(var room in layout.Chambers)
        {
            if(room.Status!=NestStructureStatus.Completed || world.GetCell(room.Access,false).WaterAmount>=200)continue;
            var index=world.Index(room.Access);
            if(!reachable.ContainsKey(index))continue;
            var nearest=int.MaxValue;
            foreach(var distances in exits)
                if(distances.TryGetValue(index,out var distance))nearest=Math.Min(nearest,distance);
            if(nearest==int.MaxValue)continue;
            var preferredTie=room.Id==layout.QueenChamberId || selected!=layout.QueenChamberId&&room.Id<selected;
            if(nearest>best || nearest==best&&preferredTie){selected=room.Id;best=nearest;}
        }
        if(selected==layout.QueenChamberId)return;
        layout.QueenChamberId=selected;
        layout.Touch();
        // DecisionSystem moves the queen along an actual supported route. Never relocate her here.
    }
}
