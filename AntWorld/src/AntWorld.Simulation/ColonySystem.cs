using AntWorld.Domain;
namespace AntWorld.Simulation;
public sealed class ColonySystem
{
    public const int MaximumAnts=20000;
    public const int MaximumColonies=12;
    public void Step(SimulationState state)
    {
        // Queen physiology is handled once, as an individual. No duplicate queen health/consumption.
        foreach(var colony in state.Colonies.ToArray())
        {
            if(colony.Queen is not {IsAlive:true} queen) continue;
            queen.Signature=queen.Signature.Assimilate(colony.Signature,(int)(state.Tick%64));
            if(state.Tick%1200==0) TryFound(state,colony);
        }
    }
    private static void TryFound(SimulationState state,Colony parent)
    {
        if(parent.Food<150 || state.Colonies.Count>=MaximumColonies) return;
        var queen=state.Ants.FirstOrDefault(a=>a.ColonyId==parent.Id&&a.Id!=parent.QueenId&&a.Caste==AntCaste.Queen&&a.Stage==LifeStage.Adult&&a.IsAlive);
        if(queen is null) return;
        var workers=state.Ants.Where(a=>a.ColonyId==parent.Id&&a.Caste==AntCaste.Worker&&a.Stage==LifeStage.Adult&&a.CarriedAntId is null).Take(8).ToArray();
        if(workers.Length<8) return;
        var x=state.World.ChunkManager.NormalizeX(parent.Nest.X+20+state.Random.NextInt(35));
        if(state.Colonies.Any(c=>c.IsAlive&&Math.Abs(state.World.HorizontalDelta(c.Nest.X,x))<18)) return;
        NestLayout layout;
        try
        {
            layout=NestFoundation.Create(state.World,state.NextColonyId,x,parent.Species,
                state.Colonies.Where(c=>c.Layout is not null).Select(c=>c.Layout!).ToArray(),
                workers.Select(a=>a.Position).Append(queen.Position).ToArray());
        }
        catch(InvalidOperationException) { return; } // No terrain or resources committed on an inaccessible foundation.
        var nest=layout.Designated(layout.QueenChamberId)!.Access;
        var daughter=new Colony(state.NextColonyId++,parent.Species,nest,queen.Genes,parent.Generation+1)
        { Layout=layout,ShaftX=x,Food=70,Queen=queen,QueenId=queen.Id,Signature=parent.Signature.Individual(queen.Id) };
        parent.Food-=70; state.Colonies.Add(daughter); state.Pheromones[daughter.Id]=new(state.World.CellCount) { LastUpdateTick=state.Tick };
        queen.JoinColony(daughter); foreach(var worker in workers) worker.JoinColony(daughter);
        state.Relations.Set(parent.Id,daughter.Id,ColonyRelation.Tolerated);
    }
}
