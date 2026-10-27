using Xunit;
using AntWorld.Specifications;
namespace AntWorld.Domain.Tests;
public sealed class DomainTests
{
    [Fact] public void GenerationAndInheritance() => GenerationScenarios.Run();
    [Fact] public void DomainInvariants() => DomainScenarios.Run();
}
