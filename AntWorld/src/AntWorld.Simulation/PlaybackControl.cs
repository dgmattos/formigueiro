namespace AntWorld.Simulation;
public sealed class PlaybackControl
{
    public PlaybackSettings Settings { get; }
    public SimulationClock Clock { get; }
    public int SelectedMultiplier { get; private set; }
    public PlaybackControl(PlaybackSettings? settings=null)
    {
        Settings=settings??PlaybackSettings.Default;Settings.Validate();
        Clock=new(Settings);SelectedMultiplier=Settings.X1;
    }
    public bool IsPaused => Clock.Speed==0;
    public void TogglePause() => Clock.Speed=IsPaused?SelectedMultiplier:0;
    public void Select(int multiplier)
    {
        if(!Settings.Multipliers.Contains(multiplier)) throw new ArgumentOutOfRangeException(nameof(multiplier));
        SelectedMultiplier=multiplier;
        if(!IsPaused) Clock.Speed=multiplier;
    }
}
