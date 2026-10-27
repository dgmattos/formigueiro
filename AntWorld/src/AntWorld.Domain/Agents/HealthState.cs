namespace AntWorld.Domain;
public struct HealthState
{
    public double Current { get; private set; }
    public double Maximum { get; private set; }
    public double RecoveryRate { get; private set; }
    public readonly double Percentage => Maximum == 0 ? 0 : Current / Maximum;
    public HealthState(double maximum, double recoveryRate = .1) { Check(maximum); Check(recoveryRate); Maximum = Current = maximum; RecoveryRate = recoveryRate; }
    public void Damage(double amount) { Check(amount); Current = Math.Max(0, Current - amount); }
    public void Recover(double amount) { Check(amount); Current = Math.Min(Maximum, Current + amount); }
    public void ChangeMaximum(double maximum) { Check(maximum); Maximum = maximum; Current = Math.Min(Current, maximum); }
    public void Restore(double current, double maximum, double recovery) { Check(current); Check(maximum); Check(recovery); if (current > maximum) throw new ArgumentOutOfRangeException(nameof(current)); Current=current; Maximum=maximum; RecoveryRate=recovery; }
    private static void Check(double value) { if (!double.IsFinite(value) || value < 0 || value > 100000) throw new ArgumentOutOfRangeException(nameof(value)); }
}
