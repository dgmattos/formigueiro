using AntWorld.Simulation;
using Xunit;
namespace AntWorld.Simulation.Tests;
public sealed class PlaybackSettingsTests
{
    [Fact] public void EditedMultipliersReachClockAndResume()
    {
        var settings=PlaybackSettings.Parse("""{"speedMultipliers":{"x1":1,"x2":7,"x3":25}}""");
        var control=new PlaybackControl(settings);control.Select(settings.X2);var ticks=0;control.Clock.Advance(1,()=>ticks++,1000);Assert.Equal(140,ticks);
        control.TogglePause();control.Select(settings.X3);Assert.True(control.IsPaused);control.TogglePause();Assert.Equal(25,control.Clock.Speed);
        Assert.Throws<ArgumentOutOfRangeException>(()=>control.Select(50));
    }
    [Fact] public void FrameWorkBudgetRetainsUnprocessedTime()
    {
        var clock=new SimulationClock {Speed=100};var ticks=0;
        clock.Advance(1,()=>{ticks++;Thread.Sleep(2);},100,1);
        Assert.Equal(1,ticks);Assert.True(clock.PendingSeconds>99);
    }
    [Theory]
    [InlineData("{}")]
    [InlineData("not json")]
    [InlineData("{\"speedMultipliers\":{\"x1\":0,\"x2\":50,\"x3\":100}}")]
    [InlineData("{\"speedMultipliers\":{\"x1\":1,\"x2\":100,\"x3\":50}}")]
    [InlineData("{\"speedMultipliers\":{\"x1\":1,\"x2\":50,\"x3\":1001}}")]
    [InlineData("{\"speedMultipliers\":{\"x1\":1,\"x2\":2.5,\"x3\":100}}")]
    public void InvalidSettingsHaveExplicitErrors(string json) => Assert.Throws<ArgumentException>(()=>PlaybackSettings.Parse(json));
}
