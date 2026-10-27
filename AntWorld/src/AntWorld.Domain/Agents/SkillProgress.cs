namespace AntWorld.Domain;
public struct SkillProgress
{
    public byte Level { get; private set; }
    public double Experience { get; private set; }
    public void Gain(double amount, double learning, int maximum = 20)
    {
        if (!double.IsFinite(amount) || amount < 0 || !double.IsFinite(learning) || learning <= 0 || maximum is < 1 or > 255) throw new ArgumentOutOfRangeException(nameof(amount));
        if (Level == 0) Level = 1;
        Experience += amount * learning;
        while (Level < maximum && Experience >= Level * Level * 10) { Experience -= Level * Level * 10; Level++; }
        if (Level >= maximum) Experience = 0;
    }
    public void Restore(byte level, double experience)
    { if (level > 20 || !double.IsFinite(experience) || experience < 0 || experience >= Math.Max(1, (int)level) * Math.Max(1, (int)level) * 10 || level == 20 && experience != 0) throw new ArgumentException("Progresso inválido."); Level=level; Experience=experience; }
}
