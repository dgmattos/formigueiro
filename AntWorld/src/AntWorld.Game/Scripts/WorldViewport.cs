using Godot;
namespace AntWorld.Game;

public static class WorldViewport
{
    public static Rect2 Bounds(CanvasItem view)
    {
        var inverse = view.GetGlobalTransformWithCanvas().AffineInverse();
        var first = inverse * Vector2.Zero;
        var last = inverse * view.GetViewportRect().Size;
        return new Rect2(first, last - first).Abs();
    }
}
