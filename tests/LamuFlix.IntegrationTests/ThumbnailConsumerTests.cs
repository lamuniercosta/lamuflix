using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Infrastructure.Thumbnails;
using LamuFlix.Tests.Common;
using LamuFlix.UnitTests.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using RabbitMQ.Client;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(RabbitMqCollection))]
public sealed class ThumbnailConsumerTests(RabbitMqFixture fixture) : IAsyncLifetime
{
    private static readonly byte[] ValidJpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02, 0x03];
    private static readonly byte[] ReplacementJpeg = [0xFF, 0xD8, 0x11, 0x22, 0x33];
    private static readonly TimeSpan PollDeadline = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);

    private readonly RabbitMqProbe probe = new(fixture);

    public async ValueTask InitializeAsync()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetTopologyAsync(ct);
        await RabbitMqThumbnailQueues.PurgeThumbnailQueuesAsync(fixture, ct);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Consumer_AnEligibleRequest_GeneratesPromotesAndAcknowledgesOneCanonicalJpeg()
    {
        var ct = TestContext.Current.CancellationToken;
        using var temporary = new TemporaryDirectory();
        var generator = SuccessfulGenerator(temporary, ReplacementJpeg);
        await using var host = NewHost(temporary, generator);
        var movie = host.AddMovie(501, temporary.LibraryFile("movie.mp4"));

        await host.StartAsync(ct);
        await host.Publisher.PublishAsync(new ThumbnailRequested(movie, host.SourcePath(movie)), ct);
        await WaitUntilAsync(_ => Task.FromResult(File.Exists(temporary.CanonicalPath(movie))), ct);
        await host.StopAsync(CancellationToken.None);

        (await File.ReadAllBytesAsync(temporary.CanonicalPath(movie), ct)).ShouldBe(ReplacementJpeg);
        generator.CallCount.ShouldBe(1);
        generator.Sources.ShouldHaveSingleItem().ShouldBe(host.SourcePath(movie));
        (await probe.DeclarePassiveAsync(RabbitMqTopology.ThumbnailDeadLetterQueue, ct)).MessageCount.ShouldBe(0U);
        AssertNoSensitiveValues(host.Log, temporary);
    }

    [Fact]
    public async Task Consumer_AValidSoiCacheHit_SkipsGenerationAndAcknowledges()
    {
        var ct = TestContext.Current.CancellationToken;
        using var temporary = new TemporaryDirectory();
        var generator = new ScriptedThumbnailGenerator((_, _) =>
            Task.FromResult(Failed("BinaryNotFound")));
        await using var host = NewHost(temporary, generator);
        var movie = host.AddMovie(502, temporary.LibraryFile("movie.mp4"));
        await File.WriteAllBytesAsync(temporary.CanonicalPath(movie), ValidJpeg, ct);

        await host.StartAsync(ct);
        await host.Publisher.PublishAsync(new ThumbnailRequested(movie, host.SourcePath(movie)), ct);
        await WaitUntilAsync(async token =>
            (await probe.DeclarePassiveAsync(RabbitMqTopology.ThumbnailQueue, token)).MessageCount == 0U, ct);
        await Task.Delay(PollInterval, ct);
        await host.StopAsync(CancellationToken.None);

        generator.CallCount.ShouldBe(0);
        (await File.ReadAllBytesAsync(temporary.CanonicalPath(movie), ct)).ShouldBe(ValidJpeg);
        (await probe.DeclarePassiveAsync(RabbitMqTopology.ThumbnailDeadLetterQueue, ct)).MessageCount.ShouldBe(0U);
    }

    [Fact]
    public async Task Consumer_AMalformedBodyWithACorruptTraceParent_DeadLettersWithoutThrowing()
    {
        var ct = TestContext.Current.CancellationToken;
        using var temporary = new TemporaryDirectory();
        var generator = SuccessfulGenerator(temporary, ValidJpeg);
        await using var host = NewHost(temporary, generator);

        await host.StartAsync(ct);
        await PublishRawAsync(
            Encoding.UTF8.GetBytes("this is not a thumbnail request"),
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [TraceContextCarrier.TraceParentHeader] = Encoding.UTF8.GetBytes("00-not-a-valid-traceparent"),
            },
            ct);
        var dead = await probe.PollGetAsync(RabbitMqTopology.ThumbnailDeadLetterQueue, ct);
        await host.StopAsync(CancellationToken.None);

        dead.ShouldNotBeNull();
        generator.CallCount.ShouldBe(0);
        var failure = host.Log.Entries.Single(entry => HasState(entry, "FailureReason"));
        State(failure, "FailureReason").ShouldBe("MalformedMessage");
        HasState(failure, "MovieId").ShouldBeFalse();
        State(failure, "Redelivered").ShouldNotBeNull();
        AssertNoSensitiveValues(host.Log, temporary, "this is not a thumbnail request");
    }

    [Fact]
    public async Task Consumer_ACorruptTraceParentWithAValidBody_StillGeneratesAndAcknowledges()
    {
        var ct = TestContext.Current.CancellationToken;
        using var temporary = new TemporaryDirectory();
        var generator = SuccessfulGenerator(temporary, ValidJpeg);
        await using var host = NewHost(temporary, generator);
        var movie = host.AddMovie(503, temporary.LibraryFile("movie.mp4"));

        await host.StartAsync(ct);
        await PublishRawAsync(
            JsonSerializer.SerializeToUtf8Bytes(new ThumbnailRequested(movie, host.SourcePath(movie))),
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [TraceContextCarrier.TraceParentHeader] = Encoding.UTF8.GetBytes("corrupt"),
            },
            ct);
        await WaitUntilAsync(_ => Task.FromResult(File.Exists(temporary.CanonicalPath(movie))), ct);
        await host.StopAsync(CancellationToken.None);

        generator.CallCount.ShouldBe(1);
        (await probe.DeclarePassiveAsync(RabbitMqTopology.ThumbnailDeadLetterQueue, ct)).MessageCount.ShouldBe(0U);
    }

    [Fact]
    public async Task Consumer_AStaleQueuedPath_DeadLettersAsIneligibleWithoutGenerating()
    {
        var ct = TestContext.Current.CancellationToken;
        using var temporary = new TemporaryDirectory();
        var generator = SuccessfulGenerator(temporary, ValidJpeg);
        await using var host = NewHost(temporary, generator);
        var movie = host.AddMovie(504, temporary.LibraryFile("movie.mp4"));
        var stale = temporary.LibraryFile("stale.mp4");

        await host.StartAsync(ct);
        await host.Publisher.PublishAsync(new ThumbnailRequested(movie, stale), ct);
        var dead = await probe.PollGetAsync(RabbitMqTopology.ThumbnailDeadLetterQueue, ct);
        await host.StopAsync(CancellationToken.None);

        dead.ShouldNotBeNull();
        generator.CallCount.ShouldBe(0);
        AssertTerminalFailure(host.Log, movie.Value, "IneligibleRequest");
    }

    [Fact]
    public async Task Consumer_ARequestForAMovieThatNoLongerExists_DeadLettersAsIneligible()
    {
        var ct = TestContext.Current.CancellationToken;
        using var temporary = new TemporaryDirectory();
        var generator = SuccessfulGenerator(temporary, ValidJpeg);
        await using var host = NewHost(temporary, generator);
        var movie = new MovieId(505);
        var path = temporary.LibraryFile("missing.mp4");

        await host.StartAsync(ct);
        await host.Publisher.PublishAsync(new ThumbnailRequested(movie, path), ct);
        var dead = await probe.PollGetAsync(RabbitMqTopology.ThumbnailDeadLetterQueue, ct);
        await host.StopAsync(CancellationToken.None);

        dead.ShouldNotBeNull();
        generator.CallCount.ShouldBe(0);
        AssertTerminalFailure(host.Log, movie.Value, "IneligibleRequest");
    }

    [Fact]
    public async Task Consumer_AMovieThatNowHasAPoster_DeadLettersAsIneligible()
    {
        var ct = TestContext.Current.CancellationToken;
        using var temporary = new TemporaryDirectory();
        var generator = SuccessfulGenerator(temporary, ValidJpeg);
        await using var host = NewHost(temporary, generator);
        var source = temporary.LibraryFile("movie.mp4");
        var movie = host.AddMovie(506, source);
        host.GivePoster(movie);

        await host.StartAsync(ct);
        await host.Publisher.PublishAsync(new ThumbnailRequested(movie, source), ct);
        var dead = await probe.PollGetAsync(RabbitMqTopology.ThumbnailDeadLetterQueue, ct);
        await host.StopAsync(CancellationToken.None);

        dead.ShouldNotBeNull();
        generator.CallCount.ShouldBe(0);
        AssertTerminalFailure(host.Log, movie.Value, "IneligibleRequest");
    }

    [Theory]
    [InlineData("outside")]
    [InlineData("sibling-prefix")]
    public async Task Consumer_ASourceThatEscapesTheLibraryRoot_DeadLettersAsUnauthorized(string shape)
    {
        var ct = TestContext.Current.CancellationToken;
        using var temporary = new TemporaryDirectory();
        var generator = SuccessfulGenerator(temporary, ValidJpeg);
        await using var host = NewHost(temporary, generator);
        var source = shape switch
        {
            "outside" => temporary.OutsideFile("movie.mp4"),
            _ => temporary.SiblingPrefixFile("movie.mp4"),
        };
        var movie = host.AddMovie(507, source);

        await host.StartAsync(ct);
        await host.Publisher.PublishAsync(new ThumbnailRequested(movie, source), ct);
        var dead = await probe.PollGetAsync(RabbitMqTopology.ThumbnailDeadLetterQueue, ct);
        await host.StopAsync(CancellationToken.None);

        dead.ShouldNotBeNull();
        generator.CallCount.ShouldBe(0);
        AssertTerminalFailure(host.Log, movie.Value, "UnauthorizedSourcePath");
    }

    [Fact]
    public async Task Consumer_ASourceFileThatIsASymlink_DeadLettersAsUnauthorized()
    {
        using var temporary = new TemporaryDirectory();
        Assert.SkipUnless(
            TryCreateSymbolicLink(Path.Combine(temporary.LibraryRoot, "linked.mp4"), temporary.OutsideFile("real.mp4")),
            "Creating symbolic links requires Windows Developer Mode or elevated privilege.");
        var ct = TestContext.Current.CancellationToken;
        var generator = SuccessfulGenerator(temporary, ValidJpeg);
        await using var host = NewHost(temporary, generator);
        var source = Path.Combine(temporary.LibraryRoot, "linked.mp4");
        var movie = host.AddMovie(508, source);

        await host.StartAsync(ct);
        await host.Publisher.PublishAsync(new ThumbnailRequested(movie, source), ct);
        var dead = await probe.PollGetAsync(RabbitMqTopology.ThumbnailDeadLetterQueue, ct);
        await host.StopAsync(CancellationToken.None);

        dead.ShouldNotBeNull();
        generator.CallCount.ShouldBe(0);
        AssertTerminalFailure(host.Log, movie.Value, "UnauthorizedSourcePath");
    }

    [Fact]
    public async Task Consumer_AnAncestorDirectoryThatIsASymlink_DeadLettersAsUnauthorized()
    {
        using var temporary = new TemporaryDirectory();
        var link = Path.Combine(temporary.LibraryRoot, "linked-directory");
        Assert.SkipUnless(
            TryCreateDirectorySymbolicLink(link, temporary.OutsideDirectory),
            "Creating symbolic links requires Windows Developer Mode or elevated privilege.");
        var ct = TestContext.Current.CancellationToken;
        var generator = SuccessfulGenerator(temporary, ValidJpeg);
        await using var host = NewHost(temporary, generator);
        var source = Path.Combine(link, "movie.mp4");
        var movie = host.AddMovie(509, source);

        await host.StartAsync(ct);
        await host.Publisher.PublishAsync(new ThumbnailRequested(movie, source), ct);
        var dead = await probe.PollGetAsync(RabbitMqTopology.ThumbnailDeadLetterQueue, ct);
        await host.StopAsync(CancellationToken.None);

        dead.ShouldNotBeNull();
        generator.CallCount.ShouldBe(0);
        AssertTerminalFailure(host.Log, movie.Value, "UnauthorizedSourcePath");
    }

    [Theory]
    [InlineData("Disabled")]
    [InlineData("InvalidSourcePath")]
    [InlineData("BinaryNotFound")]
    [InlineData("ProcessTimedOut")]
    [InlineData("ProbeFailed")]
    [InlineData("NoVideoStream")]
    [InlineData("InvalidDuration")]
    [InlineData("ExtractionFailed")]
    [InlineData("InvalidOutput")]
    [InlineData("StorageError")]
    public async Task Consumer_EachClosedGeneratorFailureToken_DeadLettersWithThatExactToken(string token)
    {
        var ct = TestContext.Current.CancellationToken;
        using var temporary = new TemporaryDirectory();
        var generator = new ScriptedThumbnailGenerator((_, _) => Task.FromResult(Failed(token)));
        await using var host = NewHost(temporary, generator);
        var movie = host.AddMovie(510, temporary.LibraryFile("movie.mp4"));

        await host.StartAsync(ct);
        await host.Publisher.PublishAsync(new ThumbnailRequested(movie, host.SourcePath(movie)), ct);
        var dead = await probe.PollGetAsync(RabbitMqTopology.ThumbnailDeadLetterQueue, ct);
        await host.StopAsync(CancellationToken.None);

        dead.ShouldNotBeNull();
        AssertTerminalFailure(host.Log, movie.Value, token);
        (await probe.DeclarePassiveAsync(RabbitMqTopology.ThumbnailQueue, ct)).MessageCount.ShouldBe(0U);
        AssertNoSensitiveValues(host.Log, temporary);
    }

    [Fact]
    public async Task Consumer_APromotionFailure_PreservesThePriorJpegAndDeadLetters()
    {
        var ct = TestContext.Current.CancellationToken;
        using var temporary = new TemporaryDirectory();
        var generator = PromotionFailureGenerator(temporary, new MovieId(511));
        await using var host = NewHost(temporary, generator);
        var movie = host.AddMovie(511, temporary.LibraryFile("movie.mp4"));

        await host.StartAsync(ct);
        await host.Publisher.PublishAsync(new ThumbnailRequested(movie, host.SourcePath(movie)), ct);
        var dead = await probe.PollGetAsync(RabbitMqTopology.ThumbnailDeadLetterQueue, ct);
        await host.StopAsync(CancellationToken.None);

        dead.ShouldNotBeNull();
        (await File.ReadAllBytesAsync(temporary.CanonicalPath(movie), ct)).ShouldBe(ValidJpeg);
        AssertTerminalFailure(host.Log, movie.Value, "CachePromotionFailed");
        generator.OutputPaths.ShouldHaveSingleItem();
        File.Exists(generator.OutputPaths[0]).ShouldBeFalse();
    }

    [Fact]
    public async Task Consumer_AnUnexpectedGeneratorException_DeadLettersWithErrorActivityAndClosedLogFields()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var temporary = new TemporaryDirectory();
        var generator = new ScriptedThumbnailGenerator((sourcePath, _) =>
            Task.FromException<ThumbnailResult>(
                new InvalidOperationException("generator blew up at " + sourcePath)));
        await using var host = NewHost(temporary, generator);
        var movie = host.AddMovie(514, temporary.LibraryFile("movie.mp4"));
        var source = host.SourcePath(movie);
        var activities = new ConcurrentQueue<Activity>();
        using var listener = ListenForActivities(activities);

        // act
        await host.StartAsync(ct);
        await host.Publisher.PublishAsync(new ThumbnailRequested(movie, source), ct);
        var dead = await probe.PollGetAsync(RabbitMqTopology.ThumbnailDeadLetterQueue, ct);
        await host.StopAsync(CancellationToken.None);

        // assert
        dead.ShouldNotBeNull();
        generator.CallCount.ShouldBe(1);
        var failure = host.Log.Entries.Single(entry =>
            entry.Level == LogLevel.Error && HasState(entry, "ErrorType"));
        State(failure, "MovieId").ShouldBe(movie.Value);
        State(failure, "ErrorType").ShouldBe(nameof(InvalidOperationException));
        State(failure, "Redelivered").ShouldNotBeNull();
        failure.State
            .Select(pair => pair.Key)
            .Where(key => !string.Equals(key, "{OriginalFormat}", StringComparison.Ordinal))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ShouldBe(["ErrorType", "MovieId", "Redelivered"]);
        var process = activities.Single(activity => activity.DisplayName == TelemetryConstants.ThumbnailProcess);
        process.Status.ShouldBe(ActivityStatusCode.Error);
        process.GetTagItem(TelemetryConstants.MovieId).ShouldBe(movie.Value);
        process.GetTagItem(TelemetryConstants.ErrorType).ShouldBe(nameof(InvalidOperationException));
        var errorEvent = process.Events.Single(evt => evt.Name == "error");
        errorEvent.Tags.Single(tag => tag.Key == TelemetryConstants.MovieId).Value.ShouldBe(movie.Value);
        errorEvent.Tags.Single(tag => tag.Key == TelemetryConstants.ErrorType).Value
            .ShouldBe(nameof(InvalidOperationException));
        AssertNoSensitiveValues(host.Log, temporary, source);
        process.Tags
            .Select(tag => tag.Value ?? string.Empty)
            .Concat(errorEvent.Tags.Select(tag => tag.Value?.ToString() ?? string.Empty))
            .Any(value => value.Contains(temporary.Root, StringComparison.OrdinalIgnoreCase))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task Consumer_ShutdownAfterTheGeneratorReturnsRequeuesAndCleansOnlyItsOwnTemporaryOutput()
    {
        var ct = TestContext.Current.CancellationToken;
        using var temporary = new TemporaryDirectory();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var generator = BlockingGenerator(temporary, entered, release);
        await using var host = NewHost(temporary, generator);
        var movie = host.AddMovie(512, temporary.LibraryFile("movie.mp4"));

        await host.StartAsync(ct);
        await host.Publisher.PublishAsync(new ThumbnailRequested(movie, host.SourcePath(movie)), ct);
        await entered.Task.WaitAsync(PollDeadline, ct);
        var stopping = host.StopAsync(CancellationToken.None);
        release.TrySetResult();
        await stopping;

        generator.OutputPaths.ShouldHaveSingleItem();
        File.Exists(generator.OutputPaths[0]).ShouldBeFalse();
        (await probe.DeclarePassiveAsync(RabbitMqTopology.ThumbnailQueue, ct)).MessageCount.ShouldBe(1U);
        (await probe.DeclarePassiveAsync(RabbitMqTopology.ThumbnailDeadLetterQueue, ct)).MessageCount.ShouldBe(0U);
        host.Log.Entries.Any(entry => HasState(entry, "FailureReason")).ShouldBeFalse();
    }

    [Fact]
    public async Task Consumer_ARedelivery_StartsTheActivityWithALinkAndClosedTags()
    {
        var ct = TestContext.Current.CancellationToken;
        using var temporary = new TemporaryDirectory();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var generator = BlockingGenerator(temporary, entered, release);
        const int movieId = 513;
        string source;
        await using (var first = NewHost(temporary, generator))
        {
            var movie = first.AddMovie(movieId, temporary.LibraryFile("movie.mp4"));
            source = first.SourcePath(movie);
            await first.StartAsync(ct);
            await first.Publisher.PublishAsync(new ThumbnailRequested(movie, source), ct);
            await entered.Task.WaitAsync(PollDeadline, ct);
            var stopping = first.StopAsync(CancellationToken.None);
            release.TrySetResult();
            await stopping;
        }

        var activities = new ConcurrentQueue<Activity>();
        using var listener = ListenForActivities(activities);
        await using var host = NewHost(temporary, generator);
        var redeliveredMovie = host.AddMovie(movieId, source);
        await host.StartAsync(ct);
        await WaitUntilAsync(_ => Task.FromResult(File.Exists(temporary.CanonicalPath(redeliveredMovie))), ct);
        await host.StopAsync(CancellationToken.None);

        var process = activities.Single(activity => activity.DisplayName == TelemetryConstants.ThumbnailProcess);
        process.GetTagItem(TelemetryConstants.MovieId).ShouldBe(redeliveredMovie.Value);
        process.GetTagItem(TelemetryConstants.MessagingDeliveryCount).ShouldBe(1);
        process.Links.ShouldHaveSingleItem();
    }

    private static bool HasState(RecordingLogger<ThumbnailConsumer>.LogEntry entry, string name) =>
        entry.State.Any(pair => pair.Key == name);

    private static object? State(RecordingLogger<ThumbnailConsumer>.LogEntry entry, string name) =>
        entry.State.Single(pair => pair.Key == name).Value;

    private static void AssertTerminalFailure(
        RecordingLogger<ThumbnailConsumer> log,
        int movieId,
        string failureReason)
    {
        var failure = log.Entries.Single(entry => HasState(entry, "FailureReason"));
        State(failure, "FailureReason").ShouldBe(failureReason);
        State(failure, "MovieId").ShouldBe(movieId);
        HasState(failure, "Redelivered").ShouldBeTrue();
        HasState(failure, "ErrorType").ShouldBeFalse();
    }

    private static void AssertNoSensitiveValues(
        RecordingLogger<ThumbnailConsumer> log,
        TemporaryDirectory temporary,
        string? forbidden = null)
    {
        var values = log.Entries
            .SelectMany(entry => entry.State)
            .Select(pair => pair.Value?.ToString() ?? string.Empty)
            .ToArray();
        values.Any(value => value.Contains(temporary.Root, StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
        if (!string.IsNullOrEmpty(forbidden))
        {
            values.Any(value => value.Contains(forbidden, StringComparison.Ordinal)).ShouldBeFalse();
        }
    }

    private static ThumbnailResult Successful(string path) => new(true, path, null);

    private static ThumbnailResult Failed(string reason) => new(false, null, reason);

    private static bool TryCreateSymbolicLink(string linkPath, string target)
    {
        try
        {
            File.CreateSymbolicLink(linkPath, target);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return false;
        }
    }

    private static bool TryCreateDirectorySymbolicLink(string linkPath, string target)
    {
        try
        {
            Directory.CreateSymbolicLink(linkPath, target);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return false;
        }
    }

    private static ActivityListener ListenForActivities(ConcurrentQueue<Activity> activities)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TelemetryConstants.ActivitySourceName,
            Sample = (ref options) =>
            {
                _ = options;
                return ActivitySamplingResult.AllDataAndRecorded;
            },
            SampleUsingParentId = (ref options) =>
            {
                _ = options;
                return ActivitySamplingResult.AllDataAndRecorded;
            },
            ActivityStopped = activity => activities.Enqueue(activity),
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private async Task PublishRawAsync(
        byte[] body,
        IDictionary<string, object?> headers,
        CancellationToken ct)
    {
        await using var connection = await fixture.CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await channel.BasicPublishAsync(
            RabbitMqTopology.ExchangeName,
            RabbitMqTopology.ThumbnailRoutingKey,
            mandatory: true,
            basicProperties: new BasicProperties { Persistent = true, Headers = headers },
            body: body,
            cancellationToken: ct);
    }

    private static async Task WaitUntilAsync(Func<CancellationToken, Task<bool>> condition, CancellationToken ct)
    {
        var deadline = TimeProvider.System.GetUtcNow() + PollDeadline;
        while (TimeProvider.System.GetUtcNow() < deadline)
        {
            if (await condition(ct))
            {
                return;
            }

            await Task.Delay(PollInterval, ct);
        }

        (await condition(ct)).ShouldBeTrue();
    }

    private static ScriptedThumbnailGenerator SuccessfulGenerator(TemporaryDirectory temporary, byte[] jpeg) =>
        new((_, _) => Task.FromResult(Successful(temporary.WriteGeneratorOutput(jpeg))));

    private static ScriptedThumbnailGenerator PromotionFailureGenerator(TemporaryDirectory temporary, MovieId movie) =>
        new((_, _) =>
        {
            File.WriteAllBytes(temporary.CanonicalPath(movie), ValidJpeg);
            return Task.FromResult(Successful(temporary.WriteGeneratorOutput([0x00, 0x01, 0x02])));
        });

    private static ScriptedThumbnailGenerator BlockingGenerator(
        TemporaryDirectory temporary,
        TaskCompletionSource entered,
        TaskCompletionSource release) =>
        new(async (_, _) =>
        {
            var output = temporary.WriteGeneratorOutput(ValidJpeg);
            entered.TrySetResult();
            await release.Task;
            return Successful(output);
        });

    private ThumbnailHost NewHost(TemporaryDirectory temporary, ScriptedThumbnailGenerator generator)
    {
        var options = fixture.Options;
        var owner = new RabbitMqConnectionOwner(Options.Create(options));
        var topology = new RabbitMqTopology(
            owner,
            Options.Create(options),
            Options.Create(new EnrichmentOptions { MaxAttempts = 3 }));
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(temporary.Root);
        var thumbnailOptions = Options.Create(new ThumbnailOptions
        {
            Enabled = true,
            CacheDirectory = temporary.CacheRoot,
        });
        var cache = new ThumbnailCache(thumbnailOptions, environment);
        var repository = new InMemoryMovieRepository();
        var logger = new RecordingLogger<ThumbnailConsumer>();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMovieRepository>(repository);
        var built = services.BuildServiceProvider();
        var consumer = new ThumbnailConsumer(
            owner,
            topology,
            built.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            Options.Create(new LibraryOptions { RootPath = temporary.LibraryRoot }),
            generator,
            cache,
            environment,
            logger);
        return new ThumbnailHost(owner, topology, repository, consumer, built, logger);
    }

    private sealed class ThumbnailHost(
        RabbitMqConnectionOwner owner,
        RabbitMqTopology topology,
        InMemoryMovieRepository movies,
        ThumbnailConsumer consumer,
        ServiceProvider services,
        RecordingLogger<ThumbnailConsumer> logger) : IAsyncDisposable
    {
        public RabbitMqThumbnailQueuePublisher Publisher { get; } = new(topology, owner);

        public RecordingLogger<ThumbnailConsumer> Log { get; } = logger;

        public Task StartAsync(CancellationToken cancellationToken) => consumer.StartAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken) => consumer.StopAsync(cancellationToken);

        public MovieId AddMovie(int id, string path)
        {
            var movie = Movie.Create(new MovieId(id), "Movie " + id, new LibraryPath(path), new MediaFormat("mp4"));
            movies.Add(movie);
            return movie.Id;
        }

        public void GivePoster(MovieId id)
        {
            movies.Find(id).ShouldNotBeNull().MarkEnriched(
                new MovieMetadata("Movie", posterUrl: "https://example.invalid/poster.jpg"),
                DateTimeOffset.UnixEpoch);
        }

        public string SourcePath(MovieId id) => movies.Find(id).ShouldNotBeNull().Path.Value;

        public ValueTask DisposeAsync()
        {
            services.Dispose();
            return owner.DisposeAsync();
        }
    }

    private sealed class InMemoryMovieRepository : IMovieRepository
    {
        private readonly Dictionary<MovieId, Movie> movies = [];

        public void Add(Movie movie) => movies[movie.Id] = movie;

        public Movie? Find(MovieId id) => movies.TryGetValue(id, out var movie) ? movie : null;

        public Task<Movie?> GetAsync(MovieId id, CancellationToken ct) => Task.FromResult(Find(id));

        public Task AddAsync(Movie movie, CancellationToken ct) => Task.CompletedTask;

        public Task<bool> TryDeleteAsync(MovieId id, CancellationToken ct) => Task.FromResult(false);

        public Task<MovieId> NextIdentityAsync(CancellationToken ct) => Task.FromResult(new MovieId(movies.Count + 1));

        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<bool> TryClaimForEnrichmentAsync(MovieId id, CancellationToken ct) => Task.FromResult(false);

        public Task<IReadOnlyList<MovieId>> FindStrandedMovieIdsAsync(DateTimeOffset leaseCutoff, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<MovieId>>([]);

        public Task<string?> GetPlotAsync(MovieId id, CancellationToken ct) => Task.FromResult<string?>(null);

        public Task<MovieId?> FindMovieIdByImdbIdAsync(ImdbId imdbId, CancellationToken ct) =>
            Task.FromResult<MovieId?>(null);
    }

    private sealed class ScriptedThumbnailGenerator(
        Func<string, CancellationToken, Task<ThumbnailResult>> handler) : IThumbnailGenerator
    {
        private readonly List<string> sources = [];
        private readonly List<string> outputPaths = [];
        private int callCount;

        public int CallCount => Volatile.Read(ref callCount);

        public IReadOnlyList<string> Sources => sources;

        public IReadOnlyList<string> OutputPaths => outputPaths;

        public async Task<ThumbnailResult> GenerateAsync(
            string sourcePath,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref callCount);
            sources.Add(sourcePath);
            var result = await handler(sourcePath, cancellationToken);
            if (result.CachePath is not null)
            {
                outputPaths.Add(result.CachePath);
            }

            return result;
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "lamuflix-dev428-" + Guid.NewGuid().ToString("N"));
            LibraryRoot = Path.Combine(Root, "library");
            OutsideDirectory = Path.Combine(Root, "outside");
            SiblingDirectory = Path.Combine(Root, "library-evil");
            CacheRoot = Path.Combine(Root, "cache");
            Directory.CreateDirectory(LibraryRoot);
            Directory.CreateDirectory(OutsideDirectory);
            Directory.CreateDirectory(SiblingDirectory);
            Directory.CreateDirectory(CacheRoot);
        }

        public string Root { get; }

        public string LibraryRoot { get; }

        public string OutsideDirectory { get; }

        private string SiblingDirectory { get; }

        public string CacheRoot { get; }

        public string LibraryFile(string name) => WriteFile(Path.Combine(LibraryRoot, name));

        public string OutsideFile(string name) => WriteFile(Path.Combine(OutsideDirectory, name));

        public string SiblingPrefixFile(string name) => WriteFile(Path.Combine(SiblingDirectory, name));

        public string WriteGeneratorOutput(byte[] bytes)
        {
            var path = Path.Combine(CacheRoot, Guid.NewGuid().ToString("N") + ".jpg");
            File.WriteAllBytes(path, bytes);
            return path;
        }

        public string CanonicalPath(MovieId movieId) =>
            Path.Combine(CacheRoot, movieId.Value + ".jpg");

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _ = exception;
            }
        }

        private static string WriteFile(string path)
        {
            File.WriteAllBytes(path, [0x00]);
            return path;
        }
    }
}
