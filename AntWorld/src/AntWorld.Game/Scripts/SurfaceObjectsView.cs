using AntWorld.Domain;
using Godot;
namespace AntWorld.Game;

public partial class SurfaceObjectsView : Node2D
{
    private SimulationState state = null!;
    private PreparedSpriteCatalog catalog = null!;
    private int tileSize;
    private bool wood;
    private Rect2 previousBounds;
    private int revision = -1;
    private int season = -1;
    public int RenderedObjects { get; private set; }
    public void Initialize(SimulationState simulation, PreparedSpriteCatalog sprites, int pixels, bool woodObjects)
    { state = simulation; catalog = sprites; tileSize = pixels; wood = woodObjects; TextureFilter = TextureFilterEnum.Nearest; }
    public override void _Process(double delta)
    {
        var bounds = WorldViewport.Bounds(this);
        if (bounds == previousBounds && revision == state.World.Vegetation.Revision && season == state.Season) return;
        previousBounds = bounds; revision = state.World.Vegetation.Revision; season = state.Season; QueueRedraw();
    }
    public override void _Draw()
    {
        var bounds = WorldViewport.Bounds(this); var world = state.World; RenderedObjects = 0;
        var copies = HorizontalWrap.CopiesIntersecting(bounds.Position.X - 6 * tileSize, bounds.End.X + 6 * tileSize, world.Width * tileSize);
        foreach (var instance in world.Vegetation.QueryColumns(world, (int)Math.Floor(bounds.Position.X / tileSize), (int)Math.Ceiling(bounds.End.X / tileSize)))
        {
            var isWood = instance.Kind is VegetationKind.FallenLog or VegetationKind.TreeStump or VegetationKind.DeadWood;
            if (isWood != wood) continue;
            var sprite = catalog.Object(instance.Kind, state.Season);
            // One logical owner; wrapped visual copies are only drawn if their global bounds intersect the camera.
            for (var shift = copies.First; shift <= copies.Last; shift++)
            {
                var anchor = new Vector2((instance.Base.X + .5f + shift * world.Width) * tileSize, instance.Base.Y * tileSize);
                if (!PreparedSpriteCatalog.Destination(sprite, anchor, tileSize).Intersects(bounds)) continue;
                catalog.Draw(this, sprite, state.Season, anchor, tileSize); RenderedObjects++;
            }
        }
    }
}
