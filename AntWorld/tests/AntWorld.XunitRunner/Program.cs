using AntWorld.XunitRunner;
using Xunit.Sdk;

var assemblies = new[]
{
    typeof(AntWorld.Domain.Tests.DomainTests).Assembly,
    typeof(AntWorld.Simulation.Tests.SimulationTests).Assembly,
    typeof(AntWorld.Persistence.Tests.PersistenceTests).Assembly
};
var total = 0;
var failures = 0;
foreach (var assembly in assemblies)
{
    var sink = new ExecutionSink();
    using var framework = new XunitTestFramework(sink);
    using var executor = framework.GetExecutor(assembly.GetName());
    var options = new ExecutionOptions();
    options.SetValue("xunit.execution.DisableParallelization", true);
    executor.RunAll(sink, options, options);
    var result = await sink.Finished.Task.WaitAsync(TimeSpan.FromSeconds(60));
    total += result.TestsRun;
    failures += result.TestsFailed + result.TestsSkipped + sink.Errors;
}
Console.WriteLine($"xUnit real em processo: {total} testes, {failures} falhas/ignorados/erros.");
return total > 0 && failures == 0 ? 0 : 1;
