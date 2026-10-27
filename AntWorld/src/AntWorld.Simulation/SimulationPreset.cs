using System.Text.Json;
using System.Text.Json.Serialization;
using AntWorld.Domain;
namespace AntWorld.Simulation;

public sealed record SimulationPreset(WorldConfiguration World, IReadOnlyList<ColonySpawnSettings> Colonies)
{
    public static IReadOnlyList<ColonySpawnSettings> DefaultColonies { get; } =
        Array.AsReadOnly(new[] { new ColonySpawnSettings(0, .25), new ColonySpawnSettings(1, .75) });
    public static SimulationPreset Default { get; } = new(new(), DefaultColonies);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public void Validate()
    {
        World.Validate();
        if (Colonies.Count is < 1 or > ColonySystem.MaximumColonies)
            throw new ArgumentException($"settings.json: colonies deve conter entre 1 e {ColonySystem.MaximumColonies} colônias.");
        long population = 0;
        var nestColumns = new HashSet<int>();
        foreach (var colony in Colonies)
        {
            if (colony is null) throw new ArgumentException("Colônia inicial não pode ser null.");
            colony.Validate();
            population += colony.Population;
            if (!nestColumns.Add((int)(World.Width * colony.NestPositionRatio)))
                throw new ArgumentException("Colônias iniciais devem ter posições de ninho distintas na grid.");
        }
        if (population > new PerformanceProfile().MaximumPopulation)
            throw new ArgumentException("A população inicial excede o limite do perfil de desempenho.");
    }

    public static SimulationPreset Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var world = root.TryGetProperty("world", out var worldJson)
                ? worldJson.Deserialize<WorldConfiguration>(JsonOptions) ?? throw new ArgumentException("world não pode ser null.")
                : new WorldConfiguration();
            IReadOnlyList<ColonySpawnSettings> colonies = root.TryGetProperty("colonies", out var coloniesJson)
                ? coloniesJson.Deserialize<ColonySpawnSettings[]>(JsonOptions) ?? throw new ArgumentException("colonies não pode ser null.")
                : DefaultColonies;
            var preset = new SimulationPreset(world, colonies);
            preset.Validate();
            return preset;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            throw new ArgumentException("settings.json inválido: confira os campos world e colonies.", ex);
        }
    }
}
