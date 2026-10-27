using AntWorld.Domain;
namespace AntWorld.Benchmarks;

internal static class NestMetrics
{
    public static Dictionary<string,double> Measure(WorldMap world,NestLayout layout)
    {
        var degree=layout.Chambers.ToDictionary(c=>c.Id,c=>layout.Tunnels.Count(t=>t.FromChamberId==c.Id||t.ToChamberId==c.Id));
        var eligible=degree.Values.Count(v=>v>=2);
        var lengths=layout.Tunnels.Sum(t=>Math.Max(0,t.Path.Count-1));
        var vertical=layout.Tunnels.Where(t=>t.Path.Select(p=>p.X).Distinct().Count()==1).Sum(t=>Math.Max(0,t.Path.Count-1));
        return new()
        {
            ["offQueenAxis"]=layout.Chambers.Count(c=>c.Access.X!=layout.Chambers[0].Access.X)/(double)layout.Chambers.Count,
            ["junctionRatio"]=eligible==0?0:degree.Values.Count(v=>v>=3)/(double)eligible,
            ["shapeCount"]=layout.Chambers.Select(c=>(c.RadiusX,c.RadiusY)).Distinct().Count(),
            ["maximumColumnShare"]=layout.Chambers.GroupBy(c=>c.Access.X).Max(g=>g.Count())/(double)layout.Chambers.Count,
            ["exclusivelyVerticalLengthShare"]=lengths==0?0:vertical/(double)lengths,
            // Independent base-tree branches directly leaving the queen's chamber with lateral displacement.
            ["independentLateralBranches"]=layout.Tunnels.Count(t=>!t.IsAlternative&&t.FromChamberId==layout.QueenChamberId&&
                world.HorizontalDelta(layout.Chambers.Single(c=>c.Id==t.FromChamberId).Access.X,layout.Chambers.Single(c=>c.Id==t.ToChamberId).Access.X)!=0),
            ["plannedAlternativeRouteRatio"]=layout.AlternativeRouteRatio(),["completedAlternativeRouteRatio"]=layout.AlternativeRouteRatio(true)
        };
    }
    public static bool Organic(Dictionary<string,double> m)=>m["offQueenAxis"]>=.7&&m["junctionRatio"]>=.15&&m["shapeCount"]>=3&&
        m["maximumColumnShare"]<=.3&&m["exclusivelyVerticalLengthShare"]<=.4&&m["independentLateralBranches"]>=3;
}
