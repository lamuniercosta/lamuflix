using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Thumbnails;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LamuFlix.Infrastructure.RabbitMq;

public sealed class ThumbnailConsumer(
    RabbitMqConnectionOwner owner,
    RabbitMqTopology topology,
    IServiceScopeFactory scopes,
    IOptions<RabbitMqOptions> options,
    IOptions<LibraryOptions> library,
    IThumbnailGenerator generator,
    ThumbnailCache cache,
    IHostEnvironment hostEnvironment,
    ILogger<ThumbnailConsumer> logger) : BackgroundService
{
    private const string MalformedMessage = "MalformedMessage";
    private const string IneligibleRequest = "IneligibleRequest";
    private const string UnauthorizedSourcePath = "UnauthorizedSourcePath";
    private const string CachePromotionFailed = "CachePromotionFailed";
    private const string MissingGeneratorReason = "ExtractionFailed";
    private const string ErrorEventName = "error";
    private const string MalformedLogMessage =
        "Thumbnail message could not be read as a request with {FailureReason} and is dead-lettered redelivered {Redelivered}";
    private const string TerminalLogMessage =
        "Thumbnail processing failed with {FailureReason} for {MovieId} redelivered {Redelivered}";
    private const string UnexpectedLogMessage =
        "Thumbnail processing for {MovieId} failed unexpectedly with {ErrorType} and is dead-lettered redelivered {Redelivered}";
    private const string ShutdownLogMessage =
        "Thumbnail processing for {MovieId} requeued because the host is stopping";
    private const string CompletedLogMessage =
        "Thumbnail processing for {MovieId} completed";

    private static readonly ActivitySource Source = new(TelemetryConstants.ActivitySourceName);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    private readonly ConsumerDrain drain = new();
    private IChannel? channel;
    private string? consumerTag;
    private CancellationToken hostShutdown;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await topology.EnsureDeclaredAsync(stoppingToken);

                var connection = await owner.GetAsync(stoppingToken);
                var active = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                channel = active;
                try
                {
                    await active.BasicQosAsync(0, options.Value.Prefetch, global: false, stoppingToken);

                    var consumer = new AsyncEventingBasicConsumer(active);
                    consumer.ReceivedAsync += (_, delivery) => drain.Track(HandleAsync(active, delivery, stoppingToken));
                    consumerTag = await active.BasicConsumeAsync(
                        RabbitMqTopology.ThumbnailQueue,
                        autoAck: false,
                        consumer,
                        stoppingToken);

                    await WaitForShutdownAsync(connection, active, stoppingToken);
                }
                finally
                {
                    await ReleaseConsumerAsync(active);
                    channel = null;
                    await active.DisposeAsync();
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "The thumbnail consumer lost its RabbitMQ connection and will try to reconnect");
            }

            try
            {
                await Task.Delay(ReconnectDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private static async Task WaitForShutdownAsync(IConnection connection, IChannel active, CancellationToken stoppingToken)
    {
        var shutdown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task SignalShutdown(object? sender, ShutdownEventArgs args)
        {
            _ = sender;
            _ = args;
            shutdown.TrySetResult();
            return Task.CompletedTask;
        }

        active.ChannelShutdownAsync += SignalShutdown;
        connection.ConnectionShutdownAsync += SignalShutdown;
        try
        {
            await using (stoppingToken.Register(static state => { if (state is TaskCompletionSource tcs) tcs.TrySetResult(); }, shutdown))
            {
                await shutdown.Task;
            }
        }
        finally
        {
            active.ChannelShutdownAsync -= SignalShutdown;
            connection.ConnectionShutdownAsync -= SignalShutdown;
        }

        stoppingToken.ThrowIfCancellationRequested();
    }

    private async Task ReleaseConsumerAsync(IChannel active)
    {
        var tag = consumerTag;
        consumerTag = null;
        try
        {
            if (tag is null)
            {
                await drain.WhenIdleAsync(hostShutdown);
                return;
            }

            await drain.PauseAndDrainAsync(active, tag, hostShutdown);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "The thumbnail consumer could not be paused during shutdown");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        hostShutdown = cancellationToken;
        await base.StopAsync(cancellationToken);
        if (ExecuteTask is { IsCompleted: true } && channel is { IsOpen: true })
        {
            await channel.CloseAsync(cancellationToken: CancellationToken.None);
        }
    }

    private async Task HandleAsync(
        IChannel consumerChannel,
        BasicDeliverEventArgs delivery,
        CancellationToken stoppingToken)
    {
        var request = Read(delivery.Body);
        if (request is null)
        {
            logger.LogWarning(
                MalformedLogMessage,
                MalformedMessage,
                delivery.Redelivered);
            await RejectAsync(consumerChannel, delivery);
            return;
        }

        using var activity = StartActivity(delivery, request);
        try
        {
            var failureReason = await ProcessAsync(request, stoppingToken);
            if (failureReason is null)
            {
                logger.LogInformation(CompletedLogMessage, request.MovieId.Value);
                await AckAsync(consumerChannel, delivery);
                return;
            }

            logger.LogWarning(
                TerminalLogMessage,
                failureReason,
                request.MovieId.Value,
                delivery.Redelivered);
            await RejectAsync(consumerChannel, delivery);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(ShutdownLogMessage, request.MovieId.Value);
            await RequeueAsync(consumerChannel, delivery);
        }
        catch (Exception exception)
        {
            MarkError(activity, request.MovieId, exception);
            logger.LogError(
                UnexpectedLogMessage,
                request.MovieId.Value,
                exception.GetType().Name,
                delivery.Redelivered);
            await RejectAsync(consumerChannel, delivery);
        }
    }

    private async Task<string?> ProcessAsync(ThumbnailRequested request, CancellationToken cancellationToken)
    {
        var movie = await LoadMovieAsync(request.MovieId, cancellationToken);
        if (!IsEligible(movie, request.FilePath))
        {
            return IneligibleRequest;
        }

        if (IsCached(request.MovieId))
        {
            return null;
        }

        if (!TryAuthorizeSource(request.FilePath, out var authorizedSource))
        {
            return UnauthorizedSourcePath;
        }

        var result = await generator.GenerateAsync(authorizedSource, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            cache.DeleteTemporary(result.CachePath ?? string.Empty);
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (!result.Success)
        {
            return string.IsNullOrWhiteSpace(result.FailureReason)
                ? MissingGeneratorReason
                : result.FailureReason;
        }

        return !string.IsNullOrWhiteSpace(result.CachePath)
               && cache.TryPromote(result.CachePath, request.MovieId)
            ? null
            : CachePromotionFailed;
    }

    private async Task<Movie?> LoadMovieAsync(MovieId movieId, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var movies = scope.ServiceProvider.GetRequiredService<IMovieRepository>();
        return await movies.GetAsync(movieId, cancellationToken);
    }

    private bool IsCached(MovieId movieId)
    {
        if (!cache.TryOpenCanonical(movieId, out var stream))
        {
            return false;
        }

        stream?.Dispose();
        return true;
    }

    private static bool IsEligible(Movie? movie, string filePath) =>
        movie is not null
        && string.IsNullOrWhiteSpace(movie.Metadata?.PosterUrl)
        && !string.IsNullOrWhiteSpace(movie.Path.Value)
        && string.Equals(movie.Path.Value, filePath, PathComparison);

    private bool TryAuthorizeSource(string sourcePath, out string authorizedSource)
    {
        authorizedSource = string.Empty;
        if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(library.Value.RootPath))
        {
            return false;
        }

        string root;
        string source;
        try
        {
            root = Path.TrimEndingDirectorySeparator(
                Path.GetFullPath(ResolveRoot(library.Value.RootPath)));
            source = Path.GetFullPath(sourcePath);
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }

        if (!IsUnderRoot(source, root) || HasReparsePointInChain(source, root))
        {
            return false;
        }

        authorizedSource = source;
        return true;
    }

    private string ResolveRoot(string configured)
    {
        if (Path.IsPathRooted(configured))
        {
            return configured;
        }

        var contentRoot = string.IsNullOrWhiteSpace(hostEnvironment.ContentRootPath)
            ? AppContext.BaseDirectory
            : hostEnvironment.ContentRootPath;
        return Path.Combine(contentRoot, configured);
    }

    private static bool IsUnderRoot(string source, string root)
    {
        var rootWithSeparator = root + Path.DirectorySeparatorChar;
        return source.StartsWith(rootWithSeparator, PathComparison);
    }

    private static bool HasReparsePointInChain(string path, string root)
    {
        var current = path;
        while (!string.IsNullOrEmpty(current))
        {
            if (IsReparsePoint(current))
            {
                return true;
            }

            if (string.Equals(current, root, PathComparison))
            {
                return false;
            }

            var parent = Path.GetDirectoryName(current);
            if (parent is null || string.Equals(parent, current, PathComparison))
            {
                return false;
            }

            current = parent;
        }

        return false;
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return false;
        }
    }

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static Activity? StartActivity(BasicDeliverEventArgs delivery, ThumbnailRequested request)
    {
        var parent = ExtractContext(delivery.BasicProperties.Headers);
        var links = delivery.Redelivered
                    ? new List<ActivityLink> { new(parent) }
                    : [];
        var activity = Source.StartActivity(
                    TelemetryConstants.ThumbnailProcess,
                    ActivityKind.Consumer,
                    parent,
                    tags: null,
                    links);
        activity?.SetTag(TelemetryConstants.MovieId, request.MovieId.Value);
        activity?.SetTag(TelemetryConstants.MessagingDeliveryCount, delivery.Redelivered ? 1 : 0);
        return activity;
    }

    private static ActivityContext ExtractContext(IDictionary<string, object?>? headers)
    {
        try
        {
            return TraceContextCarrier.ExtractContext(headers);
        }
        catch (Exception)
        {
            return default;
        }
    }

    private static void MarkError(Activity? activity, MovieId movieId, Exception exception)
    {
        var errorType = exception.GetType().Name;
        activity?.SetStatus(ActivityStatusCode.Error);
        activity?.SetTag(TelemetryConstants.MovieId, movieId.Value);
        activity?.SetTag(TelemetryConstants.ErrorType, errorType);
        activity?.AddEvent(new ActivityEvent(
            ErrorEventName,
            tags: new ActivityTagsCollection
            {
                [TelemetryConstants.MovieId] = movieId.Value,
                [TelemetryConstants.ErrorType] = errorType,
            }));
    }

    private static ThumbnailRequested? Read(ReadOnlyMemory<byte> body)
    {
        try
        {
            return JsonSerializer.Deserialize<ThumbnailRequested>(body.Span, SerializerOptions)
                   is { MovieId: not null, FilePath: not null } read
                   && !string.IsNullOrWhiteSpace(read.FilePath)
                ? read
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ValueTask AckAsync(IChannel consumerChannel, BasicDeliverEventArgs delivery) =>
        consumerChannel.BasicAckAsync(delivery.DeliveryTag, multiple: false, CancellationToken.None);

    private static ValueTask RejectAsync(IChannel consumerChannel, BasicDeliverEventArgs delivery) =>
        consumerChannel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, CancellationToken.None);

    private static ValueTask RequeueAsync(IChannel consumerChannel, BasicDeliverEventArgs delivery) =>
        consumerChannel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, CancellationToken.None);
}
