using System.Text.Json;
using System.Text.Json.Serialization;
namespace AntWorld.Infrastructure;

public sealed record SimulationLogSettings(bool Enabled = true, int SampleIntervalSeconds = 60, int MaxFileSizeMb = 8, int RetainedFiles = 3)
{
    public void Validate()
    {
        if (SampleIntervalSeconds is < 1 or > 10800 || MaxFileSizeMb is < 1 or > 64 || RetainedFiles is < 1 or > 10)
            throw new ArgumentException("diagnostics: intervalo deve ser 1–10800 segundos simulados, tamanho 1–64 MiB e retenção 1–10 arquivos.");
    }
    public static SimulationLogSettings Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var settings = document.RootElement.TryGetProperty("diagnostics", out var element)
            ? element.Deserialize<SimulationLogSettings>(new JsonSerializerOptions(JsonSerializerDefaults.Web) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow })
                ?? throw new ArgumentException("diagnostics não pode ser null.")
            : new();
        settings.Validate(); return settings;
    }
}
