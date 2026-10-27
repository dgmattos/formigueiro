namespace AntWorld.Domain;

public static class ExcavationSpoil
{
    public const string Kind = "excavation_spoil";
    public static bool Is(WorldMap world, ushort id) => world.ResourceCatalog.Get(id).Kind == Kind;
    public static ushort ResourceId(WorldMap world)
    {
        var existing = world.ResourceCatalog.Definitions.FirstOrDefault(d => d.Kind == Kind);
        if (existing is not null) return existing.Id;
        var candidate = (int)ResourceIds.BrownMushroom + 1;
        foreach (var definition in world.ResourceCatalog.Definitions)
        {
            if (definition.Id < candidate) continue;
            if (definition.Id > candidate) break;
            candidate++;
        }
        var id = checked((ushort)candidate);
        world.ResourceCatalog.Register(new(id, "Entulho de escavação", Kind, true, 0, new(31)));
        return id;
    }
    public static CellPosition Exit(WorldMap world, Colony colony) => new(colony.Nest.X, world.Surface[colony.Nest.X] - 1);
    public static void Drop(WorldMap world, CellPosition position, double amount)
    {
        var id = ResourceId(world);
        var old = world.GetResource(position, id);
        var total = old.Quantity + amount;
        world.SetResource(position, id, new(total, Math.Max(old.Capacity, total), 10000));
    }
}
