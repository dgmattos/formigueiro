using System.Text.Json;
using Godot;
namespace AntWorld.Game;

public static class AssetPreparation
{
    public static void Run()
    {
        var manifest = SpriteManifest.Load(); var metadata = new List<PreparedSprite>();
        var folder = ProjectSettings.GlobalizePath("res://Assets/Prepared"); System.IO.Directory.CreateDirectory(folder);
        using var neutral = Image.LoadFromFile(ProjectSettings.GlobalizePath($"res://Assets/{manifest.Atlases["spring"]}")); neutral.Convert(Image.Format.Rgba8);
        foreach (var (season, file) in manifest.Atlases)
        {
            using var source = Image.LoadFromFile(ProjectSettings.GlobalizePath($"res://Assets/{file}"));
            if (source.GetWidth() != 1536 || source.GetHeight() != 1024) throw new InvalidDataException($"Dimensão inesperada: {file}.");
            source.Convert(Image.Format.Rgba8);
            using var terrain = Image.CreateEmpty(16 * manifest.Sprites.Count(s => s.Category == "terrain"), 16, false, Image.Format.Rgba8);
            using var objects = Image.CreateEmpty(512, 512, false, Image.Format.Rgba8);
            terrain.Fill(Colors.Transparent); objects.Fill(Colors.Transparent);
            var tile = 0; var x = 0; var y = 0; var shelfHeight = 0;
            foreach (var definition in manifest.Sprites)
            {
                var r = definition.Region; var region = new Rect2I(r[0], r[1], r[2], r[3]);
                if (!new Rect2I(0, 0, 1536, 1024).Encloses(region)) throw new InvalidDataException($"Recorte fora da imagem: {definition.Id}.");
                using var crop = (definition.SeasonInvariant ? neutral : source).GetRegion(region);
                if (definition.MirrorX) crop.FlipX();
                if (definition.RemoveBackground) RemoveBackground(crop, manifest.BackgroundMaximum, manifest.BackgroundChroma);
                if (definition.Category == "terrain")
                {
                    if (crop.GetWidth() != crop.GetHeight()) throw new InvalidDataException("Terreno deve ter recorte quadrado validado, sem deformação.");
                    crop.Resize(16, 16, Image.Interpolation.Nearest);
                    terrain.BlitRect(crop, new(0, 0, 16, 16), new(tile * 16, 0));
                    metadata.Add(new(definition.Id, season, [tile++ * 16, 0, 16, 16], [0, 0])); continue;
                }
                if (definition.KeepLargestComponent) KeepLargestComponent(crop);
                if (crop.IsInvisible()) throw new InvalidDataException($"Objeto sem pixels: {definition.Id}/{season}. Corrija o manifesto.");
                var width = definition.VisualWidth * 16; var height = definition.VisualHeight * 16;
                var scale = Math.Min((double)width / crop.GetWidth(), (double)height / crop.GetHeight());
                var sourceAnchor = definition.SeasonAnchors.GetValueOrDefault(season, definition.Anchor);
                var anchor = new Vector2I((int)Math.Round(sourceAnchor[0] * scale), (int)Math.Round(sourceAnchor[1] * scale));
                crop.Resize(Math.Max(1, (int)Math.Round(crop.GetWidth() * scale)), Math.Max(1, (int)Math.Round(crop.GetHeight() * scale)), Image.Interpolation.Nearest);
                if (definition.SnapAnchorToBase) anchor.Y = FindBase(crop, anchor.X);
                if (x + crop.GetWidth() + 2 > 512) { x = 0; y += shelfHeight + 2; shelfHeight = 0; }
                if (y + crop.GetHeight() > 512) throw new InvalidDataException("Atlas preparado excedeu o tamanho.");
                objects.BlitRect(crop, new(0, 0, crop.GetWidth(), crop.GetHeight()), new(x, y));
                metadata.Add(new(definition.Id, season, [x, y, crop.GetWidth(), crop.GetHeight()], [anchor.X, anchor.Y]));
                x += crop.GetWidth() + 2; shelfHeight = Math.Max(shelfHeight, crop.GetHeight());
            }
            Save(terrain, System.IO.Path.Combine(folder, $"terrain-{season}.png"));
            Save(objects, System.IO.Path.Combine(folder, $"objects-{season}.png"));
        }
        System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "sprites.generated.json"), JsonSerializer.Serialize(metadata, SpriteManifest.JsonOptions));
        GD.Print($"AntWorld assets: {metadata.Count} regiões preparadas; nearest, alfa e âncoras preservados.");
    }
    private static void Save(Image image, string path)
    { if (image.SavePng(path) != Error.Ok) throw new IOException($"Falha ao salvar {path}."); }
    private static int FindBase(Image image, int center)
    {
        var radius = Math.Max(1, image.GetWidth() / 6);
        for (var y = image.GetHeight() - 1; y >= 0; y--)
        for (var x = Math.Max(0, center - radius); x <= Math.Min(image.GetWidth() - 1, center + radius); x++)
            if (image.GetPixel(x, y).A > .5f) return y + 1;
        throw new InvalidDataException("Âncora sem apoio visível; corrija o manifesto.");
    }

    private static void RemoveBackground(Image image, int maximum, int chroma)
    {
        // The supplied sheets use a dark, near-neutral background. Thresholds are editable in the manifest.
        for (var y = 0; y < image.GetHeight(); y++) for (var x = 0; x < image.GetWidth(); x++)
        {
            var color = image.GetPixel(x, y); var max = Math.Max(color.R8, Math.Max(color.G8, color.B8)); var min = Math.Min(color.R8, Math.Min(color.G8, color.B8));
            if (max <= maximum && max - min <= chroma) image.SetPixel(x, y, Colors.Transparent);
        }
    }
    private static void KeepLargestComponent(Image image)
    {
        var width = image.GetWidth(); var height = image.GetHeight(); var labels = new int[width * height]; var queue = new Queue<int>();
        var next = 0; var best = 0; var largest = 0;
        for (var i = 0; i < labels.Length; i++)
        {
            if (labels[i] != 0 || image.GetPixel(i % width, i / width).A == 0) continue;
            next++; var count = 0; labels[i] = next; queue.Enqueue(i);
            while (queue.TryDequeue(out var index))
            {
                count++; var px = index % width; var py = index / width;
                for (var dy = -1; dy <= 1; dy++) for (var dx = -1; dx <= 1; dx++)
                {
                    var x = px + dx; var y = py + dy;
                    if (x < 0 || x >= width || y < 0 || y >= height) continue;
                    var neighbor = y * width + x;
                    if (labels[neighbor] != 0 || image.GetPixel(x, y).A == 0) continue;
                    labels[neighbor] = next; queue.Enqueue(neighbor);
                }
            }
            if (count > largest) { largest = count; best = next; }
        }
        for (var i = 0; i < labels.Length; i++) if (labels[i] != best) image.SetPixel(i % width, i / width, Colors.Transparent);
    }
}
