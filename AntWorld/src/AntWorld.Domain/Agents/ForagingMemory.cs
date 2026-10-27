namespace AntWorld.Domain;

// Shared, sparse colony knowledge; no map allocation per individual. -1 means seen but not visited.
public sealed class ForagingMemory
{
    public const int MaximumCells = 32768;
    private readonly Dictionary<int,long> visits = [];
    private readonly HashSet<int> food = [];
    public IReadOnlyDictionary<int,long> Visits => visits;
    public IReadOnlySet<int> Food => food;
    public void Observe(int index, long tick, bool visited, bool edible)
    {
        if (!visits.ContainsKey(index) && visits.Count >= MaximumCells) return;
        if (visited) visits[index] = tick;
        else visits.TryAdd(index, -1);
        if (edible) food.Add(index); else food.Remove(index);
    }
    public void Restore(int index, long tick, bool edible)
    {
        if (index < 0 || tick < -1 || visits.Count >= MaximumCells || !visits.TryAdd(index, tick))
            throw new ArgumentException("Memória de exploração inválida.");
        if (edible) food.Add(index);
    }
}
