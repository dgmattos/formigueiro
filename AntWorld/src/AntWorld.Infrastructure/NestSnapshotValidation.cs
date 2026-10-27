using AntWorld.Domain;
using AntWorld.WorldGeneration;
namespace AntWorld.Infrastructure;

internal static class NestSnapshotValidation
{
    public static void Validate(SimulationState state,Colony colony)
    {
        if(colony.Layout is not {} layout)return;
        var world=state.World;var jobs=layout.Construction.Jobs;
        Require(layout.ColonyId==colony.Id&&layout.Chambers.Count is >0 and <=60&&layout.Tunnels.Count<=256&&layout.Entrances.Count<=5,"Identidade ou tamanho do layout inválido.");
        Require(layout.Chambers.Select(c=>c.Id).Distinct().Count()==layout.Chambers.Count&&layout.Tunnels.Select(t=>t.Id).Distinct().Count()==layout.Tunnels.Count,"IDs de estruturas duplicados.");
        Require(layout.Designated(layout.QueenChamberId) is not null&&layout.Designated(layout.BroodChamberId) is not null&&layout.Designated(layout.FoodStorageChamberId) is not null,"Destinos funcionais incompletos.");
        foreach(var room in layout.Chambers)
        {
            Require(Enum.IsDefined(room.Type)&&Enum.IsDefined(room.Status)&&room.Id>0&&room.Capacity>0&&room.Cells.Count is >0 and <=256,"Câmara inválida.");
            Require(world.IsCanonical(room.Access)&&world.IsCanonical(room.Center)&&room.Cells.All(world.IsCanonical)&&room.Cells.Contains(room.Access),"Geometria de câmara inválida.");
            Require(room.Status!=NestStructureStatus.Completed||room.Cells.All(world.IsOpen),"Câmara completa contém blocos sólidos.");
        }
        foreach(var tunnel in layout.Tunnels)
        {
            Require(Enum.IsDefined(tunnel.Status)&&tunnel.Width is 1 or 2&&double.IsFinite(tunnel.EstimatedCost)&&tunnel.EstimatedCost>=0,"Túnel inválido.");
            Require(layout.Chambers.Any(c=>c.Id==tunnel.FromChamberId)&&layout.Chambers.Any(c=>c.Id==tunnel.ToChamberId)&&tunnel.FromChamberId!=tunnel.ToChamberId,"Extremos de túnel inválidos.");
            Require(tunnel.Path.Count is >0 and <=8192&&tunnel.Path.All(world.IsCanonical)&&tunnel.Cells.All(world.IsCanonical),"Percurso inválido.");
            Require(tunnel.Status!=NestStructureStatus.Completed||tunnel.Cells.All(world.IsOpen),"Túnel completo contém blocos sólidos.");
        }
        foreach(var entrance in layout.Entrances)
            Require(world.IsCanonical(entrance.Position)&&entrance.Path.All(world.IsCanonical)&&Enum.IsDefined(entrance.Status),"Entrada inválida.");
        Require(jobs.Count<=65536&&jobs.Select(j=>j.Id).Distinct().Count()==jobs.Count&&jobs.Select(j=>j.Target).Distinct().Count()==jobs.Count,"Trabalhos duplicados.");
        Require(jobs.Where(j=>j.AssignedAntId.HasValue).Select(j=>j.AssignedAntId).Distinct().Count()==jobs.Count(j=>j.AssignedAntId.HasValue),"Operária atribuída a vários trabalhos.");
        Require(jobs.Count(j=>j.Status is NestStructureStatus.Available or NestStructureStatus.InProgress)<=world.Configuration.NestLayout.MaximumConcurrentExcavationJobs,"Frentes excedem limite.");
        foreach(var job in jobs)
            Require(job.Id>0&&world.IsCanonical(job.Target)&&(job.Support is null||world.IsCanonical(job.Support.Value))&&job.WorkActions>=0&&job.LastProgressTick>=0&&job.LastProgressTick<=state.Tick&&Enum.IsDefined(job.Status)&&
                (job.AssignedAntId is null||state.Ants.Any(a=>a.Id==job.AssignedAntId&&a.ColonyId==colony.Id)),"Trabalho inválido.");
        var reachable=new NestPhysicalProjection(world,[]).Reachable(colony.Nest);
        Require(layout.Chambers.Where(c=>c.Status==NestStructureStatus.Completed).All(c=>reachable.Contains(c.Access)),"Câmara concluída sem passagem física.");
        Require(layout.Construction.LastPlanningTick>=0&&layout.Construction.LastPlanningTick<=state.Tick,"Relógio do planejamento inválido.");
    }
    private static void Require(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
}
