using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
namespace AntWorld.Game;
public sealed record SpriteManifest
{
    public int Version { get; init; }
    public int TilePixels { get; init; }
    public int BackgroundMaximum { get; init; }
    public int BackgroundChroma { get; init; }
    public Dictionary<string, string> Atlases { get; init; } = [];
    public SpriteDefinition[] Sprites { get; init; } = [];
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter() }, WriteIndented = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    public static SpriteManifest Load()
    {
        using var file = Godot.FileAccess.Open("res://Assets/sprites.manifest.json", Godot.FileAccess.ModeFlags.Read);
        var json = file?.GetAsText() ?? throw new InvalidDataException("Asset não encontrado: Assets/sprites.manifest.json");
        var manifest = JsonSerializer.Deserialize<SpriteManifest>(json, JsonOptions) ?? throw new InvalidDataException("Manifesto vazio.");
        if (manifest.Version != 1 || manifest.TilePixels != 16 || manifest.Atlases.Count != 4 || manifest.Sprites.Select(s => s.Id).Distinct().Count() != manifest.Sprites.Length)
            throw new InvalidDataException("Manifesto visual incompatível.");
        foreach (var sprite in manifest.Sprites)
            if (sprite.Region.Length != 4 || sprite.Anchor.Length != 2 || sprite.Region[2] <= 0 || sprite.Region[3] <= 0 || sprite.VisualWidth <= 0 || sprite.VisualHeight <= 0)
                throw new InvalidDataException($"Região inválida: {sprite.Id}.");
        return manifest;
    }
}
