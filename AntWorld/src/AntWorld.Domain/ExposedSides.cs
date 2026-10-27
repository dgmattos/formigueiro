namespace AntWorld.Domain;

[Flags]
public enum ExposedSides : byte { None = 0, Left = 1, Right = 2, Top = 4, Bottom = 8 }
