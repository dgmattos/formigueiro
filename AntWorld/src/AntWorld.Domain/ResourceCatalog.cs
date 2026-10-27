namespace AntWorld.Domain;

public sealed class ResourceCatalog
{
    private readonly Dictionary<ushort, ResourceDefinition> definitions = [];
    public IEnumerable<ResourceDefinition> Definitions => definitions.Values.OrderBy(r => r.Id);
    public ResourceCatalog()
    {
        Register(new(ResourceIds.Food, "Alimento", "food", true, .03, new(3)));
        Register(new(ResourceIds.Iron, "Ferro", "mineral", true, 0, new(24)));
        Register(new(ResourceIds.OrganicMatter, "Matéria orgânica", "organic", true, .002, new(14)));
        Register(new(ResourceIds.Fungus, "Fungos", "fungus", true, .005, new(15, .05, .6, 30)));
        // Discrete births are handled by MushroomGrowth, not generic continuous regeneration.
        Register(new(ResourceIds.RedMushroom, "Cogumelo vermelho", "food", true, 0, new(12)));
        Register(new(ResourceIds.BrownMushroom, "Cogumelo marrom", "food", true, 0, new(12)));
    }
    public void Register(ResourceDefinition definition)
    {
        definition.Validate();
        if (!definitions.TryAdd(definition.Id, definition)) throw new ArgumentException("Identificador de recurso já registrado.");
    }
    public ResourceDefinition Get(ushort id) => definitions.TryGetValue(id, out var definition)
        ? definition : throw new ArgumentOutOfRangeException(nameof(id), "Recurso desconhecido.");
}
