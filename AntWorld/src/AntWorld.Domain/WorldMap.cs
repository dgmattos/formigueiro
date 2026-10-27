namespace AntWorld.Domain;

public sealed class WorldMap
{
    public WorldConfiguration Configuration { get; }
    public int Width => Configuration.Width;
    public int Height => Configuration.Height;
    public int CellCount => Width * Height;
    public ChunkManager ChunkManager { get; }
    public ResourceCatalog ResourceCatalog { get; }
    public VegetationStore Vegetation { get; } = new();
    private readonly int[] topSolid;
    public CellGrid<TerrainKind> Terrain { get; }
    public CellGrid<double> Food { get; }
    public CellGrid<double> FoodCapacity { get; }
    public BiomeKind[] Biomes { get; }
    public int[] Surface { get; }
    // Column climate is distinct from material moisture, which is held in WorldCell.
    public double[] Moisture { get; }
    public int ResourceLayoutRevision { get; private set; }
    public int TerrainRevision { get; private set; }
    private int visualRevision;
    private readonly Dictionary<int, int> chunkRevisions = [];
    public int ChunkRevision(int x, int y) => chunkRevisions.GetValueOrDefault(ChunkManager.ChunkId(new(x, y)));
    public WorldMap(WorldConfiguration configuration, IWorldChunkSource? source = null,
        BiomeKind[]? biomes = null, int[]? surface = null, ResourceCatalog? resources = null)
    {
        configuration.Validate(); Configuration = configuration;
        topSolid = Enumerable.Repeat(-1, configuration.Width).ToArray();
        ChunkManager = new(configuration, source ?? new EmptyChunkSource());
        ResourceCatalog = resources ?? new();
        Biomes = biomes ?? new BiomeKind[Width]; Surface = surface ?? new int[Width];
        Moisture = Biomes.Select(b => BiomeProfile.For(b).Humidity).ToArray();
        Terrain = new(CellCount, i => TerrainAt(Position(i)), (i, t) => SetTerrain(Position(i), t));
        Food = new(CellCount, i => GetResource(Position(i), ResourceIds.Food).Quantity, (i, v) => SetFood(i, v, false));
        FoodCapacity = new(CellCount, i => GetResource(Position(i), ResourceIds.Food).Capacity, (i, v) => SetFood(i, v, true));
    }
    public bool Contains(CellPosition p) => p.Y >= 0 && p.Y < Height;
    public bool IsCanonical(CellPosition p) => Contains(p) && p.X >= 0 && p.X < Width;
    public int HorizontalDelta(int from, int to)
    {
        var delta = ChunkManager.NormalizeX(to) - ChunkManager.NormalizeX(from);
        return delta > Width / 2 ? delta - Width : delta < -Width / 2 ? delta + Width : delta;
    }
    public CellPosition Normalize(CellPosition p) => ChunkManager.Normalize(p);
    public int Index(CellPosition p) => ChunkManager.GlobalIndex(p);
    public CellPosition Position(int index) => new(index % Width, index / Width);
    public int Distance(CellPosition a, CellPosition b)
    {
        a = Normalize(a); b = Normalize(b); var dx = Math.Abs(a.X - b.X);
        return Math.Min(dx, Width - dx) + Math.Abs(a.Y - b.Y);
    }
    public WorldCell GetCell(CellPosition p, bool load = true) => ChunkManager.GetCell(p, load);
    public void SetCell(CellPosition p, WorldCell cell)
    {
        p = Normalize(p); cell.Validate(); var previous = GetCell(p, false);
        if (previous == cell) return;
        cell = cell with { Flags = cell.Flags | CellFlags.Modified };
        ChunkManager.SetCell(p, cell);
        if (previous.SolidFill != cell.SolidFill) topSolid[p.X] = -1;
        if (previous.SolidFill != cell.SolidFill || previous.WaterAmount != cell.WaterAmount || previous.Flags != cell.Flags) Vegetation.InvalidateAt(this, p);
        RemoveUnsupportedMushrooms(p);
        if (p.Y + 1 < Height) RemoveUnsupportedMushrooms(new(p.X, p.Y + 1));
        if (previous.SolidFill != cell.SolidFill || previous.RockFraction != cell.RockFraction || previous.Flags.HasFlag(CellFlags.Excavated) != cell.Flags.HasFlag(CellFlags.Excavated)) TerrainRevision++;
        visualRevision++;
        // Adjacent chunks can change autotile edges, including at the circular seam.
        for (var dy = -1; dy <= 1; dy++)
        for (var dx = -1; dx <= 1; dx++)
            if (Contains(new(p.X + dx, p.Y + dy))) chunkRevisions[ChunkManager.ChunkId(new(p.X + dx, p.Y + dy))] = visualRevision;
    }
    public ResourceStack GetResource(CellPosition p, ushort id) => ChunkManager.GetResource(p, id);
    public int TopSolidY(int x)
    {
        x = ChunkManager.NormalizeX(x);
        if (topSolid[x] >= 0) return topSolid[x];
        var y = 0;
        while (y < Height && GetCell(new(x, y), false).IsPassable) y++;
        return topSolid[x] = y;
    }
    public void SetResource(CellPosition p, ushort id, ResourceStack stack)
    {
        ResourceCatalog.Get(id);
        if (ResourceIds.IsMushroom(id) && stack.Quantity > 0 && !MushroomHabitat.Allows(this, p))
            throw new ArgumentException("Cogumelo exige terra sólida exposta, sem inundação.");
        var original = ChunkManager.BaselineResource(p, id);
        if ((stack.DepositId != 0 || original.DepositId != 0 && stack.Capacity > 0) && (stack.DepositId != original.DepositId || stack.Capacity != original.Capacity))
            throw new ArgumentException("Depósito não pode alterar identidade ou orçamento original.");
        if (original.DepositId != 0 && stack.Quantity > original.Capacity) throw new ArgumentException("Quantidade excede orçamento do depósito.");
        var previous = GetResource(p, id);
        if (previous == stack) return;
        if (ResourceIds.IsMushroom(id) && previous.Quantity <= 0 && stack.Quantity > 0 && CountMushrooms(ChunkManager.Region(p)) >= Configuration.Mushrooms.MaximumPerChunk)
            throw new ArgumentException("Limite de cogumelos do chunk atingido.");
        ChunkManager.SetResource(p, id, stack);
        if ((previous.Capacity > 0) != (stack.Capacity > 0)) ResourceLayoutRevision++;
        if (id != ResourceIds.Food) chunkRevisions[ChunkManager.ChunkId(p)] = ++visualRevision;
    }
    public int CountMushrooms(ChunkRegion region)
    {
        var count = 0;
        for (var i = 0; i < region.Width * region.Height; i++)
        {
            var p = region.Position(i);
            if (GetResource(p, ResourceIds.RedMushroom).Quantity > 0) count++;
            if (GetResource(p, ResourceIds.BrownMushroom).Quantity > 0) count++;
        }
        return count;
    }
    private void RemoveUnsupportedMushrooms(CellPosition support)
    {
        if (MushroomHabitat.Allows(this, support)) return;
        for (var id = ResourceIds.RedMushroom; id <= ResourceIds.BrownMushroom; id++)
            if (GetResource(support, id).Capacity > 0) SetResource(support, id, default);
    }
    public double Extract(CellPosition p, ushort id, double amount)
    {
        if (!double.IsFinite(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (!ResourceCatalog.Get(id).Extractable) return 0;
        var stack = GetResource(p, id); var extracted = Math.Min(amount, stack.Quantity);
        if (extracted > 0) SetResource(p, id, stack with { Quantity = stack.Quantity - extracted });
        return extracted;
    }
    public void AddWater(CellPosition p, int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var cell = GetCell(p, false);
        if ((long)cell.WaterAmount + amount > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(amount), "Água livre excede o armazenamento da célula.");
        SetCell(p, cell with { WaterAmount = (ushort)(cell.WaterAmount + amount), Flags = cell.Flags | CellFlags.EnvironmentChanged });
    }
    public int AbsorbWater(CellPosition p, int maximumUnits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumUnits);
        var cell = GetCell(p, false); var capacity = cell.RetentionCapacity;
        if (capacity == 0 || cell.Permeability == 0 || cell.Moisture == 100) return 0;
        var available = Math.Min(cell.WaterAmount, Math.Ceiling(maximumUnits * cell.Permeability / 255.0));
        var increase = (int)Math.Min(100 - cell.Moisture, Math.Floor(available * 100 / capacity));
        if (increase == 0) return 0;
        var absorbed = (int)Math.Ceiling(capacity * increase / 100);
        SetCell(p, cell with { Moisture = (byte)(cell.Moisture + increase), WaterAmount = (ushort)(cell.WaterAmount - absorbed), Flags = cell.Flags | CellFlags.EnvironmentChanged });
        return absorbed;
    }
    public IEnumerable<ResourceEntry> Resources() => ChunkManager.EnumerateResources();
    private void SetFood(int index, double value, bool capacity)
    {
        var p = Position(index); var stack = GetResource(p, ResourceIds.Food);
        SetResource(p, ResourceIds.Food, capacity ? stack with { Capacity = value, Quantity = Math.Min(stack.Quantity, value) } : stack with { Quantity = value });
    }
    public void ClearFood(bool capacities = false)
    {
        foreach (var entry in Resources().Where(e => e.ResourceId == ResourceIds.Food).ToArray())
            SetResource(entry.Position, entry.ResourceId, capacities ? default : entry.Stack with { Quantity = 0 });
    }
    public ExposedSides Exposure(CellPosition position)
    {
        var p = Normalize(position); var cell = GetCell(p, false); var result = ExposedSides.None;
        ReadOnlySpan<CellPosition> offsets = [new(-1, 0), new(1, 0), new(0, -1), new(0, 1)];
        for (var i = 0; i < offsets.Length; i++)
        {
            var next = new CellPosition(p.X + offsets[i].X, p.Y + offsets[i].Y);
            if (!Contains(next) || GetCell(next, false).SolidFill < cell.SolidFill) result |= (ExposedSides)(1 << i);
        }
        return result;
    }
    public bool IsOpen(CellPosition p) => Contains(p) && GetCell(p, false).IsPassable;
    public bool CanDig(CellPosition p) => Contains(p) && GetCell(p, false).SolidFill > 0;
    public bool CanWalk(CellPosition p)
    {
        if (!IsOpen(p) || Vegetation.BlocksMovement(this, p)) return false;
        if (GetCell(p, false).Flags.HasFlag(CellFlags.Excavated)) return true;
        for (var dy = -1; dy <= 1; dy++)
        for (var dx = -1; dx <= 1; dx++)
        {
            var neighbor = new CellPosition(p.X + dx, p.Y + dy);
            if (Contains(neighbor) && !IsOpen(neighbor)) return true;
        }
        return false;
    }
    public int GetWalkableNeighbors(CellPosition p,Span<CellPosition> output)
    {
        if(output.Length<8) throw new ArgumentException("Oito posições necessárias.");
        var count=0;
        for(var dy=-1;dy<=1;dy++) for(var dx=-1;dx<=1;dx++)
        {
            if(dx==0&&dy==0) continue;
            var next=new CellPosition(p.X+dx,p.Y+dy);
            if(!CanWalk(next)) continue;
            if(dx!=0&&dy!=0&&!IsOpen(new(p.X+dx,p.Y))&&!IsOpen(new(p.X,p.Y+dy))) continue;
            output[count++]=Normalize(next);
        }
        return count;
    }
    public IEnumerable<CellPosition> WalkableNeighbors(CellPosition p)
    {
        for (var dy = -1; dy <= 1; dy++)
        for (var dx = -1; dx <= 1; dx++)
        {
            if (dx == 0 && dy == 0) continue;
            var next = new CellPosition(p.X + dx, p.Y + dy);
            if (!CanWalk(next)) continue;
            if (dx != 0 && dy != 0 && !IsOpen(new(p.X + dx, p.Y)) && !IsOpen(new(p.X, p.Y + dy))) continue;
            yield return Normalize(next);
        }
    }
    public bool Dig(CellPosition p)
    {
        if (!CanDig(p)) return false;
        SetTerrain(p, TerrainKind.Tunnel); return true;
    }
    public TerrainKind TerrainAt(CellPosition p)
    {
        var cell = GetCell(p, false);
        if (cell.Flags.HasFlag(CellFlags.Excavated)) return TerrainKind.Tunnel;
        if (cell.SolidFill == 0) return TerrainKind.Air;
        if (cell.RockFraction == 255) return TerrainKind.Rock;
        return cell.SoilMaterial switch { MaterialCatalog.Sand => TerrainKind.Sand, MaterialCatalog.Clay => TerrainKind.Clay, _ => TerrainKind.Soil };
    }
    public void SetTerrain(CellPosition p, TerrainKind kind)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        var old = GetCell(p, false);
        if (kind is TerrainKind.Air or TerrainKind.Tunnel)
        {
            SetCell(p, new() { WaterAmount = old.WaterAmount, Temperature = old.Temperature, Flags = kind == TerrainKind.Tunnel ? CellFlags.Excavated : CellFlags.None }); return;
        }
        var id = kind switch { TerrainKind.Sand => MaterialCatalog.Sand, TerrainKind.Clay => MaterialCatalog.Clay, TerrainKind.Rock => MaterialCatalog.Granite, _ => MaterialCatalog.Loam };
        var material = MaterialCatalog.Get(id);
        SetCell(p, new WorldCell { SolidFill = 255, RockFraction = (byte)(kind == TerrainKind.Rock ? 255 : 0), SoilMaterial = MaterialCatalog.Loam,
            RockMaterial = MaterialCatalog.Granite, Porosity = material.Porosity, Permeability = material.Permeability,
            Moisture = old.Moisture, WaterAmount = old.WaterAmount, Temperature = old.Temperature, Nutrients = old.Nutrients } with { SoilMaterial = kind == TerrainKind.Rock ? MaterialCatalog.Loam : id });
    }
    public IEnumerable<ChunkRegion> Chunks()
    {
        var size = Configuration.ChunkSize;
        for (var y = 0; y < Height; y += size)
        for (var x = 0; x < Width; x += size)
            yield return new(x, y, Math.Min(size, Width - x), Math.Min(size, Height - y));
    }
}
