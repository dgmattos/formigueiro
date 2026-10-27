namespace AntWorld.Domain;

// Transient evidence captured before removal, not a cache required by snapshots.
public sealed record AntDeath(
    long Tick, AntIdentity Identity, AntCaste Caste, LifeStage Stage,
    CellPosition Position, CellPosition? Destination, long AgeTicks, AntTask Task,
    DamageCause Causes, int? AttackerId, double HealthBeforeDamage, double FinalDamage,
    double MaximumHealth, double Energy, double Nutrition, double Hydration,
    double Temperature, double ThermalOptimum, double ColdTolerance, double ColonyFood,
    double Carrying, ushort CargoResourceId, ushort WaterAmount);
