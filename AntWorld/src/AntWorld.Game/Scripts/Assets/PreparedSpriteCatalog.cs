using System.Text.Json;
using AntWorld.Domain;
using AntWorld.WorldGeneration;
using Godot;
namespace AntWorld.Game;

public sealed class PreparedSpriteCatalog
{
    public static readonly string[] Seasons = ["spring", "summer", "autumn", "winter"];
    private readonly SpriteManifest manifest = SpriteManifest.Load();
    private readonly Dictionary<(string, string), PreparedSprite> sprites;
    private readonly Dictionary<TerrainVisualType, SpriteDefinition[]> terrain;
    private readonly Dictionary<VegetationKind, string> vegetation;
    private readonly Texture2D[] objects = new Texture2D[4];
    private readonly TileSet[] tileSets = new TileSet[4];
    public PreparedSpriteCatalog()
    {
        var json = System.IO.File.ReadAllText(ProjectSettings.GlobalizePath("res://Assets/Prepared/sprites.generated.json"));
        var entries = JsonSerializer.Deserialize<PreparedSprite[]>(json, SpriteManifest.JsonOptions) ?? throw new InvalidDataException("Execute a preparação dos assets.");
        sprites = entries.ToDictionary(s => (s.Season, s.Id));
        terrain = manifest.Sprites.Where(s => s.Terrain.HasValue).GroupBy(s => s.Terrain!.Value).ToDictionary(g => g.Key, g => g.OrderBy(s => s.Variation).ToArray());
        vegetation = manifest.Sprites.Where(s => s.Kind.HasValue).ToDictionary(s => s.Kind!.Value, s => s.Id);
        for (var season = 0; season < 4; season++)
        {
            objects[season] = GD.Load<Texture2D>($"res://Assets/Prepared/objects-{Seasons[season]}.png");
            tileSets[season] = CreateTileSet(season);
        }
    }
    public TileSet TileSet(int season) => tileSets[season];
    public Vector2I TerrainCoordinates(TerrainAppearance appearance, int season, byte solidFill)
    {
        var variants = terrain[appearance.Type];
        var chosen = variants[0];
        if (appearance.Type == TerrainVisualType.SoilRockTransition) chosen = variants[Math.Min(variants.Length - 1, appearance.Variation)];
        else
        {
            var pick = (appearance.Variation + .5) / 6 * variants.Sum(v => v.Weight);
            foreach (var candidate in variants) { pick -= candidate.Weight; if (pick <= 0) { chosen = candidate; break; } }
        }
        var sprite = sprites[(Seasons[season], chosen.Id)];
        return new(sprite.Region[0] / 16, Math.Clamp((int)Math.Round(solidFill * 16 / 255.0), 1, 16) - 1);
    }
    public PreparedSprite Object(VegetationKind kind, int season) => Object(vegetation[kind], season);
    public PreparedSprite Object(string id, int season) => sprites[(Seasons[season], id)];
    public Texture2D ObjectTexture(int season) => objects[season];
    public static Rect2 Destination(PreparedSprite sprite, Vector2 anchor, int tileSize, float scale = 1)
    {
        var factor = tileSize / 16f * scale;
        return new((anchor - new Vector2(sprite.Anchor[0], sprite.Anchor[1]) * factor).Round(),
            new Vector2(sprite.Region[2] * factor, sprite.Region[3] * factor).Round());
    }
    public void Draw(CanvasItem canvas, PreparedSprite sprite, int season, Vector2 anchor, int tileSize, float scale = 1)
    {
        var r = sprite.Region;
        canvas.DrawTextureRectRegion(objects[season], Destination(sprite, anchor, tileSize, scale), new(r[0], r[1], r[2], r[3]));
    }
    private TileSet CreateTileSet(int season)
    {
        using var image = GD.Load<Texture2D>($"res://Assets/Prepared/terrain-{Seasons[season]}.png").GetImage();
        using var fills = Image.CreateEmpty(image.GetWidth(), 16 * 16, false, Image.Format.Rgba8); fills.Fill(Colors.Transparent);
        for (var fill = 1; fill <= 16; fill++)
        for (var x = 0; x < image.GetWidth(); x += 16)
            fills.BlitRect(image, new(x, 0, 16, fill), new(x, (fill - 1) * 16 + 16 - fill));
        var source = new TileSetAtlasSource { Texture = ImageTexture.CreateFromImage(fills), TextureRegionSize = new(16, 16), Margins = Vector2I.Zero, Separation = Vector2I.Zero, UseTexturePadding = true };
        var result = new TileSet { TileSize = new(16, 16) }; result.AddSource(source, 0);
        for (var y = 0; y < 16; y++) for (var x = 0; x < image.GetWidth() / 16; x++)
        { source.CreateTile(new(x, y)); source.GetTileData(new(x, y), 0).TextureOrigin = Vector2I.Zero; }
        return result;
    }
    public void Validate()
    {
        for (var season = 0; season < 4; season++)
        {
            using var image = objects[season].GetImage();
            if (image.DetectAlpha() == Image.AlphaMode.None) throw new InvalidDataException("Objetos sem transparência.");
            foreach (var definition in manifest.Sprites.Where(s => s.Category == "object"))
            {
                var sprite = Object(definition.Id, season); var r = sprite.Region;
                using var part = image.GetRegion(new(r[0], r[1], r[2], r[3]));
                if (part.IsInvisible() || part.DetectAlpha() == Image.AlphaMode.None) throw new InvalidDataException($"Objeto vazio/opaco: {definition.Id}/{season}.");
            }
        }
    }
}
