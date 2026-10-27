using System.Diagnostics;
namespace AntWorld.Simulation;

public sealed class SimulationClock
{
    private double accumulatedSeconds;
    private int speed = 1;
    public const double TickSeconds = AntWorld.Domain.SimulationState.TickSeconds;
    public IReadOnlyList<int> Speeds { get; }
    public SimulationClock(PlaybackSettings? settings=null)
    {
        settings??=PlaybackSettings.Default;settings.Validate();
        Speeds=Array.AsReadOnly(new[]{0,settings.X1,settings.X2,settings.X3});speed=settings.X1;
    }
    public int Speed
    {
        get => speed;
        set => speed = Speeds.Contains(value) ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }
    public double PendingSeconds => accumulatedSeconds;
    public bool IsBehind => accumulatedSeconds >= TickSeconds;

    public int Advance(double elapsedSeconds, Action tick, int maximumTicks = 200, double maximumProcessingMilliseconds = double.PositiveInfinity)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumTicks);
        if(double.IsNaN(maximumProcessingMilliseconds)||maximumProcessingMilliseconds<=0) throw new ArgumentOutOfRangeException(nameof(maximumProcessingMilliseconds));
        if (Speed == 0) return 0;
        var pending = accumulatedSeconds + elapsedSeconds * Speed;
        if (!double.IsFinite(pending)) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        accumulatedSeconds = pending;
        var count = 0;
        var started=Stopwatch.GetTimestamp();
        while (accumulatedSeconds + 1e-9 >= TickSeconds && count < maximumTicks)
        {
            if(count>0 && Stopwatch.GetElapsedTime(started).TotalMilliseconds>=maximumProcessingMilliseconds) break;
            tick();
            accumulatedSeconds = Math.Max(0, accumulatedSeconds - TickSeconds);
            count++;
        }
        return count;
    }
}
