using System.Text;
using AntWorld.Domain;
using AntWorld.WorldGeneration;
namespace AntWorld.Infrastructure;

internal static class WorldSnapshotCodec
{
    public static void Write(BinaryWriter w, WorldMap world)
    {
        var c = world.Configuration; var g = c.Geology;
        w.Write(WorldGenerator.Version); w.Write(c.Seed); w.Write(c.Width); w.Write(c.Height); w.Write(c.ChunkSize); w.Write(c.MaximumLoadedChunks);
        w.Write(g.SurfaceLevel); w.Write(g.SoilDepth); w.Write(g.RockDepth); w.Write(g.CaveStart); w.Write(g.CaveThreshold);
        w.Write(g.OrganicDepth); w.Write(g.MineralDepth); w.Write(g.DepositSpacing); w.Write(g.VeinSteps); w.Write(g.VeinBudget);
        MushroomSettingsCodec.Write(w, c.Mushrooms);
        VegetationSnapshotCodec.WriteSettings(w, c.Vegetation);
        w.Write(c.Nests.NormalBaseDepth); w.Write(c.Nests.NormalDepthMultiplier); w.Write(c.Nests.StrongUndergroundRatio); w.Write(c.Nests.SurfaceDepth);
        NestSnapshotCodec.Write(w,c.NestLayout);
        var custom = world.ResourceCatalog.Definitions.Where(d => d.Id > ResourceIds.BrownMushroom).ToArray();
        if (custom.Length > 1024) throw new InvalidDataException("Limite de catálogo excedido.");
        w.Write(custom.Length);
        foreach (var d in custom)
        {
            w.Write(d.Id); WriteText(w, d.Name); WriteText(w, d.Kind); w.Write(d.Extractable); w.Write(d.Regeneration);
            w.Write(d.Occurrence.BlockMask); w.Write(d.Occurrence.MinimumDepth); w.Write(d.Occurrence.MaximumDepth); w.Write(d.Occurrence.MinimumMoisture);
        }
        var columns = Enumerable.Range(0, world.Width).Where(x => world.Moisture[x] != BiomeProfile.For(world.Biomes[x]).Humidity).ToArray();
        w.Write(columns.Length);
        foreach (var x in columns) { w.Write(x); w.Write(world.Moisture[x]); }
        w.Write(world.ChunkManager.CellChanges.Count);
        foreach (var (index, cell) in world.ChunkManager.CellChanges.OrderBy(p => p.Key)) { w.Write(index); WriteCell(w, cell); }
        w.Write(world.ChunkManager.ResourceChanges.Count);
        foreach (var (key, stack) in world.ChunkManager.ResourceChanges.OrderBy(p => p.Key.CellIndex).ThenBy(p => p.Key.ResourceId))
        {
            w.Write(key.CellIndex); w.Write(key.ResourceId); w.Write(stack.Quantity); w.Write(stack.Capacity); w.Write(stack.Concentration); w.Write(stack.DepositId);
        }
        VegetationSnapshotCodec.WriteChanges(w, world.Vegetation);
    }
    public static WorldMap Read(BinaryReader r, int version)
    {
        if (r.ReadInt32() != WorldGenerator.Version) throw new InvalidDataException("Versão do gerador incompatível; não é seguro reconstruir a seed.");
        var c = new WorldConfiguration(r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32()) { MaximumLoadedChunks = r.ReadInt32(),
            Geology = new(r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadInt32(), r.ReadInt32(), r.ReadDouble()) };
        c = c with { Mushrooms = MushroomSettingsCodec.Read(r) };
        c = c with { Vegetation = VegetationSnapshotCodec.ReadSettings(r) };
        c = c with { Nests = new(r.ReadInt32(), r.ReadDouble(), r.ReadDouble(), version >= 6 ? r.ReadInt32() : 2) };
        if(version>=10)c=c with {NestLayout=NestSnapshotCodec.Read<NestLayoutSettings>(r)};
        var world = new WorldGenerator().Generate(c);
        var customCount = Count(r, 1024);
        for (var i = 0; i < customCount; i++)
            world.ResourceCatalog.Register(new(r.ReadUInt16(), ReadText(r), ReadText(r), r.ReadBoolean(), r.ReadDouble(), new(r.ReadByte(), r.ReadDouble(), r.ReadDouble(), r.ReadByte())));
        var columnCount = Count(r, world.Width); var columns = new HashSet<int>();
        for (var i = 0; i < columnCount; i++)
        {
            var x = Index(r, world.Width);
            if (!columns.Add(x)) throw new InvalidDataException("Coluna duplicada.");
            world.Moisture[x] = r.ReadDouble();
        }
        var cellCount = Count(r, world.CellCount);
        if ((long)cellCount * 20 > r.BaseStream.Length - r.BaseStream.Position) throw new InvalidDataException("Células truncadas.");
        var cells = new HashSet<int>();
        for (var i = 0; i < cellCount; i++)
        {
            var index = Index(r, world.CellCount);
            if (!cells.Add(index)) throw new InvalidDataException("Célula duplicada.");
            var cell = ReadCell(r);
            if (!cell.Flags.HasFlag(CellFlags.Modified)) throw new InvalidDataException("Alteração sem flag de modificação.");
            world.SetCell(world.Position(index), cell);
        }
        var resourceCount = Count(r, BinarySnapshotCodec.MaximumBytes / 28);
        if ((long)resourceCount * 28 > r.BaseStream.Length - r.BaseStream.Position) throw new InvalidDataException("Recursos truncados.");
        var resources = new HashSet<ResourceKey>();
        var patches = new ResourceEntry[resourceCount];
        for (var i = 0; i < resourceCount; i++)
        {
            var index = Index(r, world.CellCount); var id = r.ReadUInt16();
            if (!resources.Add(new(index, id))) throw new InvalidDataException("Recurso duplicado.");
            patches[i] = new(world.Position(index), id, new(r.ReadDouble(), r.ReadDouble(), r.ReadUInt16(), r.ReadInt32()));
        }
        // Apply removals before births: a full chunk may have changed all occupied mushroom sites.
        foreach (var entry in patches)
            if (entry.Stack.Quantity == 0) world.RestoreResourceChange(entry.Position, entry.ResourceId, entry.Stack);
        foreach (var entry in patches)
            if (entry.Stack.Quantity != 0) world.RestoreResourceChange(entry.Position, entry.ResourceId, entry.Stack);
        VegetationSnapshotCodec.ReadChanges(r, world);
        return world;
    }
    private static void WriteCell(BinaryWriter w, WorldCell c)
    {
        w.Write(c.SolidFill); w.Write(c.RockFraction); w.Write(c.SoilMaterial); w.Write(c.RockMaterial);
        w.Write(c.Moisture); w.Write(c.WaterAmount); w.Write(c.Temperature); w.Write(c.Porosity); w.Write(c.Permeability); w.Write(c.Nutrients); w.Write((byte)c.Flags);
    }
    private static WorldCell ReadCell(BinaryReader r) => new() { SolidFill = r.ReadByte(), RockFraction = r.ReadByte(), SoilMaterial = r.ReadUInt16(), RockMaterial = r.ReadUInt16(),
        Moisture = r.ReadByte(), WaterAmount = r.ReadUInt16(), Temperature = r.ReadInt16(), Porosity = r.ReadByte(), Permeability = r.ReadByte(), Nutrients = r.ReadUInt16(), Flags = (CellFlags)r.ReadByte() };
    public static int Count(BinaryReader r, int limit)
    {
        var count = r.ReadInt32(); return count >= 0 && count <= limit ? count : throw new InvalidDataException("Contagem inválida.");
    }
    public static int Index(BinaryReader r, int length)
    {
        var index = r.ReadInt32(); return index >= 0 && index < length ? index : throw new InvalidDataException("Índice inválido.");
    }
    private static void WriteText(BinaryWriter w, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value); w.Write((ushort)bytes.Length); w.Write(bytes);
    }
    private static string ReadText(BinaryReader r)
    {
        var count = r.ReadUInt16(); if (count > 320) throw new InvalidDataException("Texto excede limite.");
        var bytes = r.ReadBytes(count); if (bytes.Length != count) throw new EndOfStreamException();
        return Encoding.UTF8.GetString(bytes);
    }
}
