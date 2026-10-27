using AntWorld.Domain;
namespace AntWorld.WorldGeneration;

public static class NestGeometry
{
    public static CellPosition[] Ellipse(WorldMap world, CellPosition center, int rx, int ry)
    {
        var cells=new List<CellPosition>();
        for(var y=-ry;y<=ry;y++) for(var x=-rx;x<=rx;x++)
            if(x*x*ry*ry+y*y*rx*rx<=rx*rx*ry*ry) cells.Add(world.Normalize(new(center.X+x,center.Y+y)));
        return cells.ToArray();
    }
    public static HashSet<CellPosition> Footprint(NestLayout layout) => layout.Chambers.Where(c=>c.Status!=NestStructureStatus.Cancelled).SelectMany(c=>c.Cells)
        .Concat(layout.Tunnels.Where(t=>t.Status!=NestStructureStatus.Cancelled).SelectMany(t=>t.Cells))
        .Concat(layout.Entrances.Where(e=>e.Status!=NestStructureStatus.Cancelled).SelectMany(e=>e.Path)).ToHashSet();
    public static HashSet<CellPosition> Reservations(WorldMap world,IReadOnlyCollection<NestLayout> layouts)
    {
        var reserved=new HashSet<CellPosition>();
        foreach(var p in layouts.SelectMany(Footprint))
        for(var dy=-1;dy<=1;dy++)for(var dx=-1;dx<=1;dx++)
        {
            var q=new CellPosition(p.X+dx,p.Y+dy);if(world.Contains(q))reserved.Add(world.Normalize(q));
        }
        return reserved;
    }
    public static bool Fits(WorldMap world,NestLayout layout,NestChamber chamber,IReadOnlyCollection<NestLayout> others)
    {
        var spacing=world.Configuration.NestLayout.MinimumChamberSpacing;
        if(chamber.Cells.Any(p=>p.Y<world.Surface[p.X] || p.Y>=world.Height-2 || world.GetCell(p,false).WaterAmount>=200)) return false;
        foreach(var other in layout.Chambers.Concat(others.SelectMany(n=>n.Chambers)))
            if(Math.Abs(world.HorizontalDelta(chamber.Center.X,other.Center.X))<chamber.RadiusX+other.RadiusX+spacing &&
                Math.Abs(chamber.Center.Y-other.Center.Y)<chamber.RadiusY+other.RadiusY+spacing) return false;
        var occupied=Reservations(world,others);
        return !chamber.Cells.Any(occupied.Contains);
    }
}
