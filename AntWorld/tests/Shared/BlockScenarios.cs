using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AntWorld.Domain;
using AntWorld.WorldGeneration;
namespace AntWorld.Specifications;

public static class BlockScenarios
{
    private static WorldMap World(int seed = 42) => new WorldGenerator().Generate(new(seed) { MaximumLoadedChunks = 3 });
    public static void PhysicalTypes()
    {
        var soil = new WorldCell { SolidFill = 255, SoilMaterial = MaterialCatalog.Loam, RockMaterial = MaterialCatalog.Granite, Porosity = 150 };
        var cells = new[] { default(WorldCell), soil with { SolidFill = 180 }, soil, soil with { RockFraction = 80 }, soil with { RockFraction = 255 } };
        Check.That(cells.Select(c => c.Type).SequenceEqual(Enum.GetValues<BlockType>()), "Cinco tipos físicos não correspondem à composição.");
        foreach (var cell in cells) cell.Validate();
        Check.That(cells[1].SolidFill != 128 && cells[3].RockFraction != 128, "Misturas presumidas 50/50.");
        Check.That(Marshal.SizeOf<WorldCell>() == 16 && !RuntimeHelpers.IsReferenceOrContainsReferences<WorldCell>(), "Célula não compacta ou contém referências.");
        Check.Throws<ArgumentException>(() => (soil with { Moisture = 101 }).Validate());
        Check.Throws<ArgumentException>(() => (soil with { SoilMaterial = MaterialCatalog.Granite }).Validate());
    }
    public static void MoistureAndWater()
    {
        var world = World(); var air = new CellPosition(5, 0);
        Check.That(world.GetCell(air).Moisture == 0, "Air deve iniciar seco.");
        world.SetCell(air, world.GetCell(air) with { WaterAmount = 500 });
        Check.That(world.GetCell(air).WaterAmount == 500 && world.GetCell(air).Moisture == 0, "Água livre depende de umidade.");
        var soil = new WorldCell { SolidFill = 255, SoilMaterial = MaterialCatalog.Loam, RockMaterial = MaterialCatalog.Granite, Porosity = 150, Moisture = 50 };
        Check.That(soil.RetentionCapacity > (soil with { RockFraction = 128 }).RetentionCapacity &&
            (soil with { RockFraction = 128 }).RetentionCapacity > (soil with { RockFraction = 255 }).RetentionCapacity, "Retenção ignora materiais.");
        Check.That((soil with { SolidFill = 100 }).RetentionCapacity < soil.RetentionCapacity &&
            (soil with { Porosity = 10 }).RetentionCapacity < soil.RetentionCapacity, "Retenção ignora preenchimento/porosidade.");
        Check.That(soil.RetainedWater == soil.RetentionCapacity * .5, "Escala de umidade inválida.");
        world.SetCell(new(8, 8), soil with { Moisture = 0, WaterAmount = 1000, Permeability = 255 });
        var absorbed = world.AbsorbWater(new(8, 8), 1000);
        Check.That(absorbed > 0 && world.GetCell(new(8, 8)).Moisture == 100 && world.GetCell(new(8, 8)).WaterAmount == 1000 - absorbed, "Absorção não transferiu água livre.");
        Check.That(world.AbsorbWater(air, 1000) == 0, "Air reteve água no material.");
    }
    public static void Neighbors()
    {
        var world = World(); Span<CellPosition> buffer = stackalloc CellPosition[8];
        var count = world.ChunkManager.GetNeighbors(new(15, 15), buffer);
        Check.That(count == 4 && buffer[..count].ToArray().SequenceEqual(new[] { new CellPosition(14, 15), new(16, 15), new(15, 14), new(15, 16) }), "Ordem/vizinhos ortogonais inválidos.");
        Check.That(world.ChunkManager.ChunkId(buffer[1]) != world.ChunkManager.ChunkId(new(15, 15)), "Não atravessou chunk.");
        world.ChunkManager.GetNeighbors(new(0, 15), buffer);
        Check.That(buffer[0] == new CellPosition(world.Width - 1, 15), "Emenda circular quebrada.");
        world.ChunkManager.GetNeighbors(new(-1, 15), buffer);
        Check.That(buffer[1] == new CellPosition(0, 15), "Coordenadas negativas não normalizadas.");
        Check.That(world.ChunkManager.GetNeighbors(new(8, 0), buffer) == 3, "Topo não limitado.");
        Check.That(world.ChunkManager.GetNeighbors(new(8, 8), buffer, true) == 8, "Diagonais ausentes.");
        Check.That(world.Normalize(new(int.MinValue, 1)).X >= 0, "Overflow de módulo negativo.");
        Check.Throws<ArgumentOutOfRangeException>(() => world.GetCell(new(1, -1)));
    }
    public static void UnloadedQueries()
    {
        var world = World(); Span<CellPosition> buffer = stackalloc CellPosition[8];
        for (var i = 0; i < 1000; i++)
        {
            var n = world.ChunkManager.GetNeighbors(new(i - 500, 20), buffer);
            for (var j = 0; j < n; j++) world.GetCell(buffer[j], false).Validate();
        }
        Check.That(world.ChunkManager.GeneratedCount == 0, "Consulta lógica gerou chunks.");
        foreach (var region in world.Chunks()) world.ChunkManager.Load(new(region.X, region.Y));
        Check.That(world.ChunkManager.LoadedCount == 3, "Cache excedeu limite.");
        var p = new CellPosition(0, 20); world.SetCell(p, world.GetCell(p, false) with { WaterAmount = 123 });
        world.ChunkManager.Load(p); world.ChunkManager.Unload(p);
        Check.That(world.GetCell(p, false).WaterAmount == 123 && !world.ChunkManager.IsLoaded(p), "Unload perdeu edição ou carregou chunk.");
    }
    public static void GenerationOrder()
    {
        var a = World(); var b = World();
        var expected = new Dictionary<CellPosition, WorldCell>();
        foreach (var region in a.Chunks())
        {
            var chunk = a.ChunkManager.Load(new(region.X, region.Y));
            for (var i = 0; i < chunk.Cells.Length; i++) expected[region.Position(i)] = chunk.Cells.Span[i];
        }
        foreach (var region in b.Chunks().Reverse())
        {
            var chunk = b.ChunkManager.Load(new(region.X, region.Y));
            for (var i = 0; i < chunk.Cells.Length; i++) Check.That(expected[region.Position(i)] == chunk.Cells.Span[i], "Ordem de geração alterou células.");
            foreach (var (key, stack) in chunk.Resources.Entries)
                Check.That(stack == a.GetResource(region.Position(key.CellIndex), key.ResourceId), "Recurso local diverge do plano global.");
        }
        Check.That(a.Resources().SequenceEqual(b.Resources()), "Recursos dependem da ordem.");
        Check.That(Math.Abs(a.Surface[0] - a.Surface[^1]) <= 1, "Relevo descontínuo na emenda.");
    }
    public static void Deposits()
    {
        var world = World(); var groups = world.Resources().Where(e => e.Stack.DepositId != 0).GroupBy(e => e.Stack.DepositId).ToArray();
        Check.That(groups.Length > 0, "Depósitos ausentes.");
        Check.That(groups.Any(g => g.Select(e => world.ChunkManager.ChunkId(e.Position)).Distinct().Count() > 1), "Veios interrompidos por chunk.");
        Check.That(groups.Any(g => g.Any(e => e.Position.X == 0) && g.Any(e => e.Position.X == world.Width - 1)), "Nenhum depósito atravessa emenda.");
        foreach (var group in groups)
        {
            var deposit = world.ChunkManager.Deposits.Single(d => d.Id == group.Key);
            Check.That(group.Sum(e => e.Stack.Quantity) <= deposit.Budget + 1e-8, "Depósito excedeu orçamento.");
            var remaining = group.Select(e => e.Position).ToHashSet(); var queue = new Queue<CellPosition>(); queue.Enqueue(remaining.First()); remaining.Remove(queue.Peek());
            var neighbors = new CellPosition[4];
            while (queue.TryDequeue(out var p))
            {
                var n = world.ChunkManager.GetNeighbors(p, neighbors);
                for (var i = 0; i < n; i++) if (remaining.Remove(neighbors[i])) queue.Enqueue(neighbors[i]);
            }
            Check.That(remaining.Count == 0, "Depósito fragmentado.");
            foreach (var entry in group)
            {
                var cell = world.GetCell(entry.Position, false);
                Check.That(entry.ResourceId != ResourceIds.Iron || cell.RockFraction > 100, "Minério em material incompatível.");
            }
        }
        Check.That(groups.SelectMany(g => g).Select(e => e.Stack.Concentration).Distinct().Count() > 5, "Concentração uniforme.");
    }
    public static void ExtractionAndCatalog()
    {
        var world = World(); var entry = world.Resources().First(e => e.ResourceId == ResourceIds.Iron);
        var loaded = world.ChunkManager.Load(entry.Position);
        var amount = world.Extract(entry.Position, entry.ResourceId, entry.Stack.Quantity / 3);
        Check.That(loaded.Resources.Get(new(loaded.Region.Index(entry.Position), entry.ResourceId)).Quantity == entry.Stack.Quantity - amount, "Extração não atualizou chunk residente.");
        world.ChunkManager.Unload(entry.Position);
        loaded = world.ChunkManager.Load(entry.Position);
        Check.That(loaded.Resources.Get(new(loaded.Region.Index(entry.Position), entry.ResourceId)).Quantity == entry.Stack.Quantity - amount, "Unload/load regenerou minério extraído.");
        Check.That(amount > 0 && world.GetResource(entry.Position, entry.ResourceId).Quantity == entry.Stack.Quantity - amount, "Extração parcial falhou.");
        world.Extract(entry.Position, entry.ResourceId, 1e8);
        Check.That(world.GetResource(entry.Position, entry.ResourceId).Quantity == 0, "Extração negativa.");
        Check.Throws<ArgumentException>(() => world.SetResource(entry.Position, entry.ResourceId, entry.Stack with { Capacity = entry.Stack.Capacity * 2 }));
        Check.Throws<ArgumentOutOfRangeException>(() => world.Extract(entry.Position, entry.ResourceId, double.NaN));
        world.ResourceCatalog.Register(new(99, "Semente", "seed", true, .01, new(31)));
        world.SetResource(entry.Position, 99, new(8, 10, 2500));
        Check.That(world.GetResource(entry.Position, 99).Quantity == 8 && world.GetResource(entry.Position, entry.ResourceId).Quantity == 0, "Recursos diferentes se sobrepõem.");
    }
    public static void BiomesAndDepth()
    {
        var world = World(); var types = new HashSet<BlockType>();
        foreach (var region in world.Chunks())
        {
            var chunk = world.ChunkManager.Load(new(region.X, region.Y));
            foreach (var cell in chunk.Cells.Span) { cell.Validate(); types.Add(cell.Type); Check.That(cell.Moisture <= 100, "Umidade inválida."); }
        }
        Check.That(types.Count == 5, "Geração não usou os cinco tipos.");
        foreach (var biome in Enum.GetValues<BiomeKind>())
        {
            var x = Array.IndexOf(world.Biomes, biome); var cell = world.GetCell(new(x, world.Surface[x]));
            Check.That(cell.Temperature == (short)(BiomeProfile.For(biome).Temperature * 10), "Temperatura ignora bioma.");
            if (biome == BiomeKind.Swamp) Check.That(cell.WaterAmount > 0 && cell.Moisture > 80, "Pântano sem saturação.");
            if (biome == BiomeKind.Desert) Check.That(cell.SoilMaterial == MaterialCatalog.Sand && cell.Moisture < 20, "Deserto não arenoso/seco.");
        }
        var other = new WorldGenerator().Generate(world.Configuration with { Geology = world.Configuration.Geology with { SoilDepth = .3, RockDepth = .7 } });
        Check.That(!world.Terrain.SequenceEqual(other.Terrain), "Profundidades configuráveis ignoradas.");
    }
    public static void NoPerCellAllocation()
    {
        var world = World();
        for (var i = 0; i < 1000; i++) world.GetCell(new(i % world.Width, i % world.Height), false);
        var before = GC.GetAllocatedBytesForCurrentThread();
        long checksum = 0;
        for (var i = 0; i < 10000; i++) checksum += world.GetCell(new(i % world.Width, i % world.Height), false).SolidFill;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check.That(checksum > 0 && allocated < 1024, "Consulta de célula aloca objetos por posição.");
    }
    public static void AutotileBoundaries()
    {
        var world = World(); var p = new CellPosition(0, world.Surface[0] + 1);
        var original = world.GetCell(p, false);
        _ = world.Exposure(p);
        Check.That(world.ChunkManager.GeneratedCount == 0 && world.ChunkManager.CellChanges.Count == 0 && world.GetCell(p, false) == original, "Consulta visual alterou/carregou estado.");
        world.SetTerrain(p, TerrainKind.Tunnel);
        Check.That(world.ChunkRevision(0, p.Y) > 0 && world.ChunkRevision(world.Width - 1, p.Y) > 0, "Borda circular não invalidou autotile vizinho.");
        Check.That(world.Exposure(new(world.Width - 1, p.Y)).HasFlag(ExposedSides.Right), "Autotile não enxerga abertura na emenda.");
    }
    public static void Run()
    {
        AutotileBoundaries(); NoPerCellAllocation(); PhysicalTypes(); MoistureAndWater(); Neighbors(); UnloadedQueries(); GenerationOrder(); Deposits(); ExtractionAndCatalog(); BiomesAndDepth();
    }
}
