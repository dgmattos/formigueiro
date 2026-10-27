using Godot;
namespace AntWorld.Game;

// The transparent atlas has six frames in five rows, without the old labels/grid.
public static class AntSpriteAtlas
{
    public const string ResourcePath = "res://Assets/ants.png";
    public const string StrongResourcePath = "res://Assets/ants-strong.png";
    public const string SurfaceResourcePath = "res://Assets/ants-surface.png";
    public const int FrameCount = 6;
    public const int RowCount = 5;
    private static readonly int[] ColumnEdges = [98, 398, 683, 958, 1233, 1503, 1774];
    private static readonly int[] RowEdges = [50, 260, 440, 585, 708, 860];

    // Measured transparent gaps in the Surface atlas; its artwork starts left of the older atlas.
    private static readonly int[] SurfaceRowEdges = [0, 278, 465, 610, 730, 887];
    private static readonly int[][] SurfaceColumnEdges =
    [
        [0, 376, 677, 983, 1250, 1531, 1774],
        [0, 370, 672, 963, 1241, 1516, 1774],
        [0, 365, 670, 940, 1210, 1487, 1774],
        [0, 365, 670, 940, 1210, 1480, 1774],
        [0, 365, 670, 940, 1210, 1480, 1774]
    ];

    public static Rect2I FrameRegion(int row, int frame, int species = 0)
    {
        if ((uint)row >= RowCount || (uint)frame >= FrameCount) throw new ArgumentOutOfRangeException(nameof(frame));
        var columns = species == 3 ? SurfaceColumnEdges[row] : ColumnEdges;
        var rows = species == 3 ? SurfaceRowEdges : RowEdges;
        return new(columns[frame], rows[row], columns[frame + 1] - columns[frame], rows[row + 1] - rows[row]);
    }

    public static void ValidateImportedTexture(Texture2D texture, int species = 0)
    {
        using var image = texture.GetImage();
        if (image.GetWidth() != 1774 || image.GetHeight() != 887 || image.DetectAlpha() == Image.AlphaMode.None)
            throw new InvalidOperationException("Atlas de indivíduos incompatível ou sem transparência. Recompile e reimporte os atlas de indivíduos.");
        for (var row = 0; row < RowCount; row++)
        for (var frame = 0; frame < FrameCount; frame++)
        {
            using var region = image.GetRegion(FrameRegion(row, frame, species));
            if (region.IsInvisible() || region.DetectAlpha() == Image.AlphaMode.None)
                throw new InvalidOperationException($"Sprite vazio ou opaco: linha {row + 1}, quadro {frame + 1}.");
        }
    }
}
