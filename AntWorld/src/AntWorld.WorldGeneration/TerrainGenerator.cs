using AntWorld.Domain;
namespace AntWorld.WorldGeneration;

public sealed class TerrainGenerator
{
    public int[] CreateSurface(WorldConfiguration configuration)
    {
        var phase = DeterministicRandom.FromSeed(configuration.Seed, 2).NextDouble() * Math.Tau;
        // Integer harmonics close the cylinder. Amplitude bounds keep adjacent slopes <= 1 cell.
        var amplitude = Math.Min(configuration.Height * .05, configuration.Width / 35.0);
        return Enumerable.Range(0, configuration.Width).Select(x => (int)(configuration.Height * configuration.Geology.SurfaceLevel +
            Math.Sin(x * Math.Tau / configuration.Width * 2 + phase) * amplitude +
            Math.Sin(x * Math.Tau / configuration.Width * 5 + phase) * amplitude * .23)).ToArray();
    }
    public WorldCell Sample(WorldConfiguration configuration, BiomeKind biome, int surface, CellPosition position)
    {
        var profile = BiomeProfile.For(biome);
        var temperature = (short)Math.Round(profile.Temperature * 10);
        var depth = (double)(position.Y - surface) / (configuration.Height - surface);
        if (depth < 0) return new() { Temperature = temperature };
        var settings = configuration.Geology;
        var phase = SpatialNoise.Unit(configuration.Seed, 0, 0, 2) * Math.Tau;
        var angle = position.X * Math.Tau / configuration.Width;
        var cavity = Math.Sin(angle * Math.Max(1, (int)Math.Round(configuration.Width / 37.0)) + phase) *
            Math.Cos(position.Y * .23 - phase) + .3 * Math.Sin(angle * Math.Max(1, (int)Math.Round(configuration.Width / 20.0)) + position.Y * .19);
        if (depth > settings.CaveStart && depth < .96 && cavity > settings.CaveThreshold)
        {
            // Coherent wet pockets in underground voids; zero material moisture.
            var water = profile.Humidity > .65 && Math.Sin(angle * 3 + phase) + Math.Cos(depth * 16) > 1.3 ? (ushort)400 : (ushort)0;
            return new() { Temperature = temperature, WaterAmount = water };
        }
        var soilDepth = Math.Min(settings.RockDepth * .95, settings.SoilDepth * (biome == BiomeKind.Grassland ? 1.2 : biome == BiomeKind.Desert ? .75 : 1));
        var blend = Math.Clamp((depth - soilDepth) / (settings.RockDepth - soilDepth), 0, 1);
        var rock = (byte)Math.Round(blend * 255);
        var soilId = biome == BiomeKind.Desert ? MaterialCatalog.Sand : depth > soilDepth * .7 ? MaterialCatalog.Clay : MaterialCatalog.Loam;
        var soil = MaterialCatalog.Get(soilId); var stone = MaterialCatalog.Get(MaterialCatalog.Granite);
        var fill = (byte)255;
        if (position.Y == surface)
        {
            // Most exposed soil is solid substrate; retain partial edges without making the entire surface AirAndSoil.
            var variation = SpatialNoise.Unit(configuration.Seed, position.X, position.Y, 5);
            if (variation < .25) fill = (byte)(145 + variation * 360);
        }
        if (rock == 0 && depth > settings.CaveStart && cavity > settings.CaveThreshold - .06) fill = 190;
        var porosity = (byte)Math.Clamp(soil.Porosity * (1 - blend) + stone.Porosity * blend + Math.Sin(angle * 4 + depth * 12) * 12, 1, 255);
        var moisture = (byte)Math.Clamp(Math.Round(profile.Humidity * 100 * (1 - .65 * blend)), 0, 100);
        return new() { SolidFill = fill, RockFraction = rock, SoilMaterial = soilId, RockMaterial = MaterialCatalog.Granite,
            Moisture = moisture, WaterAmount = biome == BiomeKind.Swamp && depth < .035 ? (ushort)180 : (ushort)0,
            Temperature = temperature, Porosity = porosity, Permeability = (byte)(soil.Permeability * (1 - blend) + stone.Permeability * blend),
            Nutrients = (ushort)Math.Round(1000 * profile.Fertility * Math.Max(0, 1 - depth / settings.OrganicDepth)) };
    }
}
