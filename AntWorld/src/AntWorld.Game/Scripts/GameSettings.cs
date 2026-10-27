using AntWorld.Simulation;
namespace AntWorld.Game;

public sealed record GameSettings(PlaybackSettings Playback, WindowSettings Window, SimulationPreset Simulation, RenderingSettings Rendering);
