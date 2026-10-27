using AntWorld.Domain;
namespace AntWorld.Simulation;

// Sparse, non-regenerating pantry stock. Kept separate from wild food to prevent collection loops.
public static class LocalProvisions
{
    private const string Kind="colony-provisions";
    private static ushort FindId(WorldMap world) => world.ResourceCatalog.Definitions.FirstOrDefault(d=>d.Kind==Kind)?.Id??0;
    private static ushort EnsureId(WorldMap world)
    {
        var existing=FindId(world);if(existing!=0)return existing;
        var used=world.ResourceCatalog.Definitions.Select(d=>d.Id).ToHashSet();
        for(var value=1;value<=ushort.MaxValue;value++)
        {
            var id=(ushort)value;if(used.Contains(id))continue;
            world.ResourceCatalog.Register(new(id,"Provisões da colônia",Kind,true,0,new(31)));return id;
        }
        throw new InvalidOperationException("Catálogo sem identificador disponível para provisões.");
    }
    public static void Deposit(WorldMap world,CellPosition position,double amount)
    {
        var id=EnsureId(world);var stock=world.GetResource(position,id);
        var quantity=stock.Quantity+amount;
        world.SetResource(position,id,new(quantity,Math.Max(stock.Capacity,quantity),10000));
    }
    public static double Amount(WorldMap world,CellPosition position)
    {var id=FindId(world);return id==0?0:world.GetResource(position,id).Quantity;}
    public static double Consume(WorldMap world,CellPosition position,double amount)
    {var id=FindId(world);return id==0?0:world.Extract(position,id,amount);}
}
