using AntWorld.Domain;
namespace AntWorld.Infrastructure;
internal static class VegetationSnapshotCodec
{
    public static void WriteSettings(BinaryWriter w, VegetationSettings s)
    {
        w.Write(s.Enabled); w.Write(s.WoodBlocksMovement); w.Write(s.MinimumMoisture); w.Write(s.MinimumTemperature); w.Write(s.MaximumTemperature); w.Write(s.MinimumTreeSpacing);
        foreach (var biome in Enum.GetValues<BiomeKind>())
        {
            var p = s.For(biome);
            foreach (var kind in Enum.GetValues<VegetationKind>()) w.Write(p.Density(kind));
            w.Write(p.GrassCoverage);
        }
    }
    public static VegetationSettings ReadSettings(BinaryReader r) => new()
    {
        Enabled = r.ReadBoolean(), WoodBlocksMovement = r.ReadBoolean(), MinimumMoisture = r.ReadByte(), MinimumTemperature = r.ReadDouble(), MaximumTemperature = r.ReadDouble(), MinimumTreeSpacing = r.ReadInt32(),
        Forest = ReadProfile(r), Grassland = ReadProfile(r), Desert = ReadProfile(r), Swamp = ReadProfile(r), Tundra = ReadProfile(r)
    };
    private static BiomeVegetationSettings ReadProfile(BinaryReader r) => new(r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(),
        r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble());
    public static void WriteChanges(BinaryWriter w, VegetationStore store)
    {
        w.Write(store.Changes.Count);
        foreach (var v in store.Changes.Values.OrderBy(v => v.Id))
        {
            w.Write(v.Id); w.Write((byte)v.Kind); w.Write(v.Base.X); w.Write(v.Base.Y); w.Write((byte)v.Biome);
            w.Write(v.BornTick); w.Write(v.InitialAgeTicks); w.Write(v.Health); w.Write(v.Removed);
        }
    }
    public static void ReadChanges(BinaryReader r, WorldMap world)
    {
        var count = WorldSnapshotCodec.Count(r, world.Width); var ids = new HashSet<int>();
        for (var i = 0; i < count; i++)
        {
            var v = new VegetationInstance(r.ReadInt32(), (VegetationKind)r.ReadByte(), new(r.ReadInt32(), r.ReadInt32()),
                (BiomeKind)r.ReadByte(), r.ReadInt64(), r.ReadInt64(), r.ReadSingle(), r.ReadBoolean());
            if (!ids.Add(v.Id)) throw new InvalidDataException("Identificador vegetal duplicado.");
            world.Vegetation.Set(v);
        }
    }
}
