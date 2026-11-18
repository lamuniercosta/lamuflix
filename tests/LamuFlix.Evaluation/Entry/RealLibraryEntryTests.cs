using System;
using System.Threading.Tasks;

namespace LamuFlix.Evaluation.Entry;

public sealed class RealLibraryEntryTests
{
    [Fact]
    public async Task Selected_entry_runs_a_real_library_mode_only_when_requested()
    {
        // arrange
        Func<string, string?> readVariable = Environment.GetEnvironmentVariable;
        var requested = EvaluationEntry.IsRealLibraryRequested(readVariable);

        // act
        var output = await EvaluationEntry.RunRealLibraryAsync(
            readVariable,
            TestContext.Current.CancellationToken);

        // assert
        if (requested)
        {
            output.ShouldNotBeNull();
        }
        else
        {
            output.ShouldBeNull();
        }
    }
}
