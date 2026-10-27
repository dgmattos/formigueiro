namespace AntWorld.Domain;

public readonly record struct ResourceStack(double Quantity, double Capacity, ushort Concentration, int DepositId = 0)
{
    public void Validate()
    {
        if (!double.IsFinite(Quantity) || !double.IsFinite(Capacity) || Quantity < 0 || Quantity > Capacity || Capacity > 1e9 ||
            Concentration > 10000 || DepositId < 0) throw new ArgumentException("Estoque de recurso inválido.");
    }
}
