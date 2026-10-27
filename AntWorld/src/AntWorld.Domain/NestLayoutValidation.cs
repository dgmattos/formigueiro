namespace AntWorld.Domain;

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
