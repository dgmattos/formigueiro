using AntWorld.Domain;
using Xunit;
namespace AntWorld.Domain.Tests;
public sealed class NestLayoutTests
{
    [Fact]
    public void SettingsRejectInvalidBudgetsRatiosAndNonFiniteProfiles()
    {
        new NestLayoutSettings().Validate();
        Assert.Throws<ArgumentException>(()=>new NestLayoutSettings(TargetAlternativeRouteRatio:double.NaN).Validate());
        Assert.Throws<ArgumentException>(()=>new NestLayoutSettings(MaximumChambers:61).Validate());
        Assert.Throws<ArgumentException>(()=>new NestLayoutSettings{Strong=new(RockCost:double.PositiveInfinity)}.Validate());
    }
    [Fact]
    public void DuplicateEdgesAndUnbuiltDestinationsAreRejected()
    {
        var n=new NestLayout(1);n.AddChamber(new(1,NestChamberType.Queen,new(10,10),2,1));
        n.AddChamber(new(2,NestChamberType.Brood,new(20,20),2,1));Assert.Null(n.Designated(1));
        n.AddTunnel(new(1,1,2,[new(10,10),new(20,20)]));
        Assert.Throws<ArgumentException>(()=>n.AddTunnel(new(2,2,1,[])));
        Assert.True(n.IsConnected());Assert.Equal(0,n.AlternativeRouteRatio(true));
    }
}
