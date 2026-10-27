using AntWorld.Domain;
namespace AntWorld.WorldGeneration;

internal sealed class ResourcePlan
{
    public Dictionary<ResourceKey, ResourceStack> Entries { get; } = [];
    public List<DepositDefinition> Deposits { get; } = [];
}
