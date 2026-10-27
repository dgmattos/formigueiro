using System.Runtime.InteropServices;
namespace AntWorld.Domain;

// 16 bytes, no references. RockFraction describes the SOLID fraction, not the entire cell.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct WorldCell
{
    public byte SolidFill { get; init; }
    public byte RockFraction { get; init; }
    public ushort SoilMaterial { get; init; }
    public ushort RockMaterial { get; init; }
    public byte Moisture { get; init; }
    public ushort WaterAmount { get; init; }
    public short Temperature { get; init; } // Tenths of a Celsius degree.
    public byte Porosity { get; init; }
    public byte Permeability { get; init; }
    public ushort Nutrients { get; init; }
    public CellFlags Flags { get; init; }
    public BlockType Type => SolidFill == 0 ? BlockType.Air : RockFraction == 255 ? BlockType.Rock :
        RockFraction > 0 ? BlockType.SoilAndRock : SolidFill < 255 ? BlockType.AirAndSoil : BlockType.Soil;
    public double RetentionCapacity
    {
        get
        {
            if (SolidFill == 0) return 0;
            var rock = RockFraction / 255.0;
            var affinity = (rock < 1 ? MaterialCatalog.Get(SoilMaterial).WaterAffinity * (1 - rock) : 0) +
                (rock > 0 ? MaterialCatalog.Get(RockMaterial).WaterAffinity * rock : 0);
            return 1000 * SolidFill / 255.0 * Porosity / 255.0 * affinity;
        }
    }
    public double RetainedWater => RetentionCapacity * Moisture / 100;
    public bool IsPassable => SolidFill < 96;
    public void Validate()
    {
        if (Moisture > 100 || (SolidFill == 0 && (Moisture != 0 || RockFraction != 0 || Porosity != 0 || Permeability != 0)))
            throw new ArgumentException("Umidade/composição de célula inválida.");
        if (SolidFill > 0 && RockFraction < 255 && MaterialCatalog.Get(SoilMaterial).Family != MaterialFamily.Soil)
            throw new ArgumentException("Material de solo incompatível.");
        if (SolidFill > 0 && RockFraction > 0 && MaterialCatalog.Get(RockMaterial).Family != MaterialFamily.Rock)
            throw new ArgumentException("Material rochoso incompatível.");
        if (Flags.HasFlag(CellFlags.Excavated) && SolidFill != 0) throw new ArgumentException("Célula escavada deve estar aberta.");
        if ((Flags & ~(CellFlags.Modified | CellFlags.Excavated | CellFlags.EnvironmentChanged | CellFlags.VegetationRemoved)) != 0)
            throw new ArgumentException("Flags desconhecidas.");
    }
}
