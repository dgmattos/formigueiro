namespace AntWorld.Domain;
public sealed record PerformanceProfile
{
    public string Name => "LowMemory4GB";
    public int TargetFps { get; init; } = 60;
    public int MaxRenderedAgents { get; init; } = 300;
    public int MaxDetailedAgents { get; init; } = 2000;
    public int IndividualDecisionsPerSecond { get; init; } = 2;
    public int MovementTicksPerSecond { get; init; } = 20;
    public int MaxMemoryBudgetMb { get; init; } = 768;
    public int MaximumPopulation { get; init; } = 20000;
    public int BiologicalIntervalTicks { get; init; } = 20;
    public int ColonyIntervalTicks { get; init; } = 20;
    public bool EnableSpatialPartitioning => true;
    public bool EnableSimulationLevels => true;
    // Reuse spatial buckets and scheduler buffers; live identities are never recycled.
    public bool EnableAgentPooling => false;
    public void Validate()
    {
        if (MaxRenderedAgents < 0 || MaxRenderedAgents > 100000 || MaxDetailedAgents < 1 || MaxDetailedAgents > 100000 ||
            IndividualDecisionsPerSecond is < 1 or > 20 || MovementTicksPerSecond is < 1 or > 20 ||
            20 % IndividualDecisionsPerSecond != 0 || 20 % MovementTicksPerSecond != 0 ||
            MaxMemoryBudgetMb is < 64 or > 4096 || MaximumPopulation is < 1 or > 100000 ||
            BiologicalIntervalTicks is < 1 or > 200 || ColonyIntervalTicks is < 1 or > 200 || TargetFps is < 1 or > 240)
            throw new ArgumentException("Perfil de desempenho inválido.");
    }
}
