using Xunit;
using AntWorld.Specifications;
namespace AntWorld.Persistence.Tests;
public sealed class PersistenceTests
{
    [Fact] public void BinarySnapshots() => SnapshotScenarios.Run();
    [Fact] public void RealSqliteRoundTrip() => PersistenceScenarios.Run();
}
