using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AntWorld.Domain;
using AntWorld.Simulation;
using AntWorld.WorldGeneration;
namespace AntWorld.Benchmarks;

public static class NestBenchmarks
{
    public static void Run(string output)
    {
        var warm=ControlledWorld(9999);var warmLayout=NestFoundation.Create(warm,1,256,0);
        new OrganicNestPlanner().TryExpand(warm,warmLayout,0);new NestNavigation().Distances(warm,warmLayout.Chambers[0].Access);
        var scenarios=new List<object>();var planning=new List<double>();var connectivity=new List<double>();var signatures=new HashSet<string>();var peakResident=0L;
        for(var seed=10000;seed<10100;seed++)foreach(var species in new[]{0,2,3})
        {
            var world=ControlledWorld(seed);var layout=NestFoundation.Create(world,1,256,species);var before=world.TerrainRevision;
            var initialReachable=new NestNavigation().Distances(world,layout.Entrances.First(e=>e.Safe).Position);
            var initialValid=layout.Chambers.All(c=>initialReachable.ContainsKey(world.Index(c.Access)));
            var planner=new OrganicNestPlanner();var sampleTimes=new List<double>();
            while(layout.Chambers.Count<20)
            {
                var timer=Stopwatch.StartNew();var added=planner.TryExpand(world,layout,species);timer.Stop();sampleTimes.Add(timer.Elapsed.TotalMilliseconds);
                if(!added)break;
            }
            planning.AddRange(sampleTimes);var metrics=NestMetrics.Measure(world,layout);var plannedOnly=world.TerrainRevision==before;
            var cells=NestGeometry.Footprint(layout);
            // Test fixture materialization only: it does not publish completed construction or simulate workers.
            foreach(var p in cells)world.SetTerrain(p,TerrainKind.Tunnel);
            var watch=Stopwatch.StartNew();var reachable=new NestNavigation().Distances(world,layout.Chambers[0].Access);watch.Stop();connectivity.Add(watch.Elapsed.TotalMilliseconds);
            var accessible=layout.Chambers.Count(c=>reachable.ContainsKey(world.Index(c.Access)));
            var retention=BlockSecondary(world,layout);
            var signature=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{layout.Chambers,layout.Tunnels}))));signatures.Add(signature);
            var ratio=metrics["plannedAlternativeRouteRatio"];var eligible=layout.Chambers.Count>=10;
            var overlap=layout.Chambers.SelectMany(c=>c.Cells).GroupBy(p=>p).Count(g=>g.Count()>1);
            var failures=new List<string>();
            if(!initialValid)failures.Add("NEST-001/002");if(!plannedOnly)failures.Add("BUILD-001");
            if(accessible!=layout.Chambers.Count)failures.Add("NEST-010 planned fixture");if(overlap>0)failures.Add("NEST-006");
            if(eligible&&!NestMetrics.Organic(metrics))failures.Add("ORG");if(ratio<.2||ratio>.3)failures.Add("LOOP-001");if(retention is {} r&&r<.7)failures.Add("LOOP-005");
            peakResident=Math.Max(peakResident,Environment.WorkingSet);
            scenarios.Add(new{seed,species,configuration=world.Configuration,commit=Environment.GetEnvironmentVariable("ANTWORLD_BENCHMARK_COMMIT")??"working-tree",dirty=true,
                chambers=layout.Chambers.Count,plannedOnly,completedChambers=layout.Chambers.Count(c=>c.Status==NestStructureStatus.Completed),initialValid,
                reachableInCarvedFixture=accessible,initialEntrances=layout.Entrances.Count(e=>e.Safe),overlap,metrics,secondaryBlockRetention=retention,
                organicEligible=eligible,ineligibleReason=eligible?null:"Fewer than ten feasible chambers",planning=Stats(sampleTimes),
                status=!eligible?"NOT_RUN":failures.Count==0?"PASS":"FAIL",failures,signature,diagnostics=layout.Diagnostics});
        }
        var jobTimes=MeasureJobs();
        var report=new {formatVersion=2,generatedAt=DateTimeOffset.UtcNow,hardware=new{os=RuntimeInformation.OSDescription,architecture=RuntimeInformation.ProcessArchitecture.ToString(),runtime=RuntimeInformation.FrameworkDescription,processors=Environment.ProcessorCount,godot="4.6.3 mono",sdk="10.0.201"},
            scope="300 controlled structural fixtures; initial foundation completed; later rooms planned only. Carved fixture is separate from live worker execution. No three-day ecological run.",sampleCount=300,warmup="Foundation, incremental planning and navigation excluded before sampling",planning=Stats(planning),connectivity=Stats(connectivity),jobQuery=Stats(jobTimes),
            residentMemoryBytes=peakResident,memoryMeasurementStatus=peakResident<=0?"NOT_RUN":peakResident<=768L*1024*1024?"PASS":"FAIL",uniqueSignatures=signatures.Count,
            integratedWorkerConstruction="OrganicNestTests.ExpansionUsesExclusiveBoundedJobsAndActualDigging",unmeasured=new[]{"extended expansion/reload memory soak"},scenarios};
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);File.WriteAllText(output,JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Nest validation: {scenarios.Count} scenarios, {signatures.Count} signatures; report {output}");
    }
    private static double? BlockSecondary(WorldMap world,NestLayout layout)
    {
        var alternate=layout.Tunnels.FirstOrDefault(t=>t.IsAlternative);if(alternate is null)return null;
        var otherCells=layout.Tunnels.Where(t=>t!=alternate).SelectMany(t=>t.Cells).Concat(layout.Chambers.SelectMany(c=>c.Cells)).ToHashSet();
        var candidates=alternate.Cells.Where(p=>!otherCells.Contains(p)).ToArray();if(candidates.Length==0)return null;
        var block=candidates[candidates.Length/2];var old=world.GetCell(block,false);world.SetTerrain(block,TerrainKind.Rock);
        var reachable=new NestNavigation().Distances(world,layout.Chambers[0].Access);world.SetCell(block,old);
        return layout.Chambers.Count(c=>reachable.ContainsKey(world.Index(c.Access)))/(double)layout.Chambers.Count;
    }
    private static List<double> MeasureJobs()
    {
        var state=SimulationFactory.Create(new(42,128,64),[new(0,.5)]);var colony=state.Colonies[0];var layout=colony.Layout!;
        new OrganicNestPlanner().TryExpand(state.World,layout,0);OrganicNestConstruction.Enqueue(state.World,layout);new OrganicNestConstruction().Step(state,colony);
        var index=new AgentSpatialIndex();index.Rebuild(state);var worker=state.Ants.First(a=>a.Caste==AntCaste.Worker);var context=new AntContext(state,worker,colony,index,new());
        OrganicNestConstruction.FindJob(context,out _,out _);var results=new List<double>();
        for(var i=0;i<1000;i++){var timer=Stopwatch.StartNew();OrganicNestConstruction.FindJob(context,out _,out _);results.Add(timer.Elapsed.TotalMilliseconds);}return results;
    }
    private static object Stats(List<double> values){var sorted=values.Order().ToArray();return new{samples=sorted.Length,medianMs=sorted[sorted.Length/2],p95Ms=sorted[(int)(sorted.Length*.95)],maxMs=sorted[^1]};}
    private static WorldMap ControlledWorld(int seed)=>new(new(seed,512,128),new ControlledNestTerrain(),surface:Enumerable.Repeat(16,512).ToArray());
}
