namespace AntWorld.Domain;
public readonly record struct AntIdentity(int Id, int ColonyId, int SpeciesId, long LineageId, int? MotherId, int? FatherId);
