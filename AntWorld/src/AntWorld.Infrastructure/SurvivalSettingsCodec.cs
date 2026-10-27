using AntWorld.Domain;
namespace AntWorld.Infrastructure;
internal static class SurvivalSettingsCodec
{
    public static void Write(BinaryWriter w, SurvivalSettings s)
    {
        w.Write(s.EnergyPerDay); w.Write(s.NutritionPerDay); w.Write(s.HydrationPerDay);
        w.Write(s.ThermalDamagePerDegreeDay); w.Write(s.ExhaustionDamagePerDay);
        w.Write(s.StarvationDamagePerDay); w.Write(s.DehydrationDamagePerDay);
        w.Write(s.MinimumEggIntervalDays); w.Write(s.FoodReservePerIndividual); w.Write(s.FoodHydration);
    }
    public static SurvivalSettings Read(BinaryReader r)
    {
        var s = new SurvivalSettings(r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(),
            r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble());
        s.Validate(); return s;
    }
}
