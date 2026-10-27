namespace AntWorld.Domain;

public readonly record struct ChunkRegion(int X, int Y, int Width, int Height)
{
    public int Index(CellPosition p) => (p.Y - Y) * Width + p.X - X;
    public CellPosition Position(int index) => new(X + index % Width, Y + index / Width);
}
