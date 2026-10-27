using AntWorld.Domain;
using Godot;
using AntWorld.Simulation;
namespace AntWorld.Game;

public partial class EntityView : Node2D
{
    public SimulationState State { get; set; } = null!;
    public bool ShowPheromones { get; set; }
    public int? SelectedColony { get; set; }
    private readonly List<Ant> rendered=[];
    private Texture2D antAtlas=null!;
    private Texture2D strongAtlas=null!;
    private Texture2D surfaceAtlas=null!;
    public int RenderedCount { get; private set; }
    private int[] resourceCells = [];
    private int Cell;
    public void Initialize(SimulationState state, int tileSize)
    {
        State = state; Cell = tileSize;
        TextureFilter = TextureFilterEnum.Nearest;
        antAtlas=GD.Load<Texture2D>(AntSpriteAtlas.ResourcePath);
        strongAtlas=GD.Load<Texture2D>(AntSpriteAtlas.StrongResourcePath);
        surfaceAtlas=GD.Load<Texture2D>(AntSpriteAtlas.SurfaceResourcePath);
        resourceCells = state.World.Resources().Where(e => e.ResourceId == ResourceIds.Food).Select(e => state.World.Index(e.Position)).ToArray();
    }
    public override void _Process(double delta) => QueueRedraw();
    public static Color SpeciesColor(int species) => species switch { 3 => new("b8cf42"), 2 => new("bd83ed"), 1 => new("82dbb5"), _ => new("f3997f") };
    public override void _Draw()
    {
        var bounds = WorldViewport.Bounds(this).Grow(Cell * 2);
        var world = State.World;
        AgentRenderSelection.Select(State,new((int)Math.Floor(bounds.Position.X/Cell),(int)Math.Floor(bounds.Position.Y/Cell)),
            new((int)Math.Ceiling(bounds.End.X/Cell),(int)Math.Ceiling(bounds.End.Y/Cell)),rendered);
        var copies = HorizontalWrap.CopiesIntersecting(bounds.Position.X, bounds.End.X, world.Width * Cell);
        RenderedCount = 0;
        for (var copy = copies.First; copy <= copies.Last; copy++)
        {
            var offset = new Vector2(copy * world.Width * Cell, 0);
            DrawSetTransform(offset);
            DrawWorldCopy(new(bounds.Position - offset, bounds.Size));
        }
        DrawSetTransform(Vector2.Zero);
    }
    private void DrawWorldCopy(Rect2 bounds)
    {
        var world = State.World;
        if (ShowPheromones)
        foreach (var colony in State.Colonies)
        {
            var trail = State.Pheromones[colony.Id];
            foreach(var (i,intensity) in trail.Entries(PheromoneType.FoodTrail))
                if (intensity > .2) DrawRect(new(i % world.Width * Cell, i / world.Width * Cell, Cell, Cell), SpeciesColor(colony.Species) with { A = (float)Math.Min(.45, intensity / 25) });
        }
        foreach (var i in resourceCells)
        {
            if (world.Food[i] < .5) continue;
            var size = (float)Math.Clamp(world.Food[i] / 5, 2, 6);
            DrawRect(new(i % world.Width * Cell + 3, i / world.Width * Cell + 10 - size, 4, size), new("debd72"));
        }
        foreach (var (key, stack) in world.ChunkManager.ResourceChanges)
        {
            if (stack.Quantity <= 0 || !ExcavationSpoil.Is(world, key.ResourceId)) continue;
            var p = world.Position(key.CellIndex);
            var rect = new Rect2(p.X * Cell + 2, (p.Y + 1) * Cell - 4, Cell - 4, 4);
            if (rect.Intersects(bounds)) DrawRect(rect, new("9b8c7b"));
        }
        foreach(var ant in rendered)
        {
            if (RenderedCount >= State.Performance.MaxRenderedAgents) break;
            var position=new Vector2(ant.Position.X*Cell+Cell/2f,ant.Position.Y*Cell+Cell*.6f);
            var row=ant.Stage switch { LifeStage.Egg=>4,LifeStage.Larva or LifeStage.Pupa=>3,_=>ant.Caste switch { AntCaste.Queen=>0,AntCaste.Soldier=>1,_=>2 } };
            var frame=ant.Movement is MovementState.Idle ? 0 : (int)(State.Tick/4%AntSpriteAtlas.FrameCount);
            var size=row==0?new Vector2(20,12):row==1?new(16,10):new Vector2(12,7);
            size *= Cell / 10f;
            var rect=new Rect2((position-size/2).Round(),size.Round());
            if (!rect.Intersects(bounds)) continue;
            RenderedCount++;
            if(world.HorizontalDelta(ant.PreviousPosition.X,ant.Position.X)<0) { rect.Position+=new Vector2(size.X,0); rect.Size=new(-size.X,size.Y); }
            var atlas = ant.SpeciesId switch { 3 => surfaceAtlas, 2 => strongAtlas, _ => antAtlas };
            DrawTextureRectRegion(atlas,rect,AntSpriteAtlas.FrameRegion(row,frame,ant.SpeciesId));
            if(ant.Carrying>.1) DrawRect(new(position.X,position.Y-5,2,2),new(ExcavationSpoil.Is(world,ant.CargoResourceId) ? "9b8c7b" : "f3d988"));
        }
        if (State.RainIntensity <= 0) return;
        for (var i = 0; i < 90; i++)
        {
            var x = (i * 79 + State.Tick * 2) % (world.Width * Cell);
            if (BiomeProfile.For(world.Biomes[(int)x / Cell]).RainChance < .2) continue;
            var y = (i * 53 + State.Tick * 9) % (world.Surface[(int)x / Cell] * Cell);
            DrawLine(new(x, y), new(x - 2, y + 9), new(.65f, .8f, .86f, .35f));
        }
    }
}
