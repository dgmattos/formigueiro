namespace AntWorld.Domain;

public sealed record MaterialDefinition(ushort Id, string Name, MaterialFamily Family,
    byte Porosity, byte Permeability, double WaterAffinity);
