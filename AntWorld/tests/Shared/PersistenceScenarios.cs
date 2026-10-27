using AntWorld.Simulation;
using AntWorld.Infrastructure;
using AntWorld.Persistence;
using Microsoft.Data.Sqlite;
namespace AntWorld.Specifications;

public static class PersistenceScenarios
{
    public static void Run()
    {
        var path = Path.Combine(Path.GetTempPath(), $"antworld-{Guid.NewGuid():N}.db");
        try
        {
            var store = new SqliteSnapshotStore(path);
            var state = SimulationFactory.Create(new(77, 64, 48));
            var codec = new BinarySnapshotCodec();
            const string slot = "save'; DROP TABLE snapshots; --";
            store.Save(slot, state);
            Check.That(codec.Encode(state).SequenceEqual(codec.Encode(store.Load(slot))), "SQLite alterou o snapshot.");
            var engine = new SimulationEngine(state); engine.Run(20); store.Save(slot, state);
            Check.That(new SqliteSnapshotStore(path).Load(slot).Tick == 20, "SQLite não persistiu overwrite/reabertura.");
            Check.Throws<KeyNotFoundException>(() => store.Load("ausente"));
            Check.Throws<ArgumentException>(() => store.Save(" ", state));
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
            connection.Open(); using var command = connection.CreateCommand();
            command.CommandText = "UPDATE snapshots SET payload=zeroblob(100)"; command.ExecuteNonQuery();
            Check.Throws<InvalidDataException>(() => store.Load(slot));
        }
        finally { File.Delete(path); }
    }
}
