namespace AntWorld.Domain;

public sealed record GeologySettings(double SurfaceLevel = .29, double SoilDepth = .16,
    double RockDepth = .42, double CaveStart = .12, double CaveThreshold = .78,
    double OrganicDepth = .24, double MineralDepth = .48, int DepositSpacing = 32,
    int VeinSteps = 72, double VeinBudget = 1200)
{
    public void Validate()
    {
        var values = new[] { SurfaceLevel, SoilDepth, RockDepth, CaveStart, CaveThreshold, OrganicDepth, MineralDepth, VeinBudget };
        if (values.Any(v => !double.IsFinite(v)) || SurfaceLevel is < .15 or > .5 || SoilDepth is < .05 or > .4 ||
            RockDepth <= SoilDepth || RockDepth > .9 || CaveStart is < 0 or > 1 || CaveThreshold is < .4 or > 1.4 ||
            OrganicDepth is <= 0 or > 1 || MineralDepth is <= 0 or > .95 || DepositSpacing is < 16 or > 256 ||
            VeinSteps is < 8 or > 256 || VeinBudget is <= 0 or > 100000)
            throw new ArgumentException("Configuração geológica inválida.");
    }
}
