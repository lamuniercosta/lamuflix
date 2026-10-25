using System;
using System.IO;

namespace LamuFlix.Evaluation.Entry;

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
