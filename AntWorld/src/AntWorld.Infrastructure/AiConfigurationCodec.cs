using AntWorld.Domain;
namespace AntWorld.Infrastructure;
internal static class AiConfigurationCodec
{
    public static void Write(BinaryWriter w,SimulationState state)
    {
        var p=state.Performance;
        w.Write(p.TargetFps);w.Write(p.MaxRenderedAgents);w.Write(p.MaxDetailedAgents);w.Write(p.IndividualDecisionsPerSecond);w.Write(p.MovementTicksPerSecond);
        w.Write(p.MaxMemoryBudgetMb);w.Write(p.MaximumPopulation);w.Write(p.BiologicalIntervalTicks);w.Write(p.ColonyIntervalTicks);w.Write(state.SchedulerCursor);
        w.Write(state.Species.Count);
        foreach(var (id,s) in state.Species.OrderBy(p=>p.Key))
        {
            w.Write(id);w.Write(s.EggDays);w.Write(s.LarvaDays);w.Write(s.PupaDays);w.Write(s.EggIntervalTicks);w.Write(s.EggCost);
            w.Write(s.SoilDiggingMultiplier); w.Write(s.MixedDiggingMultiplier); w.Write(s.SolidRockSpeedFactor); w.Write(s.SolidRockEnergyFactor);
            foreach(var c in s.Castes) { w.Write(c.MaximumHealth);w.Write(c.Speed);w.Write(c.Attack);w.Write(c.Defense);w.Write(c.CarryCapacity);w.Write(c.LongevityDays);w.Write((uint)c.Capabilities); }
            foreach(var b in s.Behaviors) { w.Write(b.Foraging);w.Write(b.Defense);w.Write(b.Excavation);w.Write(b.BroodCare);w.Write(b.Exploration);w.Write(b.Reproduction); }
        }
        var relations=state.Relations.Entries.OrderBy(p=>p.Key.First).ThenBy(p=>p.Key.Second).ToArray(); w.Write(relations.Length);
        foreach(var (pair,relation) in relations) { w.Write(pair.First);w.Write(pair.Second);w.Write((byte)relation); }
    }
    public static void Read(BinaryReader r,SimulationState state)
    {
        state.Performance=new() { TargetFps=r.ReadInt32(),MaxRenderedAgents=r.ReadInt32(),MaxDetailedAgents=r.ReadInt32(),IndividualDecisionsPerSecond=r.ReadInt32(),MovementTicksPerSecond=r.ReadInt32(),
            MaxMemoryBudgetMb=r.ReadInt32(),MaximumPopulation=r.ReadInt32(),BiologicalIntervalTicks=r.ReadInt32(),ColonyIntervalTicks=r.ReadInt32() }; state.Performance.Validate(); state.SchedulerCursor=r.ReadInt32();
        state.Species.Clear(); var speciesCount=Count(r,32);
        for(var i=0;i<speciesCount;i++)
        {
            var id=r.ReadInt32(); var s=new SpeciesDefinition { EggDays=r.ReadDouble(),LarvaDays=r.ReadDouble(),PupaDays=r.ReadDouble(),EggIntervalTicks=r.ReadInt32(),EggCost=r.ReadDouble(),
                SoilDiggingMultiplier=r.ReadDouble(), MixedDiggingMultiplier=r.ReadDouble(), SolidRockSpeedFactor=r.ReadDouble(), SolidRockEnergyFactor=r.ReadDouble() };
            for(var j=0;j<4;j++) s.Castes[j]=new(r.ReadDouble(),r.ReadDouble(),r.ReadDouble(),r.ReadDouble(),r.ReadDouble(),r.ReadDouble(),(AntCapability)r.ReadUInt32());
            for(var j=0;j<4;j++) s.Behaviors[j]=new(r.ReadDouble(),r.ReadDouble(),r.ReadDouble(),r.ReadDouble(),r.ReadDouble(),r.ReadDouble());
            s.Validate(); if(id<0 || !state.Species.TryAdd(id,s)) throw new InvalidDataException("Espécie duplicada ou inválida.");
        }
        var count=Count(r,66); var keys=new HashSet<(int,int)>();
        for(var i=0;i<count;i++)
        { var a=r.ReadInt32();var b=r.ReadInt32(); if(a>=b||!keys.Add((a,b))) throw new InvalidDataException("Relação duplicada ou inválida."); state.Relations.Set(a,b,(ColonyRelation)r.ReadByte()); }
    }
    private static int Count(BinaryReader r,int maximum) { var n=r.ReadInt32(); return n>=0&&n<=maximum?n:throw new InvalidDataException("Contagem inválida."); }
}
