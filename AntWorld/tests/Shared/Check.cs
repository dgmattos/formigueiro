namespace AntWorld.Specifications;

public static class Check
{
    public static void That(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    public static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Esperava {typeof(T).Name}.");
    }
}
