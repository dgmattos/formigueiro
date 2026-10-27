namespace AntWorld.Domain;

public sealed record ResourceDefinition(ushort Id, string Name, string Kind, bool Extractable,
    double Regeneration, ResourceOccurrence Occurrence)
{
    public void Validate()
    {
        if (Id == 0 || string.IsNullOrWhiteSpace(Name) || Name.Length > 80 || string.IsNullOrWhiteSpace(Kind) || Kind.Length > 80 ||
            !double.IsFinite(Regeneration) || Regeneration < 0 || !double.IsFinite(Occurrence.MinimumDepth) ||
            !double.IsFinite(Occurrence.MaximumDepth) || Occurrence.MinimumDepth < 0 || Occurrence.MaximumDepth > 1 ||
            Occurrence.MaximumDepth < Occurrence.MinimumDepth || Occurrence.BlockMask is 0 or > 31 || Occurrence.MinimumMoisture > 100)
            throw new ArgumentException("Definição de recurso inválida.");
    }
}
