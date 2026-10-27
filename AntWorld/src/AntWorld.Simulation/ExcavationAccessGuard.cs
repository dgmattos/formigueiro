using AntWorld.Domain;
using AntWorld.WorldGeneration;
namespace AntWorld.Simulation;

// Removing support can shrink the walking graph even though it enlarges the air volume.
public sealed class ExcavationAccessGuard
{
    private readonly NestNavigation navigation=new();
    public bool CanRemove(SimulationState state,Colony colony,CellPosition target)
    {
        if(colony.Layout is null)return true;
        var world=state.World;var projection=new NestPhysicalProjection(world,[target]);
        Span<CellPosition> neighbors=stackalloc CellPosition[4];var count=world.ChunkManager.GetNeighbors(target,neighbors);var losesSupport=false;
        for(var i=0;i<count;i++)if(world.CanWalk(neighbors[i])&&!projection.Walkable(neighbors[i])){losesSupport=true;break;}
        if(!losesSupport)return true;
        var before=navigation.Distances(world,colony.Nest);var after=projection.Reachable(colony.Nest);
        foreach(var room in colony.Layout.Chambers)
            if(room.Status==NestStructureStatus.Completed&&before.ContainsKey(world.Index(room.Access))&&!after.Contains(room.Access))return false;
        foreach(var ant in state.Ants)
        {
            if(ant.ColonyId!=colony.Id||!ant.IsAlive||ant.CarrierId.HasValue||!before.ContainsKey(world.Index(ant.Position)))continue;
            var position=ant.Position;
            for(var fall=0;fall<world.Height&&!projection.Walkable(position);fall++)
            {
                var below=new CellPosition(position.X,position.Y+1);if(!projection.Open(below))break;position=world.Normalize(below);
            }
            if(!after.Contains(position))return false;
        }
        return true;
    }
}
