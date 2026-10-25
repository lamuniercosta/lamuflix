using System;
using System.IO;

namespace LamuFlix.Evaluation.Entry;

/// <summary>
/// The dedicated evaluation entry (TB002 Mode A). `scripts/run-evaluation.ps1`
/// selects exactly this class with the runner's `-class` filter and transports the
/// fixture path, output path, evaluation seed and mode marker through child-scoped
/// environment variables. Ordinary suite execution never publishes.
/// </summary>
public sealed class EvaluationEntryTests
{
    [Fact]
    public void Selected_entry_publishes_the_metrics_summary_only_when_requested()
    {
        // arrange
        var requested = EvaluationEntry.IsRequested(Environment.GetEnvironmentVariable);

        // act
        var published = EvaluationEntry.Publish(Environment.GetEnvironmentVariable);

        // assert
        if (requested)
        {
            published.ShouldNotBeNull();
            File.Exists(published).ShouldBeTrue();
        }
        else
        {
            published.ShouldBeNull();
        }
    }
}
