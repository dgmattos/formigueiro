using AntWorld.Domain;
namespace AntWorld.Infrastructure;

public static class CircularTerrainVisibility
{
    // Bounds and offsets are in logical cells. Margin is one chunk by default.
    public static IEnumerable<VisibleTerrainChunk> Select(WorldConfiguration world,
        double left, double top, double right, double bottom, int margin = 1)
    {
        if (margin < 0 || margin > 2 || !double.IsFinite(top) || !double.IsFinite(bottom) || bottom < top)
            throw new ArgumentException("Área de streaming inválida.");
        var size = world.ChunkSize;
        if (bottom + margin * size <= 0 || top - margin * size >= world.Height) yield break;
        var copies = HorizontalWrap.CopiesIntersecting(left - margin * size, right + margin * size, world.Width);
        var minY = Math.Max(0, (int)Math.Floor(Math.Max(0, top - margin * size) / size));
        var maxY = Math.Min((world.Height - 1) / size, (int)Math.Ceiling(Math.Min(world.Height, bottom + margin * size) / size) - 1);
        var columns = (world.Width + size - 1) / size;
        for (var copy = copies.First; copy <= copies.Last; copy++)
        {
            var offset = checked(copy * world.Width);
            var minX = (int)Math.Floor(Math.Max(0, left - margin * size - offset) / size);
            var maxX = (int)Math.Ceiling(Math.Min(world.Width, right + margin * size - offset) / size) - 1;
            for (var y = minY; y <= maxY; y++)
            for (var x = minX; x <= maxX; x++)
            {
                var region = new ChunkRegion(x * size, y * size, Math.Min(size, world.Width - x * size), Math.Min(size, world.Height - y * size));
                yield return new(y * columns + x, copy, offset, region);
            }
        }
    }
}
