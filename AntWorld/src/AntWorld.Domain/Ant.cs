namespace AntWorld.Domain;

public sealed class Ant(int id, int colonyId, CellPosition position, Genome genes)
{
    private double energy = 80, nutrition = 1, hydration = 1, condition = 1;
    private HealthState health = new(60);
    public int Id { get; } = id;
    public int ColonyId { get; private set; } = colonyId;
    public void JoinColony(Colony colony) { ColonyId=colony.Id; SpeciesId=colony.Species; Position=PreviousPosition=colony.Nest; Destination=null; TargetId=null; CurrentTask=AntTask.Rest; }
    public int SpeciesId { get; set; }
    public long LineageId { get; set; } = colonyId;
    public int? MotherId { get; set; }
    public int? FatherId { get; set; }
    public AntIdentity Identity => new(Id, ColonyId, SpeciesId, LineageId, MotherId, FatherId);
    public ChemicalSignature Signature { get; set; }
    public AntCaste Caste { get; set; } = AntCaste.Worker;
    public LifeStage Stage { get; set; } = LifeStage.Adult;
    public BiologicalSex Sex => Caste == AntCaste.MaleReproductive ? BiologicalSex.Male : BiologicalSex.Female;
    public CellPosition Position { get; set; } = position;
    public CellPosition PreviousPosition { get; set; } = position;
    public CellPosition? Destination { get; set; }
    public Genome Genes { get; } = genes;
    public double Energy { get => energy; set => energy = Bounded(value,100); }
    public double MaxEnergy => 100;
    public double Nutrition { get => nutrition; set => nutrition = Bounded(value,1); }
    public double Hydration { get => hydration; set => hydration = Bounded(value,1); }
    public double BiologicalCondition { get => condition; set => condition = Bounded(value,1); }
    public double Health { get => health.Current; set { if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); health.Restore(Math.Clamp(value,0,health.Maximum),health.Maximum,health.RecoveryRate); } }
    public double MaxHealth => health.Maximum;
    public double RecoveryRate => health.RecoveryRate;
    public void ChangeMaximumHealth(double value) => health.ChangeMaximum(value);
    public DamageCause FatalDamageCauses { get; private set; }
    public int? FatalAttackerId { get; private set; }
    public double HealthBeforeFatalDamage { get; private set; }
    public double FatalDamageAmount { get; private set; }
    public void Damage(double value, DamageCause causes = DamageCause.Unknown, int? attackerId = null)
    {
        var before = health.Current;
        health.Damage(value);
        if (before > 0 && !IsAlive)
        {
            FatalDamageCauses = causes; FatalAttackerId = attackerId;
            HealthBeforeFatalDamage = before; FatalDamageAmount = value;
        }
    }
    public void Recover(double value) => health.Recover(value);
    public void RestoreHealth(double current,double maximum,double recovery) => health.Restore(current,maximum,recovery);
    public double Carrying { get; set; }
    public ushort CargoResourceId { get; set; } = ResourceIds.Food;
    public int? CarriedAntId { get; set; }
    public int? CarrierId { get; set; }
    public long Age { get; set; }
    public double DevelopmentTicks { get; set; }
    public double MovementCredit { get; set; }
    public MovementState Movement { get; set; }
    public AntTask CurrentTask { get; set; } = AntTask.Explore;
    public int? TargetId { get; set; }
    public long LastDecisionTick { get; set; } = -1;
    public long LastAttackTick { get; set; } = -100;
    public long LastEggTick { get; set; } = -480;
    public int StoredSperm { get; set; } = 10000;
    public double CurrentUtility { get; set; }
    public bool Returning { get; set; }
    public bool IsAlive => Health > 0;
    public bool IsTransportableBrood => IsAlive && Stage is LifeStage.Egg or LifeStage.Larva;
    public SkillProgress[] Skills { get; } = new SkillProgress[10];
    public double Efficiency(SkillType type) => 1 + Math.Max(0, Skills[(int)type].Level - 1) * SkillCatalog.Get(type).EfficiencyPerLevel;
    public void Learn(SkillType type, double amount) => Skills[(int)type].Gain(amount, .5 + Genes.Exploration / 2, SkillCatalog.Get(type).MaximumLevel);
    private static double Bounded(double value,double maximum) => double.IsFinite(value) ? Math.Clamp(value,0,maximum) : throw new ArgumentOutOfRangeException(nameof(value));
}
