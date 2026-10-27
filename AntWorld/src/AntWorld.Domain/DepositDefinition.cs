namespace AntWorld.Domain;

public sealed record DepositDefinition(int Id, ushort ResourceId, CellPosition Origin, double Budget, double AllocatedQuantity);
