using Xunit.Abstractions;
namespace AntWorld.XunitRunner;

public sealed class ExecutionOptions : ITestFrameworkDiscoveryOptions, ITestFrameworkExecutionOptions
{
    private readonly Dictionary<string, object?> values = [];
    public TValue GetValue<TValue>(string name) => values.TryGetValue(name, out var value) ? (TValue)value! : default!;
    public void SetValue<TValue>(string name, TValue value) => values[name] = value;
}
