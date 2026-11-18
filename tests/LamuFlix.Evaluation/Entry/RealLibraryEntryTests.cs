using System;
using System.Threading.Tasks;

namespace LamuFlix.Evaluation.Entry;

/// <summary>
/// Executable dispatch for the real-library modes. `dotnet run` on the evaluation project runs this
/// assembly's tests, so this selected entry performs the leave-one-out or compare run only when
/// <see cref="EvaluationEntry.ModeVariable"/> names one of those modes; an ordinary `dotnet test`
/// without the marker leaves it inert. A failed load or scoring run fails the test, so the child
/// process exits nonzero instead of publishing a partial result.
/// </summary>
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
