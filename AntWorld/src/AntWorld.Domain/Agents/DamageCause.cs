namespace AntWorld.Domain;

[Flags]
public enum DamageCause : byte
{
    Unknown = 0,
    ThermalStress = 1,
    EnergyDepletion = 2,
    Starvation = 4,
    Dehydration = 8,
    Aging = 16,
    Combat = 32
}
