using AntWorld.Domain;
namespace AntWorld.WorldGeneration;

public static class TerrainAppearanceResolver
{
    public static bool HasGrass(WorldMap world, CellPosition position)
    {
        if (!world.Contains(position)) return false;
        var p = world.Normalize(position); var cell = world.GetCell(p, false);
        var settings = world.Configuration.Vegetation;
        return settings.Enabled && !cell.Flags.HasFlag(CellFlags.VegetationRemoved) && !cell.IsPassable && cell.RockFraction < 128 && cell.SoilMaterial != MaterialCatalog.Sand &&
            cell.Moisture >= settings.MinimumMoisture && cell.WaterAmount < 200 && p.Y == world.TopSolidY(p.X) &&
            p.Y > 0 && world.GetCell(new(p.X, p.Y - 1), false).WaterAmount < 200 &&
            SpatialNoise.Unit(world.Configuration.Seed, p.X, p.Y, 80) < settings.For(world.Biomes[p.X]).GrassCoverage;
    }
    public static TerrainAppearance Resolve(WorldMap world, CellPosition p)
    {
        p = world.Normalize(p); var cell = world.GetCell(p, false);
        if (cell.SolidFill == 0) return new(TerrainVisualType.Air, 0, ExposedSides.None);
        var edges = world.Exposure(p);
        var variation = (int)(SpatialNoise.Unit(world.Configuration.Seed, p.X, p.Y, 81) * 6);
        if (cell.RockFraction == 255) return new(TerrainVisualType.Rock, variation, edges);
        if (HasGrass(world, p))
        {
            var left = world.IsOpen(new(p.X - 1, p.Y)); var right = world.IsOpen(new(p.X + 1, p.Y));
            return new(left && right ? TerrainVisualType.GrassCorner : left ? TerrainVisualType.GrassLeftEdge : right ? TerrainVisualType.GrassRightEdge : TerrainVisualType.GrassTop, variation, edges);
        }
        if (cell.RockFraction > 0) return new(TerrainVisualType.SoilRockTransition, Math.Min(3, cell.RockFraction / 64), edges);
        if (cell.SoilMaterial == MaterialCatalog.Sand)
        {
            if (p.Y == world.TopSolidY(p.X)) return new(TerrainVisualType.SandTop, variation, edges);
            var above = p.Y > 0 ? world.GetCell(new(p.X, p.Y - 1), false) : default;
            return new(above.SolidFill > 0 && above.SoilMaterial != MaterialCatalog.Sand ? TerrainVisualType.SoilSandTransition : TerrainVisualType.Sand, variation, edges);
        }
        return new(TerrainVisualType.Soil, variation, edges);
    }
}
