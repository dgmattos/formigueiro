using AntWorld.Domain;
namespace AntWorld.Infrastructure;

internal static class MushroomSettingsCodec
{
    public static void Write(BinaryWriter w, MushroomSettings settings)
    {
        w.Write(settings.UndergroundRedChance); w.Write(settings.UndergroundBrownChance);
        w.Write(settings.SurfaceRedChance); w.Write(settings.SurfaceBrownChance); w.Write(settings.AdjacentBonus);
        w.Write(settings.RenewalIntervalSeconds); w.Write(settings.MinimumPerChunk); w.Write(settings.MaximumPerChunk);
        w.Write(settings.FoodPerMushroom);
    }
    public static MushroomSettings Read(BinaryReader r) => new()
    {
        UndergroundRedChance = r.ReadDouble(), UndergroundBrownChance = r.ReadDouble(),
        SurfaceRedChance = r.ReadDouble(), SurfaceBrownChance = r.ReadDouble(), AdjacentBonus = r.ReadDouble(),
        RenewalIntervalSeconds = r.ReadInt32(), MinimumPerChunk = r.ReadInt32(), MaximumPerChunk = r.ReadInt32(),
        FoodPerMushroom = r.ReadDouble()
    };
}
