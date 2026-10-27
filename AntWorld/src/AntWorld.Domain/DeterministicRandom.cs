namespace AntWorld.Domain;

// Explicit, serializable PRNG state. No System.Random/runtime-specific sequence.
public sealed class DeterministicRandom
{
    public ulong State { get; private set; }
    public DeterministicRandom(ulong state) => State = state == 0 ? 0x9E3779B97F4A7C15UL : state;
    public static DeterministicRandom FromSeed(int seed, uint stream = 0) => new(unchecked((ulong)(uint)seed + 0x9E3779B97F4A7C15UL * (stream + 1)));
    public double NextDouble()
    {
        var x = State;
        x ^= x >> 12; x ^= x << 25; x ^= x >> 27;
        State = x;
        return (unchecked(x * 2685821657736338717UL) >> 11) * (1.0 / 9007199254740992.0);
    }
    public int NextInt(int exclusiveMaximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exclusiveMaximum);
        return (int)(NextDouble() * exclusiveMaximum);
    }
}
