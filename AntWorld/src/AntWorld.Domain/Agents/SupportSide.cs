namespace AntWorld.Domain;

public enum SupportSide : byte { None, Down, Left, Up, Right }

public static class SurfaceOrientation
{
    // Atlas feet point along local +Y. Positive angles rotate clockwise in screen coordinates.
    public static double Angle(SupportSide side) => side switch
    { SupportSide.Left => Math.PI / 2, SupportSide.Up => Math.PI, SupportSide.Right => -Math.PI / 2, _ => 0 };
    public static CellPosition Offset(SupportSide side) => side switch
    { SupportSide.Down => new(0,1), SupportSide.Left => new(-1,0), SupportSide.Up => new(0,-1), SupportSide.Right => new(1,0), _ => new(0,0) };
}
