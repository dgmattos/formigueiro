using System.Diagnostics;
using System.Text.Json;
using AntWorld.Domain;
using AntWorld.Simulation;
namespace AntWorld.Benchmarks;
public static class AgentBenchmarks
{
    public static void Run()
    {
        var results=new List<object>();
        foreach(var population in new[]{100,500,2000,10000})
        {
            GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
            var state=SimulationFactory.Create(new(42,240,96));
            state.Performance=new() { MaximumPopulation=population };
            while(state.Ants.Count<population)
            {
                var colony=state.Colonies[state.Ants.Count%2];
                state.Ants.Add(AntFactory.Create(state,colony,state.Ants.Count%8==0?AntCaste.Soldier:AntCaste.Worker));
            }
            foreach(var c in state.Colonies) c.Food=100000;
            var engine=new SimulationEngine(state);engine.Run(200);
            var allocated=GC.GetTotalAllocatedBytes(true);var times=new double[400];var decisions=0;
            for(var tick=0;tick<times.Length;tick++)
            {
                var start=Stopwatch.GetTimestamp();engine.Step();times[tick]=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                decisions+=engine.Scheduler.DecisionsLastTick;
            }
            var allocation=GC.GetTotalAllocatedBytes(true)-allocated;
            Array.Sort(times);
            results.Add(new { population,finalPopulation=state.Ants.Count,ticks=times.Length,simulatedSeconds=times.Length*SimulationState.TickSeconds,
                meanTickMs=times.Average(),p95TickMs=times[(int)(times.Length*.95)-1],maxTickMs=times[^1],decisions,
                allocatedBytes=allocation,managedLiveBytes=GC.GetTotalMemory(true),workingSetBytes=Process.GetCurrentProcess().WorkingSet64,
                maxDecisionsPerTick=state.Performance.MaxDetailedAgents/(20/state.Performance.IndividualDecisionsPerSecond),
                layout="dense nesting chambers, default neutral relations; L0/L1 logic, no renderer" });
        }
        Console.WriteLine(JsonSerializer.Serialize(new {runtime=Environment.Version.ToString(),processors=Environment.ProcessorCount,profile="LowMemory4GB",results},new JsonSerializerOptions{WriteIndented=true}));
    }
}
