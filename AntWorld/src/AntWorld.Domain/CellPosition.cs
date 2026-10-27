namespace AntWorld.Domain;

public readonly record struct CellPosition(int X, int Y)
{
    public int DistanceTo(CellPosition other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
}
