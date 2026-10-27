using AntWorld.Domain;
namespace AntWorld.Infrastructure;

public static class SnapshotValidator
{
    public static void Validate(SimulationState state)
    {
        state.Survival.Validate();
        var world = state.World;
        state.Performance.Validate(); foreach(var species in state.Species.Values) species.Validate();
        Require(state.SchedulerCursor>=0,"Cursor inválido.");
        world.Configuration.Validate();
        Require(state.Tick >= 0 && state.Tick < int.MaxValue * (long)SimulationState.TicksPerDay, "Tick inválido.");
        Require(state.DryTicks >= 0 && Range(state.RainIntensity, 0, 1), "Clima inválido.");
        Require(state.Colonies.Count is >= 1 and <= 12 && state.Ants.Count <= state.Performance.MaximumPopulation, "População inválida.");
        Require(state.Colonies.Select(c => c.Id).Distinct().Count() == state.Colonies.Count, "IDs de colônia duplicados.");
        Require(state.Ants.Select(a => a.Id).Distinct().Count() == state.Ants.Count, "IDs de formiga duplicados.");
        Require(state.NextAntId > state.Ants.Select(a => a.Id).DefaultIfEmpty(0).Max() && state.NextColonyId > state.Colonies.Max(c => c.Id), "Sequência de IDs inválida.");
        for (var x = 0; x < world.Width; x++)
            Require(Enum.IsDefined(world.Biomes[x]) && world.Surface[x] > 0 && world.Surface[x] < world.Height && Range(world.Moisture[x], 0, 1), "Coluna inválida.");
        foreach (var (index, cell) in world.ChunkManager.CellChanges)
        {
            Require(index >= 0 && index < world.CellCount && cell.Flags.HasFlag(CellFlags.Modified), "Alteração de célula inválida.");
            cell.Validate();
        }
        foreach (var (key, stack) in world.ChunkManager.ResourceChanges)
        {
            Require(key.CellIndex >= 0 && key.CellIndex < world.CellCount, "Índice de recurso inválido.");
            world.ResourceCatalog.Get(key.ResourceId); stack.Validate();
        }
        Require(state.Pheromones.Count == state.Colonies.Count, "Trilhas incompletas.");
        foreach (var colony in state.Colonies)
        {
            colony.QueenGenome.Validate();
            Require(colony.ExpansionLevel >= 0 && colony.ExpansionLevel <= world.Height / Colony.BroodChamberDepth &&
                colony.Nest.Y - colony.ExpansionLevel * Colony.BroodChamberDepth >= 0 && world.IsCanonical(colony.BroodChamber), "Andares da colônia inválidos.");
            Require(colony.Id > 0 && state.Species.ContainsKey(colony.Species) && colony.Generation >= 0 && world.IsCanonical(colony.Nest) && world.CanWalk(colony.Nest), "Colônia inválida.");
            Require(Range(colony.Food, 0, 1e12) && double.IsFinite(colony.QueenHealth) && colony.ReproductionProgress >= 0 && colony.Births >= 0 && colony.Deaths >= 0, "Fisiologia da rainha inválida.");
            Require(state.Pheromones.TryGetValue(colony.Id, out var trail) && trail.Length == world.Terrain.Length && trail.LastUpdateTick>=0 && trail.LastUpdateTick<=state.Tick && Enumerable.Range(0,4).All(t=>trail.Entries((PheromoneType)t).All(p=>p.Key>=0&&p.Key<world.CellCount&&Range(p.Value,.001,15))), "Feromônios inválidos.");
        }
        var colonyIds = state.Colonies.Select(c => c.Id).ToHashSet();
        var antsById=state.Ants.ToDictionary(a=>a.Id);
        foreach(var (pair,relation) in state.Relations.Entries) Require(colonyIds.Contains(pair.First)&&colonyIds.Contains(pair.Second)&&Enum.IsDefined(relation),"Relação com colônia inexistente.");
        foreach(var colony in state.Colonies)
            Require(colony.QueenId>0 && (colony.Queen is { Caste:AntCaste.Queen } q && q.Id==colony.QueenId && q.ColonyId==colony.Id || colony.QueenHealth<=0),"Identidade da rainha inválida.");
        foreach (var ant in state.Ants)
        {
            ant.Genes.Validate();
            Require(Enum.IsDefined(ant.WorkerRole) && (ant.WorkerRole == WorkerRole.None || ant.Caste == AntCaste.Worker && ant.Stage == LifeStage.Adult), "Função de operária inválida.");
            Require(state.Species.ContainsKey(ant.SpeciesId) && Enum.IsDefined(ant.Caste)&&Enum.IsDefined(ant.Stage)&&Enum.IsDefined(ant.Movement)&&Enum.IsDefined(ant.CurrentTask),"Estado biológico inválido.");
            Require(ant.SpeciesId==state.Colonies.First(c=>c.Id==ant.ColonyId).Species && ant.LineageId>0 && (ant.MotherId is null or >0) && (ant.FatherId is null or >0),"Identidade familiar inválida.");
            Require(Range(ant.MaxHealth,.001,100000)&&Range(ant.RecoveryRate,0,100000)&&Range(ant.Nutrition,0,1)&&Range(ant.Hydration,0,1)&&Range(ant.BiologicalCondition,0,1)&&Range(ant.DevelopmentTicks,0,1e12)&&Range(ant.MovementCredit,0,2),"Biologia inválida.");
            Require(ant.Destination is null || world.IsCanonical(ant.Destination.Value),"Destino inválido.");
            Require(ant.LastDecisionTick>=-1&&ant.LastDecisionTick<=state.Tick&&ant.LastAttackTick>=-100&&ant.LastAttackTick<=state.Tick&&ant.LastEggTick>=-480&&ant.LastEggTick<=state.Tick&&ant.StoredSperm>=0&&Range(ant.CurrentUtility,0,1000),"Estado comportamental inválido.");
            if(ant.CarriedAntId is {} id) Require(antsById.TryGetValue(id,out var brood)&&brood.CarrierId==ant.Id&&brood.IsTransportableBrood&&brood.ColonyId==ant.ColonyId,"Carga biológica inconsistente.");
            if(ant.CarrierId is {} carrierId) Require(antsById.TryGetValue(carrierId,out var carrier)&&carrier.CarriedAntId==ant.Id&&carrier.Position==ant.Position,"Transportador inconsistente.");
            world.ResourceCatalog.Get(ant.CargoResourceId);
            foreach(var skill in ant.Skills) { var copy=skill; copy.Restore(skill.Level,skill.Experience); }
            Require(ant.Id > 0 && colonyIds.Contains(ant.ColonyId) && world.IsCanonical(ant.Position) && world.CanWalk(ant.Position) && world.IsCanonical(ant.PreviousPosition), "Formiga inválida.");
            Require(Range(ant.Energy, 0, 100) && Range(ant.Health, 0, ant.MaxHealth) && Range(ant.Carrying, 0, 100000) && ant.Age >= 0, "Fisiologia da formiga inválida.");
        }
    }
    private static bool Range(double value, double minimum, double maximum) => double.IsFinite(value) && value >= minimum && value <= maximum;
    private static void Require(bool valid, string message) { if (!valid) throw new InvalidDataException(message); }
}
