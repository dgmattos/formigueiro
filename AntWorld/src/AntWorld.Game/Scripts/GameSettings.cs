using AntWorld.Simulation;
using AntWorld.Infrastructure;
namespace AntWorld.Game;

public sealed record GameSettings(PlaybackSettings Playback, WindowSettings Window, SimulationPreset Simulation, RenderingSettings Rendering, SimulationLogSettings Diagnostics);
