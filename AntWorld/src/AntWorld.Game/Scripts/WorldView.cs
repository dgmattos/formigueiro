using AntWorld.Domain;
using AntWorld.Infrastructure;
using Godot;
namespace AntWorld.Game;

public partial class WorldView : Node2D
{
    private readonly Dictionary<(int Id, int Copy), TerrainChunkView> terrainViews = [];
    private (int, int, int, int)? previousBounds;
    private readonly HashSet<(int Id, int Copy)> visible = [];
    private PreparedSpriteCatalog sprites = null!;
    private int tileSize;
    public SimulationState State { get; private set; } = null!;
    public EntityView Entities { get; private set; } = null!;
    public int ValidateVisibleTerrainCoverage()
    {
        StreamVisibleTerrain();
        var area = WorldViewport.Bounds(this); var world = State.World; var checkedCells = 0;
        var firstY = Math.Max(0, (int)Math.Floor(area.Position.Y / tileSize));
        var lastY = Math.Min(world.Height, (int)Math.Ceiling(area.End.Y / tileSize));
        for (var x = (int)Math.Floor(area.Position.X / tileSize); x < (int)Math.Ceiling(area.End.X / tileSize); x++)
        for (var y = firstY; y < lastY; y++)
        {
            var p = world.Normalize(new(x, y));
            if (world.GetCell(p, false).SolidFill == 0) continue;
            var key = (world.ChunkManager.ChunkId(p), (int)Math.Floor((double)x / world.Width));
            if (!terrainViews.TryGetValue(key, out var view) || !view.HasRenderedCell(p, new(x, y)))
                throw new InvalidOperationException($"Terreno ausente/desalinhado em {x},{y}, cópia {key.Item2}.");
            checkedCells++;
        }
        if (world.ChunkManager.LoadedCount > world.Configuration.MaximumLoadedChunks)
            throw new InvalidOperationException("Streaming ultrapassou limite do cache lógico.");
        return checkedCells;
    }
    public void ValidateTerrainLayers()
    {
        if (terrainViews.Count == 0 || terrainViews.Values.Sum(v => v.RenderedTileCount) == 0) throw new InvalidOperationException("Terreno não populou TileMapLayers visíveis.");
        foreach (var view in terrainViews.Values) view.ValidateAlignment();
    }
    public void Initialize(SimulationState state, RenderingSettings rendering)
    {
        State = state;
        tileSize = rendering.TileSize; sprites = new();
        var vegetation = new SurfaceObjectsView { Name = "VegetationLayer", ZIndex = 4 };
        vegetation.Initialize(state, sprites, tileSize, false); AddChild(vegetation);
        var objects = new SurfaceObjectsView { Name = "ObjectLayer", ZIndex = 5 };
        objects.Initialize(state, sprites, tileSize, true); AddChild(objects);
        Entities = new EntityView { Name = "AgentLayer", ZIndex = 6 }; Entities.Initialize(state, tileSize); AddChild(Entities);
    }
    public override void _Process(double delta)
    {
        StreamVisibleTerrain(); QueueRedraw();
    }
    private void StreamVisibleTerrain()
    {
        var world = State.World;
        var area = WorldViewport.Bounds(this);
        // Floor at cell resolution also handles the short final chunk at the circular seam.
        var bounds = ((int)Math.Floor(area.Position.X / tileSize), (int)Math.Ceiling(area.End.X / tileSize),
            (int)Math.Floor(area.Position.Y / tileSize), (int)Math.Ceiling(area.End.Y / tileSize));
        if (previousBounds == bounds) return;
        previousBounds = bounds; visible.Clear();
        foreach (var chunk in CircularTerrainVisibility.Select(world.Configuration, bounds.Item1, bounds.Item3, bounds.Item2, bounds.Item4))
        {
            var key = (chunk.ChunkId, chunk.Copy); visible.Add(key);
            if (terrainViews.ContainsKey(key)) continue;
            var view = new TerrainChunkView { TextureFilter = TextureFilterEnum.Nearest, Position = new(chunk.OffsetX * tileSize, 0) };
            view.Initialize(State, chunk.Region, sprites, tileSize); terrainViews[key] = view; AddChild(view);
        }
        var logicalVisible = visible.Select(key => key.Id).ToHashSet();
        foreach (var key in terrainViews.Keys.Where(key => !visible.Contains(key)).ToArray())
        {
            RemoveChild(terrainViews[key]); terrainViews[key].QueueFree(); terrainViews.Remove(key);
            // Another visible copy may still use this logical chunk. The shared cache retains its own LRU limit.
            if (!logicalVisible.Contains(key.Id))
                world.ChunkManager.Unload(new(key.Id % world.ChunkManager.ChunkColumns * world.Configuration.ChunkSize,
                    key.Id / world.ChunkManager.ChunkColumns * world.Configuration.ChunkSize));
        }
    }

    public override void _Draw()
    {
        var w = State.World.Width * tileSize;
        var h = State.World.Height * tileSize;
        var daylight = (float)Math.Clamp(Math.Sin((State.DayFraction - .25) * Math.Tau) * .5 + .5, 0, 1);
        var area = WorldViewport.Bounds(this);
        DrawRect(new(area.Position.X, 0, area.Size.X, h), new Color("111e2b").Lerp(new("527579"), daylight));
        var copies = HorizontalWrap.CopiesIntersecting(area.Position.X, area.End.X, w);
        for (var copy = copies.First; copy <= copies.Last; copy++)
        {
        for (var i = 0; i < 45; i++)
        {
            var x = (i * 137 + 31) % w; var y = (i * 47 + 13) % 150;
            DrawRect(new(x + copy * w, y, 2, 2), new(.8f, .88f, .82f, 1 - daylight));
        }
        DrawCircle(new(w * .72f + copy * w, 85), 17, new Color("d9dbc1").Lerp(new("e4c682"), daylight));
        }
    }
}
