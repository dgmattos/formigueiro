using System.Text.Json;
namespace AntWorld.Game;
public sealed record RenderingSettings(int TileSize = 16)
{
    public static RenderingSettings Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var settings = doc.RootElement.TryGetProperty("rendering", out var section)
            ? section.Deserialize<RenderingSettings>(SpriteManifest.JsonOptions) ?? throw new ArgumentException("rendering inválido.") : new();
        if (settings.TileSize is < 16 or > 64 || settings.TileSize % 16 != 0) throw new ArgumentException("rendering.tileSize deve ser múltiplo de 16, entre 16 e 64.");
        return settings;
    }
}
