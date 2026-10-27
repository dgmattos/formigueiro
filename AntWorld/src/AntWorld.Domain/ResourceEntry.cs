namespace AntWorld.Domain;

public readonly record struct ResourceEntry(CellPosition Position, ushort ResourceId, ResourceStack Stack);
