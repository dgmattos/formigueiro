using AntWorld.Domain;
namespace AntWorld.Simulation;

// Presentation-only path history, allocated only for visible individuals. Never changes simulation state.
public sealed class AgentMotionTrack
{
    public const int MaximumSegments=32;
    private readonly Queue<(double X,double Y,double Angle,double Duration,int Dx,int Dy)> segments=new();
    private CellPosition sample;
    private SupportSide support;
    private double sampledX,fromX,fromY,fromAngle,elapsed;
    public double X { get; private set; }
    public double Y { get; private set; }
    public double Angle { get; private set; }
    public int PendingSegments=>segments.Count;
    public bool Moving=>segments.Count>0;
    public int DirectionX { get; private set; }
    public int DirectionY { get; private set; }
    public AgentMotionTrack(WorldMap world,Ant ant) => Reset(world,ant);
    public void Reset(WorldMap world,Ant ant)
    {
        sample=ant.Position;support=world.ContactSide(sample,ant.Support);var offset=SurfaceOrientation.Offset(support);
        X=sample.X+.5+offset.X*.5;Y=sample.Y+.5+offset.Y*.5;Angle=SurfaceOrientation.Angle(support);
        sampledX=X;segments.Clear();elapsed=0;
        DirectionX=world.HorizontalDelta(ant.PreviousPosition.X,ant.Position.X);DirectionY=ant.Position.Y-ant.PreviousPosition.Y;
    }
    public void Capture(WorldMap world,Ant ant,double speed)
    {
        var side=world.ContactSide(ant.Position,ant.Support);
        if(sample==ant.Position&&side==support)return;
        var dx=world.HorizontalDelta(sample.X,ant.Position.X);var dy=ant.Position.Y-sample.Y;
        if(Math.Abs(dx)>1||Math.Abs(dy)>1||segments.Count>=MaximumSegments){Reset(world,ant);return;}
        var oldOffset=SurfaceOrientation.Offset(support);var offset=SurfaceOrientation.Offset(side);
        sampledX+=dx+(offset.X-oldOffset.X)*.5;
        var duration=sample==ant.Position ? 0.15 : ant.Movement==MovementState.Falling ? SimulationState.TickSeconds:
            Math.Clamp(MovementSystem.StepCost(sample,ant.Position)/Math.Max(.05,speed),SimulationState.TickSeconds,5);
        if(segments.Count==0){fromX=X;fromY=Y;fromAngle=Angle;elapsed=0;}
        segments.Enqueue((sampledX,ant.Position.Y+.5+offset.Y*.5,SurfaceOrientation.Angle(side),duration,dx,dy));
        sample=ant.Position;support=side;
    }
    public void Advance(double simulatedSeconds)
    {
        if(!double.IsFinite(simulatedSeconds)||simulatedSeconds<0)throw new ArgumentOutOfRangeException(nameof(simulatedSeconds));
        while(simulatedSeconds>0&&segments.TryPeek(out var target))
        {
            if(target.Dx!=0||target.Dy!=0){DirectionX=target.Dx;DirectionY=target.Dy;}
            var step=Math.Min(simulatedSeconds,target.Duration-elapsed);elapsed+=step;simulatedSeconds-=step;
            var fraction=Math.Clamp(elapsed/target.Duration,0,1);
            X=fromX+(target.X-fromX)*fraction;Y=fromY+(target.Y-fromY)*fraction;
            var turn=Math.Atan2(Math.Sin(target.Angle-fromAngle),Math.Cos(target.Angle-fromAngle));
            Angle=fromAngle+turn*fraction;
            if(fraction<1)break;
            segments.Dequeue();fromX=X;fromY=Y;fromAngle=Angle;elapsed=0;
        }
    }
}
