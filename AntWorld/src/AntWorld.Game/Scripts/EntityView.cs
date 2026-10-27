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
    private readonly Dictionary<int,AgentMotionTrack> motion=[];
    private readonly HashSet<int> visibleIds=[];
    public double PresentationTime { get; set; }
    private double previousPresentationTime;
    public void CaptureMovement()
    {
        foreach(var ant in rendered)
        {
            if(!ant.IsAlive||!motion.TryGetValue(ant.Id,out var track))continue;
            var carrier=ant.CarrierId is {} id?rendered.Find(a=>a.Id==id):null;
            track.Capture(State.World,ant,carrier?.MovementSpeed??ant.MovementSpeed);
        }
    }
    private Texture2D antAtlas=null!;
    private Texture2D strongAtlas=null!;
    private Texture2D surfaceAtlas=null!;
    public int RenderedCount { get; private set; }
    private int[] resourceCells = [];
    private int Cell;
    public void Initialize(SimulationState state, int tileSize)
    {
        State = state; Cell = tileSize;
        PresentationTime=previousPresentationTime=state.Tick*SimulationState.TickSeconds;
        TextureFilter = TextureFilterEnum.Nearest;
        antAtlas=GD.Load<Texture2D>(AntSpriteAtlas.ResourcePath);
        strongAtlas=GD.Load<Texture2D>(AntSpriteAtlas.StrongResourcePath);
        surfaceAtlas=GD.Load<Texture2D>(AntSpriteAtlas.SurfaceResourcePath);
        resourceCells = state.World.Resources().Where(e => e.ResourceId == ResourceIds.Food).Select(e => state.World.Index(e.Position)).ToArray();
    }
    public override void _Process(double delta)
    {
        var elapsed=Math.Max(0,PresentationTime-previousPresentationTime);previousPresentationTime=PresentationTime;
        foreach(var track in motion.Values)track.Advance(elapsed);
        QueueRedraw();
    }
    public static Color SpeciesColor(int species) => species switch { 3 => new("b8cf42"), 2 => new("bd83ed"), 1 => new("82dbb5"), _ => new("f3997f") };
    public override void _Draw()
    {
        var bounds = WorldViewport.Bounds(this).Grow(Cell * 2);
        var world = State.World;
        AgentRenderSelection.Select(State,new((int)Math.Floor(bounds.Position.X/Cell),(int)Math.Floor(bounds.Position.Y/Cell)),
            new((int)Math.Ceiling(bounds.End.X/Cell),(int)Math.Ceiling(bounds.End.Y/Cell)),rendered);
        visibleIds.Clear();
        foreach(var ant in rendered)
        {
            visibleIds.Add(ant.Id);
            if(!motion.ContainsKey(ant.Id))motion.Add(ant.Id,new(world,ant));
        }
        foreach(var id in motion.Keys.Where(id=>!visibleIds.Contains(id)).ToArray())motion.Remove(id);
        var copies = HorizontalWrap.CopiesIntersecting(bounds.Position.X, bounds.End.X, world.Width * Cell);
        RenderedCount = 0;
        for (var copy = copies.First; copy <= copies.Last; copy++)
        {
            var offset = new Vector2(copy * world.Width * Cell, 0);
            DrawSetTransform(offset);
            DrawWorldCopy(new(bounds.Position - offset, bounds.Size), offset);
        }
        DrawSetTransform(Vector2.Zero);
    }
    private void DrawWorldCopy(Rect2 bounds, Vector2 copyOffset)
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
            var pose=ant.CarrierId is {} carrierId&&motion.TryGetValue(carrierId,out var carrierPose)?carrierPose:motion[ant.Id];
            var position=new Vector2((float)HorizontalWrap.Normalize(pose.X,world.Width)*Cell,(float)pose.Y*Cell);
            var row=ant.Stage switch { LifeStage.Egg=>4,LifeStage.Larva or LifeStage.Pupa=>3,_=>ant.Caste switch { AntCaste.Queen=>0,AntCaste.Soldier=>1,_=>2 } };
            var frame=pose.Moving ? (int)(PresentationTime/.1%AntSpriteAtlas.FrameCount) : 0;
            var size=row==0?new Vector2(20,12):row==1?new(16,10):new Vector2(12,7);
            size *= Cell / 10f;
            if (!new Rect2(position-size, size*2).Intersects(bounds)) continue;
            RenderedCount++;
            var support = world.ContactSide(ant.Position,ant.Support);
            var angle = (float)pose.Angle;
            var pivot = position;
            var rect = new Rect2(new(-size.X/2,support == SupportSide.None ? -size.Y/2 : -size.Y),size);
            var dx = pose.DirectionX;
            var dy = pose.DirectionY;
            var tangentMotion = dx*Math.Cos(angle)+dy*Math.Sin(angle);
            if(tangentMotion<0) { rect.Position+=new Vector2(size.X,0); rect.Size=new(-size.X,size.Y); }
            var atlas = ant.SpeciesId switch { 3 => surfaceAtlas, 2 => strongAtlas, _ => antAtlas };
            DrawSetTransform(copyOffset+pivot,angle);
            DrawTextureRectRegion(atlas,rect,AntSpriteAtlas.FrameRegion(row,frame,ant.SpeciesId));
            if(ant.Carrying>.1) DrawRect(new(0,-size.Y-2,2,2),new(ExcavationSpoil.Is(world,ant.CargoResourceId) ? "9b8c7b" : "f3d988"));
            DrawSetTransform(copyOffset);
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
