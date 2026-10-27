using AntWorld.Domain;
using AntWorld.Simulation;
using AntWorld.Infrastructure;
namespace AntWorld.Specifications;

public static class SnapshotScenarios
{
    public static void Run()
    {
        var engine = new SimulationEngine(SimulationFactory.Create(new(12345, 96, 48)));
        engine.Run(150);
        var codec = new BinarySnapshotCodec();
        var bytes = codec.Encode(engine.State);
        var resumed = new SimulationEngine(codec.Decode(bytes));
        Check.That(bytes.SequenceEqual(codec.Encode(resumed.State)), "Round-trip alterou o snapshot.");
        engine.Run(250); resumed.Run(250);
        Check.That(codec.Encode(engine.State).SequenceEqual(codec.Encode(resumed.State)), "Continuação após load divergiu.");
        var corrupted = (byte[])bytes.Clone(); corrupted[^1] ^= 1;
        Check.Throws<InvalidDataException>(() => codec.Decode(corrupted));
        Check.Throws<InvalidDataException>(() => codec.Decode(bytes[..20]));
        corrupted = (byte[])bytes.Clone(); corrupted[8] = 99;
        Check.Throws<InvalidDataException>(() => codec.Decode(corrupted));
        var path = Path.Combine(Path.GetTempPath(), $"antworld-{Guid.NewGuid():N}.antworld");
        try
        {
            var file = new BinarySnapshotFile(); file.Save(path, engine.State);
            Check.That(codec.Encode(file.Load(path)).SequenceEqual(codec.Encode(engine.State)), "Arquivo não preservou o estado.");
            resumed.Run(20); file.Save(path, resumed.State);
            Check.That(file.Load(path).Tick == resumed.State.Tick, "Overwrite atômico falhou.");
        }
        finally { File.Delete(path); }
    }
}
