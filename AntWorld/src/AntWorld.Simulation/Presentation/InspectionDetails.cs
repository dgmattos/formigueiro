using System.Globalization;
using AntWorld.Domain;
namespace AntWorld.Simulation;

public static class InspectionDetails
{
    private static readonly CultureInfo Portuguese=CultureInfo.GetCultureInfo("pt-BR");
    private static string Number(double value,string format="0.0")=>value.ToString(format,Portuguese);
    private static string Percent(double value)=>Number(value*100)+"%";
    public static string Individual(Ant ant)
    {
        var days=ant.Age/SimulationState.TicksPerDay;
        var seconds=(long)(ant.Age%SimulationState.TicksPerDay*SimulationState.TickSeconds);
        return string.Join('\n',new[]{
            $"Colônia: {ant.ColonyId}",$"Espécie: {ant.SpeciesId}",
            $"Casta: {ant.Caste switch{AntCaste.Queen=>"Rainha",AntCaste.Worker=>"Operária",AntCaste.Soldier=>"Soldado",_=>"Macho reprodutor"}}",
            $"Fase: {ant.Stage switch{LifeStage.Egg=>"Ovo",LifeStage.Larva=>"Larva",LifeStage.Pupa=>"Pupa",_=>"Adulto"}}",
            $"Sexo: {(ant.Sex==BiologicalSex.Female?"Fêmea":"Macho")}",
            $"Estado: {(ant.IsAlive?"Vivo":"Morto")}",
            $"Vida: {Number(ant.Health)} / {Number(ant.MaxHealth)}",
            $"Idade: {days} dias, {seconds/3600}h {seconds%3600/60}min",
            $"Energia: {Number(ant.Energy)} / {Number(ant.MaxEnergy)}",
            $"Fome: {Percent(1-ant.Nutrition)}",$"Nutrição: {Percent(ant.Nutrition)}",
            $"Hidratação: {Percent(ant.Hydration)}",$"Condição biológica: {Percent(ant.BiologicalCondition)}",
            $"Função: {ant.WorkerRole switch{WorkerRole.Collector=>"Coletora",WorkerRole.Digger=>"Cavadora",_=>"—"}}",
            $"Escolta da rainha: {(ant.QueenEscort?"Sim":"Não")}",
            $"Ação: {TaskName(ant.CurrentTask)}",$"Carga: {Number(ant.Carrying)}",
            $"Transportando cria: {ant.CarriedAntId?.ToString()??"Não"}",
            $"Posição: {ant.Position.X}, {ant.Position.Y}",
            $"Destino: {(ant.Destination is {} p?$"{p.X}, {p.Y}":"—")}"});
    }
    private static string TaskName(AntTask task)=>task switch
    {
        AntTask.Rest=>"Descansar",AntTask.Move=>"Mover",AntTask.Explore=>"Explorar",AntTask.Collect=>"Coletar",
        AntTask.Carry=>"Transportar",AntTask.Deposit=>"Depositar",AntTask.Dig=>"Escavar",AntTask.Build=>"Construir",
        AntTask.Attack=>"Atacar",AntTask.Defend=>"Defender",AntTask.Flee=>"Fugir",AntTask.FeedLarva=>"Alimentar larva",
        AntTask.RecoverHealth=>"Recuperar saúde",AntTask.LayEgg=>"Botar ovo",AntTask.Feed=>"Alimentar-se",AntTask.Drink=>"Beber",_=>"—"
    };
    public static string Block(WorldMap world,CellPosition position)
    {
        position=world.Normalize(position);var cell=world.GetCell(position,false);
        var rows=new List<string>
        {
            $"Posição: {position.X}, {position.Y}",$"Chunk: {world.ChunkManager.ChunkId(position)}",
            $"Tipo: {cell.Type switch{BlockType.Air=>"Ar",BlockType.AirAndSoil=>"Ar e solo",BlockType.Soil=>"Solo",BlockType.SoilAndRock=>"Solo e rocha",_=>"Rocha"}}",
            $"Bioma: {world.Biomes[position.X] switch{BiomeKind.Forest=>"Floresta",BiomeKind.Grassland=>"Campo",BiomeKind.Desert=>"Deserto",BiomeKind.Swamp=>"Pântano",_=>"Região fria"}}",
            $"Preenchimento sólido: {Percent(cell.SolidFill/255.0)}",
            $"Rocha na parte sólida: {Percent(cell.RockFraction/255.0)}",
            $"Umidade: {cell.Moisture}%",$"Água livre: {cell.WaterAmount}",
            $"Temperatura do bloco: {Number(cell.Temperature/10.0)} °C",
            $"Porosidade: {Percent(cell.Porosity/255.0)}",$"Permeabilidade: {Percent(cell.Permeability/255.0)}",
            $"Nutrientes: {cell.Nutrients}",$"Água retida: {Number(cell.RetainedWater)}",
            $"Escavado: {(cell.Flags.HasFlag(CellFlags.Excavated)?"Sim":"Não")}",
            $"Modificado: {(cell.Flags.HasFlag(CellFlags.Modified)?"Sim":"Não")}"
        };
        if(cell.SolidFill>0 && cell.RockFraction<255)rows.Add($"Material do solo: {MaterialCatalog.Get(cell.SoilMaterial).Name}");
        if(cell.RockFraction>0)rows.Add($"Material rochoso: {MaterialCatalog.Get(cell.RockMaterial).Name}");
        foreach(var definition in world.ResourceCatalog.Definitions)
        {
            var resource=world.GetResource(position,definition.Id);
            if(resource.Quantity>0)rows.Add($"{definition.Name}: {Number(resource.Quantity)} / {Number(resource.Capacity)}");
        }
        return string.Join('\n',rows);
    }
}
