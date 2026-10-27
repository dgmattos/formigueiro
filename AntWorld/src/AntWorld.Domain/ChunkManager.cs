namespace AntWorld.Domain;

public sealed class ChunkManager
{
    private readonly WorldConfiguration configuration;
    private readonly IWorldChunkSource source;
    private readonly Dictionary<int, WorldChunk> loaded = [];
    private readonly Dictionary<int, LinkedListNode<int>> cacheNodes = [];
    private readonly LinkedList<int> recent = [];
    private readonly Dictionary<int, WorldCell> cellChanges = [];
    private readonly Dictionary<ResourceKey, ResourceStack> resourceChanges = [];
    private readonly Dictionary<int, Dictionary<ResourceKey, ResourceStack>> resourceChangesByChunk = [];
    public int LoadedCount => loaded.Count;
    public long GeneratedCount { get; private set; }
    public IReadOnlyDictionary<int, WorldCell> CellChanges => cellChanges;
    public IReadOnlyDictionary<ResourceKey, ResourceStack> ResourceChanges => resourceChanges;
    public IReadOnlyList<DepositDefinition> Deposits => source.Deposits;
    public int ChunkColumns => (configuration.Width + configuration.ChunkSize - 1) / configuration.ChunkSize;
    public ChunkManager(WorldConfiguration configuration, IWorldChunkSource source) { this.configuration = configuration; this.source = source; }
    public int NormalizeX(int x) => (int)(((long)x % configuration.Width + configuration.Width) % configuration.Width);
    public CellPosition Normalize(CellPosition p)
    {
        if (p.Y < 0 || p.Y >= configuration.Height) throw new ArgumentOutOfRangeException(nameof(p), "Limite vertical do mundo.");
        return p with { X = NormalizeX(p.X) };
    }
    public int GlobalIndex(CellPosition position) { var p = Normalize(position); return p.Y * configuration.Width + p.X; }
    public CellPosition Position(int index) => new(index % configuration.Width, index / configuration.Width);
    public int ChunkId(CellPosition p) { p = Normalize(p); return p.Y / configuration.ChunkSize * ChunkColumns + p.X / configuration.ChunkSize; }
    public ChunkRegion Region(CellPosition p)
    {
        p = Normalize(p); var size = configuration.ChunkSize;
        var x = p.X / size * size; var y = p.Y / size * size;
        return new(x, y, Math.Min(size, configuration.Width - x), Math.Min(size, configuration.Height - y));
    }
    public bool IsLoaded(CellPosition p) => loaded.ContainsKey(ChunkId(p));
    public WorldChunk Load(CellPosition position)
    {
        var p = Normalize(position); var id = ChunkId(p);
        if (loaded.TryGetValue(id, out var chunk)) { Touch(id); return chunk; }
        if (loaded.Count == configuration.MaximumLoadedChunks) UnloadById(recent.Last!.Value);
        var region = Region(p); chunk = source.Generate(region);
        for (var i = 0; i < chunk.CellData.Length; i++)
            if (cellChanges.TryGetValue(GlobalIndex(region.Position(i)), out var cell)) chunk.CellData[i] = cell;
        if (resourceChangesByChunk.TryGetValue(id, out var resourcePatches))
            foreach (var (key, stack) in resourcePatches) chunk.Resources.Set(key, stack);
        loaded[id] = chunk; cacheNodes[id] = recent.AddFirst(id); GeneratedCount++;
        return chunk;
    }
    public void Unload(CellPosition p) => UnloadById(ChunkId(p));
    private void UnloadById(int id)
    {
        if (!loaded.Remove(id)) return;
        recent.Remove(cacheNodes[id]); cacheNodes.Remove(id);
    }
    private void Touch(int id) { var node = cacheNodes[id]; recent.Remove(node); recent.AddFirst(node); }
    public WorldCell GetCell(CellPosition position, bool load = true)
    {
        var p = Normalize(position);
        if (cellChanges.TryGetValue(GlobalIndex(p), out var modified)) return modified;
        if (loaded.TryGetValue(ChunkId(p), out var resident)) return resident.CellData[resident.Region.Index(p)];
        if (!load) return source.SampleCell(p);
        var chunk = Load(p); return chunk.CellData[chunk.Region.Index(p)];
    }
    internal void SetCell(CellPosition position, WorldCell cell)
    {
        cell.Validate(); var p = Normalize(position);
        cellChanges[GlobalIndex(p)] = cell;
        if (loaded.TryGetValue(ChunkId(p), out var chunk)) chunk.CellData[chunk.Region.Index(p)] = cell;
    }
    public ResourceStack BaselineResource(CellPosition position, ushort id) => source.SampleResource(Normalize(position), id);
    public ResourceStack GetResource(CellPosition position, ushort id)
    {
        var p = Normalize(position); var key = new ResourceKey(GlobalIndex(p), id);
        return resourceChanges.TryGetValue(key, out var value) ? value : source.SampleResource(p, id);
    }
    internal void SetResource(CellPosition position, ushort id, ResourceStack stack)
    {
        stack.Validate(); var p = Normalize(position);
        resourceChanges[new(GlobalIndex(p), id)] = stack;
        var chunkId = ChunkId(p);
        if (!resourceChangesByChunk.TryGetValue(chunkId, out var changes)) resourceChangesByChunk[chunkId] = changes = [];
        changes[new(Region(p).Index(p), id)] = stack;
        if (loaded.TryGetValue(ChunkId(p), out var chunk)) chunk.Resources.Set(new(chunk.Region.Index(p), id), stack);
    }
    public IEnumerable<ResourceEntry> EnumerateResources()
    {
        var seen = new HashSet<ResourceKey>();
        foreach (var entry in source.EnumerateResources())
        {
            var key = new ResourceKey(GlobalIndex(entry.Position), entry.ResourceId); seen.Add(key);
            var value = resourceChanges.GetValueOrDefault(key, entry.Stack);
            if (value.Capacity > 0) yield return entry with { Stack = value };
        }
        foreach (var (key, stack) in resourceChanges)
            if (!seen.Contains(key) && stack.Capacity > 0) yield return new(Position(key.CellIndex), key.ResourceId, stack);
    }
    public int GetNeighbors(CellPosition origin, Span<CellPosition> destination, bool diagonals = false)
    {
        var expected = diagonals ? 8 : 4;
        if (destination.Length < expected) throw new ArgumentException("Buffer de vizinhos insuficiente.");
        origin = Normalize(origin); var count = 0;
        ReadOnlySpan<CellPosition> offsets = [new(-1, 0), new(1, 0), new(0, -1), new(0, 1), new(-1, -1), new(1, -1), new(-1, 1), new(1, 1)];
        for (var i = 0; i < expected; i++)
        {
            var y = origin.Y + offsets[i].Y;
            if (y < 0 || y >= configuration.Height) continue;
            destination[count++] = new(NormalizeX(origin.X + offsets[i].X), y);
        }
        return count;
    }
}
