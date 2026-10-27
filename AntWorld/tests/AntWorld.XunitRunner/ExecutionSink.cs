using Xunit.Abstractions;
using System.Diagnostics.CodeAnalysis;
namespace AntWorld.XunitRunner;

[SuppressMessage("xUnit", "xUnit3000", Justification = "Runner exclusivamente em processo no .NET 10: não usa AppDomains nem remoting.")]
public sealed class ExecutionSink : IMessageSink
{
    public TaskCompletionSource<ITestAssemblyFinished> Finished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int Errors { get; private set; }
    public bool OnMessage(IMessageSinkMessage message)
    {
        switch (message)
        {
            case ITestPassed passed: Console.WriteLine($"PASS {passed.Test.DisplayName}"); break;
            case ITestFailed failed:
                Console.Error.WriteLine($"FAIL {failed.Test.DisplayName}: {string.Join(Environment.NewLine, failed.Messages)}"); break;
            case IErrorMessage error:
                Errors++; Console.Error.WriteLine(string.Join(Environment.NewLine, error.Messages)); break;
            case ITestAssemblyFinished finished: Finished.TrySetResult(finished); break;
        }
        return true;
    }
}
