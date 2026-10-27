namespace AntWorld.Domain;
public sealed record CasteDefinition(double MaximumHealth, double Speed, double Attack, double Defense, double CarryCapacity,
    double LongevityDays, AntCapability Capabilities)
{
    public void Validate()
    {
        if (!double.IsFinite(MaximumHealth) || MaximumHealth <= 0 || MaximumHealth > 100000 ||
            new[] { Speed, Attack, Defense, CarryCapacity, LongevityDays }.Any(v => !double.IsFinite(v) || v < 0 || v > 10000))
            throw new ArgumentOutOfRangeException(nameof(CasteDefinition));
    }
}
