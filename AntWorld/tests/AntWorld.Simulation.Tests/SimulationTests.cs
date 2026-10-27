using Xunit;
using AntWorld.Specifications;
namespace AntWorld.Simulation.Tests;
public sealed class SimulationTests
{
    [Fact] public void EcologyAndTime() => SimulationScenarios.Run();
}
