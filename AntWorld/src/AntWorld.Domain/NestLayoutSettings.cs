namespace AntWorld.Domain;

/// <summary>Configurable constraints for future physical nest construction.</summary>
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
    int MaximumConcurrentExcavationJobs = 6)
{
    public void Validate()
    {
        if (MinimumChamberSpacing < 0 ||
            MinimumChamberRadiusX < 1 || MaximumChamberRadiusX < MinimumChamberRadiusX ||
            MinimumChamberRadiusY < 1 || MaximumChamberRadiusY < MinimumChamberRadiusY ||
            MinimumAlternativeRouteRatio < 0 || MaximumAlternativeRouteRatio >= 1 ||
            MinimumAlternativeRouteRatio > TargetAlternativeRouteRatio ||
            TargetAlternativeRouteRatio > MaximumAlternativeRouteRatio ||
            MinimumChambersForLoops < 3 || MaximumEntrances < 1 ||
            MaximumConcurrentExcavationJobs < 1)
            throw new ArgumentException("Invalid nest layout settings.");
    }
}

/// <summary>
/// Topological acceptance metrics. A completed connection is not proof of physical
/// walkability; callers must additionally validate WorldMap.CanWalk and navigation.
/// </summary>
public static class NestLayoutValidation
{
    public static IReadOnlyList<string> ValidateTopology(NestLayout layout, NestLayoutSettings settings)
    {
        settings.Validate();
        var errors = new List<string>();
        if (!layout.IsConnected())
            errors.Add("Planned chamber graph is disconnected.");
        if (!layout.IsConnected(completedOnly: true))
            errors.Add("Completed chamber graph is disconnected.");
        if (layout.Entrances.Count > settings.MaximumEntrances)
            errors.Add("Too many entrances.");
        if (layout.Chambers.Count >= settings.MinimumChambersForLoops &&
            layout.IsConnected() && layout.Tunnels.Count >= layout.Chambers.Count - 1)
        {
            var ratio = layout.AlternativeRouteRatio();
            if (ratio > settings.MaximumAlternativeRouteRatio)
                errors.Add("Planned alternative-route ratio exceeds maximum.");
        }
        foreach (var entrance in layout.Entrances)
            if (!layout.Chambers.Any(c => c.Id == entrance.ChamberId))
                errors.Add($"Entrance {entrance.Id} has no chamber.");
        return errors;
    }
}
