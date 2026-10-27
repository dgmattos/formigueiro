using AntWorld.Domain;
namespace AntWorld.Simulation;

public static class NestDestinations
{
    public static CellPosition Exit(AntContext context)
    {
        var fallback=ExcavationSpoil.Exit(context.State.World,context.Colony);
        if(context.Colony.Layout is not {} layout)return fallback;
        var distances=context.Movement.DistancesFrom(context.State.World,context.Ant.Position);
        var best=int.MaxValue;var result=fallback;
        foreach(var entrance in layout.Entrances)
        {
            if(!entrance.Safe||entrance.Status!=NestStructureStatus.Completed||!context.State.World.IsOnExposedSurface(entrance.Position))continue;
            if(distances.TryGetValue(context.State.World.Index(entrance.Position),out var distance)&&distance<best){best=distance;result=entrance.Position;}
        }
        return result;
    }
    public static bool IsExit(WorldMap world,Colony colony,CellPosition position)=>
        colony.Layout is {} layout?layout.Entrances.Any(e=>e.Safe&&e.Status==NestStructureStatus.Completed&&e.Position==position&&world.IsOnExposedSurface(position)):
        position==ExcavationSpoil.Exit(world,colony);
}
