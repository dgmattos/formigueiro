namespace AntWorld.Domain;

public static class HorizontalWrap
{
    public static double Normalize(double coordinate, double width)
    {
        Validate(coordinate, coordinate, width);
        return (coordinate % width + width) % width;
    }
    // Half-open view bounds. Copy indices are visual offsets, not new logical worlds.
    public static (int First, int Last) CopiesIntersecting(double minimum, double maximum, double width)
    {
        Validate(minimum, maximum, width);
        if (minimum == maximum) return (0, -1);
        return (checked((int)Math.Floor(minimum / width)), checked((int)Math.Ceiling(maximum / width)) - 1);
    }
    public static bool Contains(int x, int minimum, int maximum, int width)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (maximum < minimum) return false;
        var span = (long)maximum - minimum;
        return span >= width - 1 || (((long)x - minimum) % width + width) % width <= span;
    }
    private static void Validate(double minimum, double maximum, double width)
    {
        if (!double.IsFinite(minimum) || !double.IsFinite(maximum) || !double.IsFinite(width) || width <= 0 || maximum < minimum)
            throw new ArgumentException("Intervalo horizontal inválido.");
    }
}
