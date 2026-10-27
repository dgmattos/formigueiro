using AntWorld.Domain;
using Godot;
namespace AntWorld.Game;

public partial class WorldView : Node2D
{
    private readonly Dictionary<int, TerrainChunkView> terrainViews = [];
    private (int, int, int, int)? previousBounds;
    private PreparedSpriteCatalog sprites = null!;
    private int tileSize;
    public SimulationState State { get; private set; } = null!;
    public EntityView Entities { get; private set; } = null!;
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
        var world = State.World; var size = world.Configuration.ChunkSize;
        var inverse = GetGlobalTransformWithCanvas().AffineInverse();
        var a = inverse * Vector2.Zero; var b = inverse * GetViewportRect().Size;
        var minX = Math.Max(0, (int)Math.Floor(Math.Min(a.X, b.X) / (size * tileSize)) - 1);
        var maxX = Math.Min(world.ChunkManager.ChunkColumns - 1, (int)Math.Floor(Math.Max(a.X, b.X) / (size * tileSize)) + 1);
        var minY = Math.Max(0, (int)Math.Floor(Math.Min(a.Y, b.Y) / (size * tileSize)) - 1);
        var maxY = Math.Min((world.Height - 1) / size, (int)Math.Floor(Math.Max(a.Y, b.Y) / (size * tileSize)) + 1);
        var bounds = (minX, maxX, minY, maxY);
        if (previousBounds == bounds) return;
        previousBounds = bounds;
        var visible = new HashSet<int>();
        for (var y = minY; y <= maxY; y++)
        for (var x = minX; x <= maxX; x++)
        {
            var p = new CellPosition(x * size, y * size); var id = world.ChunkManager.ChunkId(p); visible.Add(id);
            if (terrainViews.ContainsKey(id)) continue;
            var view = new TerrainChunkView { TextureFilter = TextureFilterEnum.Nearest };
            view.Initialize(State, world.ChunkManager.Region(p), sprites, tileSize); terrainViews[id] = view; AddChild(view);
        }
        foreach (var id in terrainViews.Keys.Where(id => !visible.Contains(id)).ToArray())
        {
            RemoveChild(terrainViews[id]); terrainViews[id].QueueFree(); terrainViews.Remove(id);
            world.ChunkManager.Unload(new(id % world.ChunkManager.ChunkColumns * size, id / world.ChunkManager.ChunkColumns * size));
        }
    }
    public override void _Draw()
    {
        var w = State.World.Width * tileSize;
        var h = State.World.Height * tileSize;
        var daylight = (float)Math.Clamp(Math.Sin((State.DayFraction - .25) * Math.Tau) * .5 + .5, 0, 1);
        DrawRect(new(0, 0, w, h), new Color("111e2b").Lerp(new("527579"), daylight));
        for (var i = 0; i < 45; i++)
        {
            var x = (i * 137 + 31) % w; var y = (i * 47 + 13) % 150;
            DrawRect(new(x, y, 2, 2), new(.8f, .88f, .82f, 1 - daylight));
        }
        DrawCircle(new(w * .72f, 85), 17, new Color("d9dbc1").Lerp(new("e4c682"), daylight));
    }
}
