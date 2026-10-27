using AntWorld.Domain;
using AntWorld.WorldGeneration;
using Godot;
namespace AntWorld.Game;

public partial class TerrainChunkView : Node2D
{
    private SimulationState state = null!;
    private ChunkRegion region;
    private PreparedSpriteCatalog catalog = null!;
    private int tileSize;
    private int revision = -1;
    private int season = -1;
    private TileMapLayer terrain = null!;
    private Node2D overlay = null!;
    private Node2D water = null!;
    public int RenderedTileCount => terrain.GetUsedCells().Count;
    public bool HasRenderedCell(CellPosition canonical, CellPosition displayed)
    {
        var cell = new Vector2I(canonical.X, canonical.Y);
        var center = Position + terrain.Position + terrain.MapToLocal(cell) * terrain.Scale;
        return terrain.GetCellSourceId(cell) >= 0 && center.IsEqualApprox(new Vector2((displayed.X + .5f) * tileSize, (displayed.Y + .5f) * tileSize));
    }
    public void ValidateAlignment()
    {
        var center = terrain.Position + terrain.MapToLocal(new(region.X, region.Y)) * terrain.Scale;
        var expected = new Vector2((region.X + .5f) * tileSize, (region.Y + .5f) * tileSize);
        if (!center.IsEqualApprox(expected) || terrain.TileSet.TileSize != new Vector2I(16, 16)) throw new InvalidOperationException("Centro do tile desalinhado da grid lógica.");
    }
    public void Initialize(SimulationState simulation, ChunkRegion area, PreparedSpriteCatalog sprites, int pixels)
    {
        state = simulation; region = area; catalog = sprites; tileSize = pixels;
        terrain = new TileMapLayer { Name = "TerrainLayer", TileSet = catalog.TileSet(state.Season),
            CollisionEnabled = false, NavigationEnabled = false, OcclusionEnabled = false,
            RenderingQuadrantSize = state.World.Configuration.ChunkSize, TextureFilter = TextureFilterEnum.Nearest,
            Scale = Vector2.One * tileSize / 16f, ZIndex = 1 };
        terrain.Position = (new Vector2(8, 8) - terrain.MapToLocal(Vector2I.Zero)) * tileSize / 16f;
        AddChild(terrain);
        overlay = new Node2D { Name = "TerrainOverlayLayer", ZIndex = 2 }; overlay.Draw += DrawOverlay; AddChild(overlay);
        water = new Node2D { Name = "WaterLayer", ZIndex = 3 }; water.Draw += DrawWater; AddChild(water);
        Refresh();
    }
    public override void _Process(double delta) => Refresh();
    private void Refresh()
    {
        var current = state.World.ChunkRevision(region.X, region.Y);
        if (current == revision && season == state.Season) return;
        revision = current; season = state.Season;
        terrain.TileSet = catalog.TileSet(season); terrain.Clear();
        var chunk = state.World.ChunkManager.Load(new(region.X, region.Y));
        for (var i = 0; i < chunk.Cells.Length; i++)
        {
            var cell = chunk.Cells.Span[i]; if (cell.SolidFill == 0) continue;
            var p = region.Position(i); var visual = TerrainAppearanceResolver.Resolve(state.World, p);
            terrain.SetCell(new(p.X, p.Y), 0, catalog.TerrainCoordinates(visual, season, cell.SolidFill));
        }
        QueueRedraw(); overlay.QueueRedraw(); water.QueueRedraw();
    }
    public override void _Draw()
    {
        for (var i = 0; i < region.Width * region.Height; i++)
        {
            var p = region.Position(i);
            if (p.Y > state.World.Surface[p.X]) DrawRect(new(p.X * tileSize, p.Y * tileSize, tileSize, tileSize), new("171c23"));
        }
    }
    private void DrawOverlay()
    {
        var world = state.World;
        for (var i = 0; i < region.Width * region.Height; i++)
        {
            var p = region.Position(i); var cell = world.GetCell(p, false);
            var height = Math.Clamp((int)Math.Round(cell.SolidFill * tileSize / 255.0), 0, tileSize);
            var rect = new Rect2(p.X * tileSize, (p.Y + 1) * tileSize - height, tileSize, height);
            if (height > 0)
            {
                var edges = world.Exposure(p);
                if (edges.HasFlag(ExposedSides.Left)) overlay.DrawLine(rect.Position, new(rect.Position.X, rect.End.Y), new("675843"));
                if (edges.HasFlag(ExposedSides.Right)) overlay.DrawLine(new(rect.End.X - 1, rect.Position.Y), new(rect.End.X - 1, rect.End.Y), new("42392f"));
                if (edges.HasFlag(ExposedSides.Bottom)) overlay.DrawLine(new(rect.Position.X, rect.End.Y - 1), rect.End - Vector2.One, new("302c29"));
            }
            if (world.GetResource(p, ResourceIds.Iron).Quantity > 0) overlay.DrawRect(new(p.X * tileSize + 4, p.Y * tileSize + 6, 3, 2), new("d7a472"));
            if (world.GetResource(p, ResourceIds.RedMushroom).Quantity > 0)
                catalog.Draw(overlay, catalog.Object("mushroom_red", state.Season), state.Season, new(p.X * tileSize + tileSize / 4, rect.Position.Y), tileSize, .5f);
            if (world.GetResource(p, ResourceIds.BrownMushroom).Quantity > 0)
                catalog.Draw(overlay, catalog.Object("mushroom_brown", state.Season), state.Season, new(p.X * tileSize + tileSize * 3 / 4, rect.Position.Y), tileSize, .5f);
        }
    }
    private void DrawWater()
    {
        for (var i = 0; i < region.Width * region.Height; i++)
        {
            var p = region.Position(i); var amount = state.World.GetCell(p, false).WaterAmount;
            if (amount == 0) continue;
            var height = (int)Math.Clamp(amount / 100.0 * tileSize / 10, 1, tileSize);
            water.DrawRect(new(p.X * tileSize, (p.Y + 1) * tileSize - height, tileSize, height), new(.3f, .65f, .8f, .5f));
        }
    }
}
