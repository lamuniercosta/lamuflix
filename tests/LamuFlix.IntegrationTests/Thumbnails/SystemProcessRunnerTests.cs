using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.Thumbnails;
using UnitManualTimeProvider = LamuFlix.UnitTests.ManualTimeProvider;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests.Thumbnails;

public sealed class SystemProcessRunnerTests
{
    private static readonly TimeSpan StandardTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task RunAsync_OrdinaryProcess_CapturesStdoutStderrAndExitCode()
    {
        // act
        var result = await RunScriptAsync("echo.ps1", StandardTimeout, ["world"], cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.Success.ShouldBeTrue();
        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("world");
        result.StandardError.ShouldContain("err-world");
        result.FailureReason.ShouldBeNull();
    }

    [Fact]
    public async Task RunAsync_MissingExecutable_ReturnsLaunchFailedWithoutThrowing()
    {
        // arrange
        var runner = new SystemProcessRunner();
        var executable = "lamuflix-missing-" + Guid.NewGuid().ToString("N");

        // act
        var result = await runner.RunAsync(executable, [], TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // assert
        result.Success.ShouldBeFalse();
        result.ExitCode.ShouldBeNull();
        result.FailureReason.ShouldBe("LaunchFailed");
    }

    [Fact]
    public async Task RunAsync_NonZeroExit_ReturnsNonZeroExit()
    {
        // act
        var result = await RunScriptAsync("exit.ps1", StandardTimeout, ["7"], cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.Success.ShouldBeFalse();
        result.ExitCode.ShouldBe(7);
        result.FailureReason.ShouldBe("NonZeroExit");
    }

    [Fact]
    public async Task RunAsync_OutputStreamAtExactCap_Succeeds()
    {
        // act
        var result = await RunScriptAsync("emit-stdout.ps1", StandardTimeout, ["65536"], cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.Success.ShouldBeTrue();
        result.StandardOutput.Length.ShouldBe(65536);
        result.StandardError.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_OutputStreamOneByteOverCap_ReportsTruncationAfterDraining()
    {
        // act
        var result = await RunScriptAsync("emit-stdout.ps1", StandardTimeout, ["65537"], cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.Success.ShouldBeFalse();
        result.ExitCode.ShouldBe(0);
        result.FailureReason.ShouldBe("OutputTruncated");
        result.StandardOutput.Length.ShouldBe(65536);
    }

    [Fact]
    public async Task RunAsync_SimultaneousStreamsOverCap_DrainCompletelyWithoutDeadlock()
    {
        // act
        var result = await RunScriptAsync("emit-both.ps1", StandardTimeout, ["70000"], cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.Success.ShouldBeFalse();
        result.FailureReason.ShouldBe("OutputTruncated");
        result.StandardOutput.Length.ShouldBe(65536);
        result.StandardError.Length.ShouldBe(65536);
    }

    [Fact]
    public async Task RunAsync_Timeout_KillsProcessTree()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var marker = Path.Combine(temporary.Path, "started.txt");
        var childMarker = Path.Combine(temporary.Path, "child.txt");
        var childScript = ScriptPath("write-after-delay.ps1");

        // act
        var result = await RunScriptAsync(
            "timeout-tree.ps1",
            TimeSpan.FromSeconds(3),
            [marker, childScript, childMarker, "6", "30"],
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        File.Exists(marker).ShouldBeTrue();
        result.Success.ShouldBeFalse();
        result.ExitCode.ShouldBeNull();
        result.FailureReason.ShouldBe("TimedOut");
        await Task.Delay(TimeSpan.FromSeconds(7), TestContext.Current.CancellationToken);
        File.Exists(childMarker).ShouldBeFalse();
    }

    [Fact]
    public async Task RunAsync_CallerCancellation_PropagatesAfterKillingProcess()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var marker = Path.Combine(temporary.Path, "started.txt");
        using var cancellation = new CancellationTokenSource();
        var run = RunScriptAsync(
            "sleep-with-marker.ps1",
            StandardTimeout,
            [marker, "30"],
            cancellationToken: cancellation.Token);
        await WaitForFileAsync(marker, TimeSpan.FromSeconds(10));

        // act
        cancellation.Cancel();

        // assert
        await Should.ThrowAsync<OperationCanceledException>(async () => await run);
    }

    [Fact]
    public async Task RunAsync_ControlledTimeProviderTimeout_ReturnsTimedOut()
    {
        // arrange
        using var temporary = new TemporaryDirectory();
        var marker = Path.Combine(temporary.Path, "started.txt");
        var timeProvider = new UnitManualTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var run = RunScriptAsync(
            "sleep-with-marker.ps1",
            TimeSpan.FromSeconds(5),
            [marker, "30"],
            timeProvider,
            cancellationToken: TestContext.Current.CancellationToken);
        await WaitForFileAsync(marker, TimeSpan.FromSeconds(10));
        using var waitForTimer = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await timeProvider.WaitForPendingTimerAsync(waitForTimer.Token);

        // act
        timeProvider.Advance(TimeSpan.FromSeconds(5));
        var result = await run.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        result.Success.ShouldBeFalse();
        result.ExitCode.ShouldBeNull();
        result.FailureReason.ShouldBe("TimedOut");
    }

    private static Task<ProcessResult> RunScriptAsync(
        string script,
        TimeSpan timeout,
        IReadOnlyList<string> arguments,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        var runner = new SystemProcessRunner(timeProvider);
        var processArguments = new List<string>
        {
            "-NoProfile",
            "-NonInteractive",
            "-File",
            ScriptPath(script),
        };
        processArguments.AddRange(arguments);
        return runner.RunAsync("pwsh", processArguments, timeout, cancellationToken);
    }

    private static string ScriptPath(string script)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Thumbnails", script);
        return File.Exists(path)
            ? path
            : throw new FileNotFoundException("Missing pwsh fixture script.", path);
    }

    private static async Task WaitForFileAsync(string path, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        while (!File.Exists(path))
        {
            cancellation.Token.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellation.Token);
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "lamuflix-process-runner-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Test cleanup is best effort.
            }
        }
    }
}
