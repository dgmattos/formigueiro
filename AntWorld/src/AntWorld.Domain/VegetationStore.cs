namespace AntWorld.Domain;

public sealed class VegetationStore
{
    private readonly Dictionary<int, VegetationInstance> baseline = [];
    private readonly Dictionary<int, VegetationInstance> changes = [];
    private readonly Dictionary<int, int> byColumn = [];
    public IReadOnlyDictionary<int, VegetationInstance> Changes => changes;
    public int Revision { get; private set; }
    public IEnumerable<VegetationInstance> All => baseline.Values.Select(v => changes.GetValueOrDefault(v.Id, v)).Where(v => !v.Removed);
    public void Initialize(IEnumerable<VegetationInstance> instances)
    {
        if (baseline.Count != 0 || changes.Count != 0) throw new InvalidOperationException("Plano vegetal já inicializado.");
        foreach (var instance in instances) { baseline.Add(instance.Id, instance); byColumn.Add(instance.Base.X, instance.Id); }
    }
    public VegetationInstance Get(int id) => changes.GetValueOrDefault(id, baseline.GetValueOrDefault(id));
    public void Set(VegetationInstance instance)
    {
        if (!baseline.TryGetValue(instance.Id, out var original) || original.Base != instance.Base || original.Biome != instance.Biome ||
            !Enum.IsDefined(instance.Kind) || !float.IsFinite(instance.Health) || instance.Health is < 0 or > 100 ||
            instance.BornTick < 0 || instance.InitialAgeTicks < 0) throw new ArgumentException("Alteração vegetal inválida.");
        if (Get(instance.Id) == instance) return;
        changes[instance.Id] = instance; Revision++;
    }
    public void Damage(int id, float damage)
    {
        if (!float.IsFinite(damage) || damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
        var instance = Get(id);
        if (instance.Id == 0 || instance.Removed) return;
        var health = Math.Max(0, instance.Health - damage);
        Set(health == 0 && instance.Definition.IsTree
            ? instance with { Kind = VegetationKind.TreeStump, Health = 100 }
            : instance with { Health = health, Removed = health == 0 });
    }
    public IEnumerable<VegetationInstance> QueryColumns(WorldMap world, int first, int last, int margin = 6)
    {
        var count = Math.Min(world.Width, Math.Max(0, last - first + 1 + margin * 2));
        for (var i = 0; i < count; i++)
        {
            var x = world.ChunkManager.NormalizeX(first - margin + i);
            if (!byColumn.TryGetValue(x, out var id)) continue;
            var instance = Get(id); if (!instance.Removed) yield return instance;
        }
    }
    public void InvalidateAt(WorldMap world, CellPosition changed)
    {
        foreach (var instance in QueryColumns(world, changed.X, changed.X))
        {
            var dx = Math.Abs(world.HorizontalDelta(instance.Base.X, changed.X));
            var above = changed.Y < instance.Base.Y && changed.Y >= instance.Base.Y - instance.Definition.VisualHeight && dx <= instance.Definition.VisualWidth / 2;
            var support = changed.Y == instance.Base.Y && dx <= instance.Definition.SupportWidth / 2;
            if (!(above || support)) continue;
            var cell = world.GetCell(changed, false);
            if (above && !cell.IsPassable || support && (cell.IsPassable || cell.WaterAmount >= 200 || cell.Flags.HasFlag(CellFlags.VegetationRemoved))) Set(instance with { Removed = true });
        }
    }
    public bool BlocksMovement(WorldMap world, CellPosition p)
    {
        if (!world.Configuration.Vegetation.WoodBlocksMovement) return false;
        foreach (var instance in QueryColumns(world, p.X, p.X, 2))
        {
            var definition = instance.Definition;
            if (definition.CollisionWidth > 0 && p.Y == instance.Base.Y - 1 &&
                Math.Abs(world.HorizontalDelta(p.X, instance.Base.X)) <= definition.CollisionWidth / 2) return true;
        }
        return false;
    }
}
