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
        w.Write((byte)a.WorkerRole); w.Write((byte)a.Support); w.Write(a.MovementSpeed);
        w.Write(a.QueenEscort); w.Write(a.EscortReturning); w.Write(a.PatrolCursor);w.Write((byte)a.FoodDelivery);
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
        if (version >= 9) a.Support = (SupportSide)r.ReadByte();
        a.MovementSpeed=version>=11?r.ReadDouble():0;
        if(version>=12){a.QueenEscort=r.ReadBoolean();a.EscortReturning=r.ReadBoolean();a.PatrolCursor=r.ReadInt32();}
        if(version>=13)a.FoodDelivery=(FoodDeliveryStage)r.ReadByte();
    }
    private static double Unit(BinaryReader r) { var v=r.ReadDouble(); if(!double.IsFinite(v)||v<0||v>1) throw new InvalidDataException("Necessidade inválida."); return v; }
    private static int? Id(BinaryReader r) { var v=r.ReadInt32(); if(v<0) throw new InvalidDataException("Identidade inválida."); return v==0?null:v; }
    public static void WriteColony(BinaryWriter w,Colony c)
    {
        w.Write(c.QueenId); w.Write(c.Signature.PatternA); w.Write(c.Signature.PatternB);
        w.Write(c.Priorities.FoodCollection); w.Write(c.Priorities.Defense); w.Write(c.Priorities.Excavation); w.Write(c.Priorities.BroodCare); w.Write(c.Priorities.Exploration);
        w.Write(c.ExpansionLevel); w.Write(c.ShaftX);
        w.Write(c.Foraging.Visits.Count);
        foreach (var (index,tick) in c.Foraging.Visits.OrderBy(p=>p.Key))
        { w.Write(index); w.Write(tick); w.Write(c.Foraging.Food.Contains(index)); }
        NestSnapshotCodec.WriteLayout(w,c.Layout);
    }
    public static void ReadColony(BinaryReader r,Colony c,int version)
    {
        c.QueenId=r.ReadInt32(); c.Signature=new(r.ReadUInt64(),r.ReadUInt64());
        c.Priorities=new(Unit(r),Unit(r),Unit(r),Unit(r),Unit(r));
        if (version >= 8) c.RestoreExpansionLevel(r.ReadInt32());
        if (version >= 9)
        {
            c.ShaftX=r.ReadInt32();
            var count=r.ReadInt32();
            if (count<0 || count>ForagingMemory.MaximumCells) throw new InvalidDataException("Memória de exploração excede limite.");
            for (var i=0;i<count;i++) c.Foraging.Restore(r.ReadInt32(),r.ReadInt64(),r.ReadBoolean());
        }
        if(version>=10)c.Layout=NestSnapshotCodec.ReadLayout(r);
    }
}
