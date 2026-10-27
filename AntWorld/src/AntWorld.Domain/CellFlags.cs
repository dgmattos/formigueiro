namespace AntWorld.Domain;

[Flags]
public enum CellFlags : byte { None = 0, Modified = 1, Excavated = 2, EnvironmentChanged = 4, VegetationRemoved = 8 }
