namespace AntWorld.Domain;
public sealed record NestSettings(int NormalBaseDepth = 4, double NormalDepthMultiplier = 1.25, double StrongUndergroundRatio = .75, int SurfaceDepth = 2)
{
    public void Validate()
    {
        if (SurfaceDepth is < 1 or > 128 || NormalBaseDepth is < 1 or > 128 || !double.IsFinite(NormalDepthMultiplier) || NormalDepthMultiplier is <= 0 or > 10 ||
            !double.IsFinite(StrongUndergroundRatio) || StrongUndergroundRatio is <= 0 or >= 1) throw new ArgumentException("Profundidades dos ninhos inválidas.");
    }
    public int QueenY(WorldMap world, int x, int species)
    {
        var surface = world.Surface[world.ChunkManager.NormalizeX(x)];
        var depth = species switch
        {
            2 => (int)Math.Round((world.Height - 1 - surface) * StrongUndergroundRatio),
            3 => SurfaceDepth,
            _ => (int)Math.Round(NormalBaseDepth * NormalDepthMultiplier)
        };
        return Math.Min(surface + Math.Max(1, depth), world.Height - Colony.BroodChamberDepth - 2);
    }
}
