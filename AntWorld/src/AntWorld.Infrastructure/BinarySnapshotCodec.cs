using System.Security.Cryptography;
using System.Text;
using AntWorld.Domain;
namespace AntWorld.Infrastructure;

public sealed class BinarySnapshotCodec
{
    private static readonly byte[] Magic = "ANTWORLD"u8.ToArray();
    public const int Version = 9;
    public const int MaximumBytes = 64 * 1024 * 1024;

    public byte[] Encode(SimulationState state)
    {
        SnapshotValidator.Validate(state);
        using var payload = new MemoryStream();
        using (var writer = new BinaryWriter(payload, Encoding.UTF8, true)) WriteState(writer, state);
        var bytes = payload.ToArray();
        if (bytes.Length > MaximumBytes) throw new InvalidDataException("Snapshot excede 64 MiB.");
        using var result = new MemoryStream();
        using var header = new BinaryWriter(result);
        header.Write(Magic); header.Write(Version); header.Write(bytes.Length); header.Write(SHA256.HashData(bytes)); header.Write(bytes);
        return result.ToArray();
    }

    public SimulationState Decode(byte[] snapshot)
    {
        if (snapshot.Length < 48 || snapshot.Length > MaximumBytes + 48) throw new InvalidDataException("Tamanho de snapshot inválido.");
        using var stream = new MemoryStream(snapshot, false);
        using var reader = new BinaryReader(stream);
        if (!reader.ReadBytes(8).SequenceEqual(Magic)) throw new InvalidDataException("Assinatura de snapshot inválida.");
        var version = reader.ReadInt32();
        if (version is not (5 or 6 or 7 or 8 or Version)) throw new InvalidDataException("Versão de snapshot incompatível: formato 9 com leitura compatível dos formatos 5, 6, 7 e 8. Saves dos formatos 1 a 4 não são migrados automaticamente; preserve o arquivo original.");
        var size = reader.ReadInt32();
        if (size < 0 || size != snapshot.Length - 48) throw new InvalidDataException("Snapshot truncado ou tamanho inválido.");
        var hash = reader.ReadBytes(32);
        var bytes = reader.ReadBytes(size);
        if (!CryptographicOperations.FixedTimeEquals(hash, SHA256.HashData(bytes))) throw new InvalidDataException("Integridade do snapshot inválida.");
        try
        {
            using var payload = new MemoryStream(bytes, false);
            using var data = new BinaryReader(payload);
            var state = ReadState(data, version);
            if (payload.Position != payload.Length) throw new InvalidDataException("Dados excedentes no snapshot.");
            SnapshotValidator.Validate(state);
            return state;
        }
        catch (Exception ex) when (ex is EndOfStreamException or ArgumentException or OverflowException)
        {
            throw new InvalidDataException("Conteúdo de snapshot inválido.", ex);
        }
    }

    private static void WriteState(BinaryWriter w, SimulationState state)
    {
        var world = state.World;
        WorldSnapshotCodec.Write(w, world);
        w.Write(state.Random.State); w.Write(state.Tick); w.Write(state.NextAntId); w.Write(state.NextColonyId); w.Write(state.RainIntensity); w.Write(state.DryTicks);
        AiConfigurationCodec.Write(w,state);
        SurvivalSettingsCodec.Write(w,state.Survival);
        w.Write(state.Colonies.Count);
        foreach (var c in state.Colonies)
        {
            w.Write(c.Id); w.Write(c.Species); WritePosition(w, c.Nest); WriteGenome(w, c.QueenGenome); w.Write(c.Generation);
            w.Write(c.Food); w.Write(c.QueenHealth); w.Write(c.ReproductionProgress); w.Write(c.Births); w.Write(c.Deaths);
            AgentSnapshotCodec.WriteColony(w,c);
            var trail = state.Pheromones[c.Id]; w.Write(trail.LastUpdateTick);
            for(var type=0;type<4;type++)
            {
                var entries=trail.Entries((PheromoneType)type).OrderBy(p=>p.Key).ToArray(); w.Write(entries.Length);
                foreach(var (index,intensity) in entries) { w.Write(index);w.Write(intensity); }
            }
        }
        w.Write(state.Ants.Count);
        foreach (var a in state.Ants)
        {
            w.Write(a.Id); w.Write(a.ColonyId); WritePosition(w, a.Position); WritePosition(w, a.PreviousPosition); WriteGenome(w, a.Genes);
            w.Write(a.Energy); w.Write(a.Health); w.Write(a.Carrying); w.Write(a.Age); w.Write(a.Returning); AgentSnapshotCodec.Write(w,a);
        }
    }

