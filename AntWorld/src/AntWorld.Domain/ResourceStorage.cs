namespace AntWorld.Domain;

// One sparse table per chunk; no collection is allocated inside a cell.
public sealed class ResourceStorage
{
    private readonly Dictionary<ResourceKey, ResourceStack> entries = [];
    public IEnumerable<KeyValuePair<ResourceKey, ResourceStack>> Entries => entries;
    public int Count => entries.Count;
    public ResourceStack Get(ResourceKey key) => entries.GetValueOrDefault(key);
    public void Set(ResourceKey key, ResourceStack value)
    {
        value.Validate();
        if (value.Capacity == 0) entries.Remove(key); else entries[key] = value;
    }
}
