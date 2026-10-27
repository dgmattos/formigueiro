using AntWorld.Domain;
using AntWorld.Simulation;
using AntWorld.Infrastructure;
namespace AntWorld.Specifications;

public static class BlockPersistenceScenarios
{
    public static void ModifiedCellsAndResources()
    {
        var state = SimulationFactory.Create(new(42) { MaximumLoadedChunks = 2 });
        var world = state.World; var codec = new BinarySnapshotCodec();
        var before = codec.Encode(state); var p = new CellPosition(0, 5);
        world.AddWater(p, 321);
        var surface = new CellPosition(10, world.Surface[10]);
        var old = world.GetCell(surface, false);
        world.SetCell(surface, old with { Moisture = 17, Temperature = -123, Nutrients = 57, Permeability = 33 });
        var entry = world.Resources().First(e => e.ResourceId == ResourceIds.Iron);
        var extracted = world.Extract(entry.Position, entry.ResourceId, entry.Stack.Quantity / 2);
        world.ResourceCatalog.Register(new(99, "Sementes", "seed", true, .01, new(31)));
        world.SetResource(p, 99, new(4, 7, 4500));
        foreach (var region in world.Chunks()) world.ChunkManager.Load(new(region.X, region.Y));
        var bytes = codec.Encode(state); var restored = codec.Decode(bytes);
        Check.That(bytes.SequenceEqual(codec.Encode(restored)), "Snapshot v3 não canônico.");
        Check.That(restored.World.GetCell(p, false).WaterAmount == 321 && restored.World.GetCell(p, false).Moisture == 0, "Água em Air perdida.");
        Check.That(restored.World.GetCell(surface, false) == world.GetCell(surface, false), "Propriedades ambientais perdidas.");
        Check.That(restored.World.GetResource(entry.Position, entry.ResourceId).Quantity == entry.Stack.Quantity - extracted, "Extração não persistida.");
        Check.That(restored.World.GetResource(p, 99).Quantity == 4 && restored.World.ResourceCatalog.Get(99).Name == "Sementes", "Catálogo extensível não persistido.");
        Check.That(restored.World.ChunkManager.LoadedCount == 0, "Desserialização carregou o mundo inteiro.");
        Check.That(bytes.Length - before.Length < 1000, "Poucas alterações causaram snapshot denso.");
        var direct = new SimulationEngine(state); var resumed = new SimulationEngine(restored);
        direct.Run(120); resumed.Run(120);
        Check.That(codec.Encode(direct.State).SequenceEqual(codec.Encode(resumed.State)), "Retomada de blocos/recursos divergiu.");
    }
    public static void SeedBasedSizeAndVersion()
    {
        var codec = new BinarySnapshotCodec();
        var small = codec.Encode(SimulationFactory.Create(new(42, 240, 96)));
        var largeState = SimulationFactory.Create(new(42, 1024, 256));
        var large = codec.Encode(largeState);
        Check.That(Math.Abs(large.Length - small.Length) < 2000 && large.Length < 40000, "Snapshot de mundo base cresceu com todas as células.");
        var invalid = (byte[])small.Clone(); invalid[8] = 1;
        try { codec.Decode(invalid); throw new InvalidOperationException("Versão antiga aceita."); }
        catch (InvalidDataException ex) { Check.That(ex.Message.Contains("formato 8"), "Rejeição não explica versão."); }
        Check.That(codec.Decode(large).World.Configuration == largeState.World.Configuration, "Configuração geológica não recuperada.");
    }
    public static void Run() { ModifiedCellsAndResources(); SeedBasedSizeAndVersion(); }
}
