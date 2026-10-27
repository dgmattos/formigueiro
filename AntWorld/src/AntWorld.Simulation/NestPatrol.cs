using AntWorld.Domain;
namespace AntWorld.Simulation;

// Routes are shared by colony; only the cursor belongs to each individual.
public sealed class NestPatrol
{
    private readonly Dictionary<int,(NestLayout Layout,int Revision,CellPosition[] Points)> routes=[];
    public CellPosition? Destination(AntContext context)
    {
        var ant=context.Ant;var world=context.State.World;var layout=context.Colony.Layout;
        if(layout is null)return context.Colony.Nest;
        if(!routes.TryGetValue(context.Colony.Id,out var cached) || cached.Layout!=layout || cached.Revision!=layout.Revision)
        {
            var points=layout.Chambers.Where(c=>c.Status==NestStructureStatus.Completed).SelectMany(c=>c.Cells.Prepend(c.Access))
                .Concat(layout.Tunnels.Where(t=>t.Status==NestStructureStatus.Completed).SelectMany(t=>t.Path))
                .Concat(layout.Entrances.Where(e=>e.Status==NestStructureStatus.Completed&&e.Safe).SelectMany(e=>e.Path.Append(e.Position)))
                .Distinct().ToArray();
            cached=(layout,layout.Revision,points);routes[context.Colony.Id]=cached;
        }
        var route=cached.Points;
        for(var attempt=0;attempt<route.Length;attempt++)
        {
            var index=(int)(((long)ant.PatrolCursor+ant.Id)%route.Length);var point=route[index];
            if(point!=ant.Position && world.CanWalk(point) && world.GetCell(point,false).WaterAmount<200 &&
                QueenEscortSystem.Allows(context,point) &&
                (!ant.QueenEscort || context.Colony.Queen is not {} queen || world.Distance(point,queen.Position)<=QueenEscortSystem.ReturnRadius) &&
                context.Movement.CanReach(context,point))return point;
            ant.PatrolCursor=ant.PatrolCursor==int.MaxValue?0:ant.PatrolCursor+1;
        }
        return null;
    }
}
