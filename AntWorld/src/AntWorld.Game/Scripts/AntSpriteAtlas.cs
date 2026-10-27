using Godot;
namespace AntWorld.Game;

// The transparent atlas has six frames in five rows, without the old labels/grid.
public static class AntSpriteAtlas
{
    public const string ResourcePath = "res://Assets/ants.png";
    public const string StrongResourcePath = "res://Assets/ants-strong.png";
    public const int FrameCount = 6;
    public const int RowCount = 5;
    private static readonly int[] ColumnEdges = [98, 398, 683, 958, 1233, 1503, 1774];
    private static readonly int[] RowEdges = [50, 260, 440, 585, 708, 860];

    public static Rect2I FrameRegion(int row, int frame)
    {
        if ((uint)row >= RowCount || (uint)frame >= FrameCount) throw new ArgumentOutOfRangeException(nameof(frame));
        return new(ColumnEdges[frame], RowEdges[row], ColumnEdges[frame + 1] - ColumnEdges[frame], RowEdges[row + 1] - RowEdges[row]);
    }

    public static void ValidateImportedTexture(Texture2D texture)
    {
        using var image = texture.GetImage();
        if (image.GetWidth() != 1774 || image.GetHeight() != 887 || image.DetectAlpha() == Image.AlphaMode.None)
            throw new InvalidOperationException("Atlas de indivíduos incompatível ou sem transparência. Recompile e reimporte Assets/ants.png.");
        for (var row = 0; row < RowCount; row++)
        for (var frame = 0; frame < FrameCount; frame++)
        {
            using var region = image.GetRegion(FrameRegion(row, frame));
            if (region.IsInvisible() || region.DetectAlpha() == Image.AlphaMode.None)
                throw new InvalidOperationException($"Sprite vazio ou opaco: linha {row + 1}, quadro {frame + 1}.");
        }
    }
}
