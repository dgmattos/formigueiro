using System.Text.Json;
using Godot;
namespace AntWorld.Game;

public sealed record WindowSettings(string Mode, int Width, int Height)
{
    public static WindowSettings Default { get; } = new("windowed", 1440, 900);

    public static WindowSettings Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("window", out var window)) return Default;
            var settings = new WindowSettings(window.GetProperty("mode").GetString() ?? "",
                window.GetProperty("width").GetInt32(), window.GetProperty("height").GetInt32());
            settings.Validate();
            return settings;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or OverflowException)
        {
            throw new ArgumentException("settings.json inválido: window exige mode como texto e width/height como inteiros em pixels.", ex);
        }
    }

    public void Validate()
    {
        if (Mode is not ("windowed" or "fullscreen"))
            throw new ArgumentException("settings.json: window.mode deve ser windowed ou fullscreen.");
        if (Width is < 320 or > 16384 || Height is < 240 or > 16384)
            throw new ArgumentException("settings.json: window.width deve estar entre 320 e 16384, e window.height entre 240 e 16384 pixels.");
    }

    public void Apply(Window window)
    {
        Validate();
        window.Mode = Window.ModeEnum.Windowed;
        window.Size = new(Width, Height);
        if (Mode == "fullscreen") window.Mode = Window.ModeEnum.Fullscreen;
    }
}
