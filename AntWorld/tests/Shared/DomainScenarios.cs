using AntWorld.Domain;
namespace AntWorld.Specifications;

public static class DomainScenarios
{
    public static void Run()
    {
        Check.Throws<ArgumentOutOfRangeException>(() => new WorldMap(new(0, 1, 1)));
        Check.Throws<ArgumentOutOfRangeException>(() => (Genome.Ember with { Strength = double.NaN }).Validate());
        Genome.Ember.Validate(); Genome.Moss.Validate();
        var random = DeterministicRandom.FromSeed(-42);
        for (var i = 0; i < 100; i++) random.NextDouble();
        var resumed = new DeterministicRandom(random.State);
        for (var i = 0; i < 100; i++) Check.That(random.NextDouble() == resumed.NextDouble(), "Estado do PRNG não reproduzido.");
        var world = new WorldMap(new(1, 65, 49));
        Check.That(world.Chunks().Sum(c => c.Width * c.Height) == world.Terrain.Length, "Chunks não cobrem o mapa.");
        var p = new CellPosition(10, 20);
        world.SetTerrain(new(10,21),TerrainKind.Rock);
        var beforeDigging=world.TerrainRevision;
        world.Terrain[world.Index(p)] = TerrainKind.Rock;
        Check.That(world.Dig(p) && world.CanWalk(p), "Rocha deve permitir escavação; custos são validados na ação do agente.");
        world.Terrain[world.Index(p)] = TerrainKind.Soil;
        Check.That(world.Dig(p) && world.TerrainRevision == beforeDigging+4 && world.CanWalk(p), "Escavação não atualizou o terreno.");
        Check.That(world.ChunkRevision(0, 16) == beforeDigging+4 && world.ChunkRevision(32, 16) == 0, "Escavação invalidou chunk errado.");
        Check.That(!world.CanWalk(new(0, 0)), "Formiga pode voar no céu.");
    }
}
