using AntWorld.Domain;
using AntWorld.WorldGeneration;
using Godot;
namespace AntWorld.Game;

// CPU inspection artifact, using the same prepared textures, tile selection and anchors as the scene.
public static class VisualPreviewExporter
{
    public static void Export(WorldMap world, PreparedSpriteCatalog catalog, string directory)
    {
        System.IO.Directory.CreateDirectory(directory);
        var focus = world.Vegetation.All.FirstOrDefault(v => v.Kind == VegetationKind.TreeLarge).Base;
        var width = Math.Min(80, world.Width); var height = Math.Min(40, world.Height);
        var left = Math.Clamp(focus.X - 25, 0, world.Width - width); var top = Math.Clamp(focus.Y - 14, 0, world.Height - height);
        for (var season = 0; season < 4; season++)
        {
            using var preview = Image.CreateEmpty(width * 16, height * 16, false, Image.Format.Rgba8); preview.Fill(new("90b5c7"));
            using var terrain = ((TileSetAtlasSource)catalog.TileSet(season).GetSource(0)).Texture.GetImage();
            using var objects = catalog.ObjectTexture(season).GetImage();
            for (var y = top; y < top + height; y++) for (var x = left; x < left + width; x++)
            {
                var p = new CellPosition(x, y); var cell = world.GetCell(p, false);
                if (cell.SolidFill == 0) continue;
                var tile = catalog.TerrainCoordinates(TerrainAppearanceResolver.Resolve(world, p), season, cell.SolidFill);
                preview.BlendRect(terrain, new(tile.X * 16, tile.Y * 16, 16, 16), new((x - left) * 16, (y - top) * 16));
            }
            foreach (var instance in world.Vegetation.QueryColumns(world, left, left + width))
            {
                var sprite = catalog.Object(instance.Kind, season); var r = sprite.Region;
                var anchor = new Vector2((instance.Base.X - left + .5f) * 16, (instance.Base.Y - top) * 16);
                var rect = PreparedSpriteCatalog.Destination(sprite, anchor, 16);
                preview.BlendRect(objects, new(r[0], r[1], r[2], r[3]), (Vector2I)rect.Position);
            }
            var path = System.IO.Path.Combine(directory, $"world-{PreparedSpriteCatalog.Seasons[season]}.png");
            if (preview.SavePng(path) != Error.Ok) throw new IOException($"Falha no preview {path}.");
        }
    }
}
