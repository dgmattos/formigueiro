namespace AntWorld.Domain;

/// <summary>Configurable constraints for physical nest construction.</summary>
public sealed record NestLayoutSettings(
    int MinimumChamberSpacing = 3,
    int MinimumChamberRadiusX = 2,
    int MaximumChamberRadiusX = 5,
    int MinimumChamberRadiusY = 1,
    int MaximumChamberRadiusY = 2,
    double MinimumAlternativeRouteRatio = .20,
    double TargetAlternativeRouteRatio = .25,
    double MaximumAlternativeRouteRatio = .30,
    int MinimumChambersForLoops = 5,
    int MaximumEntrances = 5,
    int MaximumConcurrentExcavationJobs = 6,
    int MaximumChambers = 60, int PlanningIntervalTicks = 200, double BranchingProbability = .65)
{
    public NestSpeciesProfile Normal { get; init; } = new();
    public NestSpeciesProfile Strong { get; init; } = new(.6,.65,1.5);
    public NestSpeciesProfile Surface { get; init; } = new(.9,-.35,4,3);
    public NestSpeciesProfile For(int species) => species switch { 2=>Strong,3=>Surface,_=>Normal };
    public void Validate()
    {
        if(Normal is null||Strong is null||Surface is null)throw new ArgumentException("Perfis de ninho ausentes.");
        Normal.Validate(); Strong.Validate(); Surface.Validate();
        if (MaximumChambers is <4 or >60 || PlanningIntervalTicks is <20 or >1000000 ||
            !double.IsFinite(BranchingProbability)||BranchingProbability is <0 or >1 ||
            !double.IsFinite(MinimumAlternativeRouteRatio)||!double.IsFinite(TargetAlternativeRouteRatio)||!double.IsFinite(MaximumAlternativeRouteRatio)||
            MinimumChamberSpacing is <1 or >32 || MaximumChamberRadiusX>5 || MaximumChamberRadiusY>2 ||
            MinimumChamberRadiusX < 1 || MaximumChamberRadiusX < MinimumChamberRadiusX ||
            MinimumChamberRadiusY < 1 || MaximumChamberRadiusY < MinimumChamberRadiusY ||
            MinimumAlternativeRouteRatio < 0 || MaximumAlternativeRouteRatio >= 1 ||
            MinimumAlternativeRouteRatio > TargetAlternativeRouteRatio ||
            TargetAlternativeRouteRatio > MaximumAlternativeRouteRatio ||
            MinimumChambersForLoops is <5 or >60 || MaximumEntrances is <1 or >5 ||
            MaximumConcurrentExcavationJobs is <1 or >64)
            throw new ArgumentException("Invalid nest layout settings.");
    }
}
