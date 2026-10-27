namespace AntWorld.Domain;

public sealed record WorldConfiguration(int Seed = 12345, int Width = 240, int Height = 96, int ChunkSize = 16)
{
    public GeologySettings Geology { get; init; } = new();
    public MushroomSettings Mushrooms { get; init; } = new();
    public VegetationSettings Vegetation { get; init; } = new();
    public NestSettings Nests { get; init; } = new();
    public int MaximumLoadedChunks { get; init; } = 64;
    public void Validate()
    {
        if (Width is < 64 or > 8192 || Height is < 48 or > 2048)
            throw new ArgumentOutOfRangeException(nameof(Width), "Mundo: largura 64–8192, altura 48–2048.");
        Geology.Validate();
        Mushrooms.Validate();
        Vegetation.Validate();
        Nests.Validate();
        if (MaximumLoadedChunks is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(MaximumLoadedChunks));
        if (ChunkSize is < 8 or > 64)
            throw new ArgumentOutOfRangeException(nameof(ChunkSize));
    }
}
