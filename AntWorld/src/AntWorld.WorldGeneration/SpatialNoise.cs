using AntWorld.Domain;
namespace AntWorld.WorldGeneration;

internal static class SpatialNoise
{
    public static double Unit(int seed, int x, int y, uint stream)
    {
        var hash = unchecked((uint)seed ^ (uint)x * 0x9E3779B9U ^ (uint)y * 0x85EBCA6BU ^ stream * 0xC2B2AE35U);
        hash ^= hash >> 16; hash = unchecked(hash * 0x7FEB352DU); hash ^= hash >> 15;
        hash = unchecked(hash * 0x846CA68BU); hash ^= hash >> 16;
        return hash / 4294967296.0;
    }
}
