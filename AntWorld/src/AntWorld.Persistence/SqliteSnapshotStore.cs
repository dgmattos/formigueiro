using AntWorld.Domain;
using AntWorld.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
namespace AntWorld.Persistence;

public sealed class SqliteSnapshotStore : ISnapshotStore
{
    private readonly string connectionString;
    private readonly BinarySnapshotCodec codec = new();
    private readonly ILogger<SqliteSnapshotStore> logger;

    public SqliteSnapshotStore(string path, ILogger<SqliteSnapshotStore>? logger = null)
    {
        this.logger = logger ?? NullLogger<SqliteSnapshotStore>.Instance;
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        connectionString = new SqliteConnectionStringBuilder { DataSource = fullPath, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false, DefaultTimeout = 5 }.ToString();
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "PRAGMA user_version;";
        var version = Convert.ToInt32(command.ExecuteScalar());
        if (version > 1) throw new InvalidDataException("Versão do banco não suportada.");
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS snapshots (
                slot TEXT PRIMARY KEY NOT NULL,
                seed INTEGER NOT NULL,
                tick INTEGER NOT NULL,
                saved_utc TEXT NOT NULL,
                payload BLOB NOT NULL
            );
            PRAGMA user_version = 1;
            """;
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public void Save(string slot, SimulationState state)
    {
        ValidateSlot(slot);
        var payload = codec.Encode(state);
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO snapshots(slot, seed, tick, saved_utc, payload)
            VALUES($slot, $seed, $tick, $saved, $payload)
            ON CONFLICT(slot) DO UPDATE SET seed=excluded.seed, tick=excluded.tick,
                saved_utc=excluded.saved_utc, payload=excluded.payload;
            """;
        command.Parameters.AddWithValue("$slot", slot);
        command.Parameters.AddWithValue("$seed", state.World.Configuration.Seed);
        command.Parameters.AddWithValue("$tick", state.Tick);
        command.Parameters.AddWithValue("$saved", DateTimeOffset.UtcNow.ToString("O"));
        command.Parameters.Add("$payload", SqliteType.Blob).Value = payload;
        command.ExecuteNonQuery();
        transaction.Commit();
        logger.LogInformation("Snapshot {Slot} salvo no tick {Tick}, {Bytes} bytes", slot, state.Tick, payload.Length);
    }

    public SimulationState Load(string slot)
    {
        ValidateSlot(slot);
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT payload FROM snapshots WHERE slot=$slot AND length(payload) <= $limit";
        command.Parameters.AddWithValue("$slot", slot);
        command.Parameters.AddWithValue("$limit", BinarySnapshotCodec.MaximumBytes + 48);
        var payload = command.ExecuteScalar() as byte[] ?? throw new KeyNotFoundException($"Snapshot '{slot}' ausente ou excede o limite de tamanho.");
        var state = codec.Decode(payload);
        logger.LogInformation("Snapshot {Slot} carregado no tick {Tick}", slot, state.Tick);
        return state;
    }
    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        try { connection.Open(); return connection; }
        catch { connection.Dispose(); throw; }
    }
    private static void ValidateSlot(string slot)
    {
        if (string.IsNullOrWhiteSpace(slot) || slot.Length > 80) throw new ArgumentException("Slot deve ter de 1 a 80 caracteres.", nameof(slot));
    }
}
