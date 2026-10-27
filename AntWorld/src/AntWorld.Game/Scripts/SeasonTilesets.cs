using Godot;
namespace AntWorld.Game;

public sealed class SeasonTilesets
{
    private readonly Dictionary<int, Texture2D> textures = [];
    public SeasonTilesets()
    {
        string[] names=["spring","summer","autumn","winter"];
        for(var season=0;season<names.Length;season++)
            Register(season,GD.Load<Texture2D>($"res://Assets/grassland-{names[season]}.png"));
    }
    public Texture2D ForSeason(int season) => textures.TryGetValue(season,out var texture) ? texture : throw new ArgumentOutOfRangeException(nameof(season));
    public void Register(int season, Texture2D atlas)
    {
        if (season is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(season));
        textures[season] = atlas ?? throw new ArgumentNullException(nameof(atlas));
    }
}
