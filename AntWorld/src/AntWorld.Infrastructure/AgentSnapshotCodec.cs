using AntWorld.Domain;
namespace AntWorld.Infrastructure;
internal static class AgentSnapshotCodec
{
    public static void Write(BinaryWriter w,Ant a)
    {
        w.Write(a.SpeciesId); w.Write(a.LineageId); w.Write(a.MotherId??0); w.Write(a.FatherId??0);
        w.Write((byte)a.Caste); w.Write((byte)a.Stage); w.Write(a.Signature.PatternA); w.Write(a.Signature.PatternB);
        w.Write(a.MaxHealth); w.Write(a.RecoveryRate); w.Write(a.Nutrition); w.Write(a.Hydration); w.Write(a.BiologicalCondition);
        w.Write(a.DevelopmentTicks); w.Write(a.MovementCredit); w.Write((byte)a.Movement); w.Write((byte)a.CurrentTask);
        w.Write(a.TargetId??0); w.Write(a.Destination.HasValue); if(a.Destination is {} p) { w.Write(p.X); w.Write(p.Y); }
        w.Write(a.LastDecisionTick); w.Write(a.LastAttackTick); w.Write(a.LastEggTick); w.Write(a.StoredSperm); w.Write(a.CurrentUtility);
        w.Write(a.CargoResourceId); w.Write(a.CarriedAntId??0); w.Write(a.CarrierId??0);
        foreach(var progress in a.Skills) { w.Write(progress.Level); w.Write(progress.Experience); }
        w.Write((byte)a.WorkerRole);
    }
    public static void Read(BinaryReader r,Ant a,double health,int version)
    {
        a.SpeciesId=r.ReadInt32(); a.LineageId=r.ReadInt64(); a.MotherId=Id(r); a.FatherId=Id(r);
        a.Caste=(AntCaste)r.ReadByte(); a.Stage=(LifeStage)r.ReadByte(); a.Signature=new(r.ReadUInt64(),r.ReadUInt64());
        a.RestoreHealth(health,r.ReadDouble(),r.ReadDouble()); a.Nutrition=Unit(r); a.Hydration=Unit(r); a.BiologicalCondition=Unit(r);
        a.DevelopmentTicks=r.ReadDouble(); a.MovementCredit=r.ReadDouble(); a.Movement=(MovementState)r.ReadByte(); a.CurrentTask=(AntTask)r.ReadByte();
        a.TargetId=Id(r); a.Destination=r.ReadBoolean()?new(r.ReadInt32(),r.ReadInt32()):null;
        a.LastDecisionTick=r.ReadInt64(); a.LastAttackTick=r.ReadInt64(); a.LastEggTick=r.ReadInt64(); a.StoredSperm=r.ReadInt32(); a.CurrentUtility=r.ReadDouble();
        a.CargoResourceId=r.ReadUInt16(); a.CarriedAntId=Id(r); a.CarrierId=Id(r);
        for(var i=0;i<a.Skills.Length;i++) a.Skills[i].Restore(r.ReadByte(),r.ReadDouble());
        a.WorkerRole = version >= 8 ? (WorkerRole)r.ReadByte() :
            a.Caste == AntCaste.Worker && a.Stage == LifeStage.Adult ?
                (unchecked((uint)a.Id * 2654435761u) >> 31) == 0 ? WorkerRole.Collector : WorkerRole.Digger : WorkerRole.None;
    }
    private static double Unit(BinaryReader r) { var v=r.ReadDouble(); if(!double.IsFinite(v)||v<0||v>1) throw new InvalidDataException("Necessidade inválida."); return v; }
    private static int? Id(BinaryReader r) { var v=r.ReadInt32(); if(v<0) throw new InvalidDataException("Identidade inválida."); return v==0?null:v; }
    public static void WriteColony(BinaryWriter w,Colony c)
    {
        w.Write(c.QueenId); w.Write(c.Signature.PatternA); w.Write(c.Signature.PatternB);
        w.Write(c.Priorities.FoodCollection); w.Write(c.Priorities.Defense); w.Write(c.Priorities.Excavation); w.Write(c.Priorities.BroodCare); w.Write(c.Priorities.Exploration);
        w.Write(c.ExpansionLevel);
    }
    public static void ReadColony(BinaryReader r,Colony c,int version)
    {
        c.QueenId=r.ReadInt32(); c.Signature=new(r.ReadUInt64(),r.ReadUInt64());
        c.Priorities=new(Unit(r),Unit(r),Unit(r),Unit(r),Unit(r));
        if (version >= 8) c.RestoreExpansionLevel(r.ReadInt32());
    }
}