    private static SimulationState ReadState(BinaryReader r, int version)
    {
        var world = WorldSnapshotCodec.Read(r, version);
        var randomState = r.ReadUInt64();
        if (randomState == 0) throw new InvalidDataException("PRNG inválido.");
        var state = new SimulationState(world, new(randomState)) { Tick = r.ReadInt64(), NextAntId = r.ReadInt32(), NextColonyId = r.ReadInt32(), RainIntensity = r.ReadDouble(), DryTicks = r.ReadInt32() };
        AiConfigurationCodec.Read(r,state,version);
        state.Survival=version>=7 ? SurvivalSettingsCodec.Read(r) : new SurvivalSettings();
        var colonyCount = ReadCount(r, 12);
        for (var i = 0; i < colonyCount; i++)
        {
            var colony = new Colony(r.ReadInt32(), r.ReadInt32(), ReadPosition(r), ReadGenome(r), r.ReadInt32())
            { Food = r.ReadDouble(), QueenHealth = r.ReadDouble(), ReproductionProgress = r.ReadInt32(), Births = r.ReadInt32(), Deaths = r.ReadInt32() };
            state.Colonies.Add(colony);
            AgentSnapshotCodec.ReadColony(r,colony,version);
            if (version < 9 && !world.CanWalk(colony.Nest))
            {
                foreach (var dx in new[] {1,-1,2,-2})
                {
                    var p=world.Normalize(new(colony.Nest.X+dx,colony.Nest.Y));
                    if (!world.CanWalk(p) || !world.CanWalk(new(p.X,p.Y+Colony.BroodChamberDepth))) continue;
                    colony.RelocateChamber(p); break;
                }
            }
            var trail = new PheromoneField(world.CellCount) { LastUpdateTick=r.ReadInt64() };
            for(var type=0;type<4;type++)
            {
                var trailCount=ReadCount(r,Math.Min(world.CellCount,1000000)); var indices=new HashSet<int>();
                if((long)trailCount*12>r.BaseStream.Length-r.BaseStream.Position) throw new InvalidDataException("Trilha truncada.");
                for(var j=0;j<trailCount;j++)
                {
                    var index=WorldSnapshotCodec.Index(r,world.CellCount);
                    if(!indices.Add(index)) throw new InvalidDataException("Feromônio duplicado.");
                    trail.Set((PheromoneType)type,index,r.ReadDouble());
                }
            }
            if (!state.Pheromones.TryAdd(colony.Id, trail)) throw new InvalidDataException("ID de colônia duplicado.");
        }
        var antCount = ReadCount(r, state.Performance.MaximumPopulation);
        for (var i = 0; i < antCount; i++)
        {
            var id = r.ReadInt32(); var colonyId = r.ReadInt32(); var position = ReadPosition(r); var previous = ReadPosition(r); var genes = ReadGenome(r);
            var energy=r.ReadDouble(); var health=r.ReadDouble();
            if(!double.IsFinite(energy)||energy<0||energy>100) throw new InvalidDataException("Energia inválida.");
            var ant=new Ant(id,colonyId,position,genes) { PreviousPosition=previous,Energy=energy,Carrying=r.ReadDouble(),Age=r.ReadInt64(),Returning=r.ReadBoolean() };
            AgentSnapshotCodec.Read(r,ant,health,version); state.Ants.Add(ant);
        }
        var ants=state.Ants.ToDictionary(a=>a.Id);
        foreach(var colony in state.Colonies) colony.Queen=ants.GetValueOrDefault(colony.QueenId);
        return state;
    }
    private static int ReadCount(BinaryReader r, int limit)
    {
        var count = r.ReadInt32();
        return count >= 0 && count <= limit ? count : throw new InvalidDataException("Contagem inválida.");
    }
    private static void WritePosition(BinaryWriter w, CellPosition p) { w.Write(p.X); w.Write(p.Y); }
    private static CellPosition ReadPosition(BinaryReader r) => new(r.ReadInt32(), r.ReadInt32());
    private static void WriteGenome(BinaryWriter w, Genome g)
    {
        w.Write(g.Aggression); w.Write(g.Strength); w.Write(g.Metabolism); w.Write(g.Exploration); w.Write(g.ThermalOptimum); w.Write(g.ColdTolerance); w.Write(g.Digging);
    }
    private static Genome ReadGenome(BinaryReader r) => new(r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble());
}
