using AntWorld.Simulation;
using Godot;
namespace AntWorld.Game;
public static class GameSettingsLoader
{
    public const string SettingsPath="res://settings.json";
    public static GameSettings Load()
    {
        using var file=Godot.FileAccess.Open(SettingsPath,Godot.FileAccess.ModeFlags.Read);
        if(file is null) throw new System.IO.IOException($"Não foi possível ler {SettingsPath}: {Godot.FileAccess.GetOpenError()}.");
        var json = file.GetAsText();
        return new(PlaybackSettings.Parse(json), WindowSettings.Parse(json), SimulationPreset.Parse(json), RenderingSettings.Parse(json));
    }
}
