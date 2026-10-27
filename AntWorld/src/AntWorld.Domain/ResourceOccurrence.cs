namespace AntWorld.Domain;

public sealed record ResourceOccurrence(byte BlockMask, double MinimumDepth = 0, double MaximumDepth = 1, byte MinimumMoisture = 0)
{
    public bool Allows(WorldCell cell, double depth) => (BlockMask & (1 << (int)cell.Type)) != 0 &&
        depth >= MinimumDepth && depth <= MaximumDepth && cell.Moisture >= MinimumMoisture;
}
