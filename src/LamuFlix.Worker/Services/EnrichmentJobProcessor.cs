using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using LamuFlix.Data;
using LamuFlix.Data.Models;
using LamuFlix.Worker.Models;

namespace LamuFlix.Worker.Services;

public interface IEnrichmentJobProcessor
{
    Task ProcessMessageAsync(MovieEnrichmentMessage message, IModel channel, ulong deliveryTag);
}

public class EnrichmentJobProcessor(
    IServiceScopeFactory scopeFactory,
    IMetadataProvider metadataProvider,
    IConfiguration configuration,
    ILogger<EnrichmentJobProcessor> logger)
    : IEnrichmentJobProcessor
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly IMetadataProvider _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly ILogger<EnrichmentJobProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task ProcessMessageAsync(MovieEnrichmentMessage message, IModel channel, ulong deliveryTag)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LamuFlixContext>();

        try
        {
            var movie = await db.Movies
                .Include(x => x.Actors).ThenInclude(x => x.Actor)
                .Include(x => x.Genres).ThenInclude(x => x.Genre)
                .Include(x => x.Directors).ThenInclude(x => x.Director)
                .SingleOrDefaultAsync(x => x.Id == message.MovieId);

            if (movie == null)
            {
                _logger.LogWarning("Movie with ID {MovieId} not found in database. Acknowledging message.", message.MovieId);
                channel.BasicAck(deliveryTag, false);
                return;
            }

            if (movie.Status == MovieEnrichmentStatus.Enriched)
            {
                _logger.LogInformation("Movie {MovieId} is already enriched. Acknowledging idempotently.", message.MovieId);
                channel.BasicAck(deliveryTag, false);
                return;
            }

            var metadata = await _metadataProvider.FetchMetadataAsync(message.Title, message.Year, message.ImdbId);
            await HandleEnrichmentResultAsync(db, movie, metadata);

            channel.BasicAck(deliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing enrichment job for movie {MovieId} (RetryCount: {RetryCount}).", message.MovieId, message.RetryCount);
            await HandleFailureAsync(db, channel, message, deliveryTag, ex);
        }
    }

    private async Task HandleEnrichmentResultAsync(LamuFlixContext db, Movie movie, MovieMetadata? metadata)
    {
        if (metadata == null)
        {
            movie.Status = MovieEnrichmentStatus.NotFound;
            _logger.LogInformation("Movie {MovieId} ('{Title}') not found in metadata provider.", movie.Id, movie.Title);
        }
        else
        {
            ApplyEnrichedMetadata(db, movie, metadata);
            movie.Status = MovieEnrichmentStatus.Enriched;
            _logger.LogInformation("Successfully enriched movie {MovieId} ('{Title}').", movie.Id, movie.Title);
        }

        await db.SaveChangesAsync();
    }

    private static void ApplyEnrichedMetadata(LamuFlixContext db, Movie movie, MovieMetadata metadata)
    {
        UpdateMovieScalarFields(movie, metadata);
        UpdateMovieRatingsAndIds(movie, metadata);
        SyncRelations(db, movie, metadata);
    }

    private static void UpdateMovieScalarFields(Movie movie, MovieMetadata metadata)
    {
        movie.Synopsis = metadata.Synopsis ?? movie.Synopsis;
        movie.Year = metadata.Year ?? movie.Year;
        movie.Duration = metadata.DurationMinutes ?? movie.Duration;
        movie.Poster = metadata.PosterUrl ?? movie.Poster;
    }

    private static void UpdateMovieRatingsAndIds(Movie movie, MovieMetadata metadata)
    {
        movie.ImdbRating = metadata.ImdbRating ?? movie.ImdbRating;
        movie.RottenTomatoes = metadata.RottenTomatoesScore ?? movie.RottenTomatoes;
        movie.MetaScore = metadata.MetaScore ?? movie.MetaScore;
        movie.IdImdb = metadata.ImdbId ?? movie.IdImdb;
    }

    private static void SyncRelations(LamuFlixContext db, Movie movie, MovieMetadata metadata)
    {
        SyncActors(db, movie, metadata.Actors);
        SyncGenres(db, movie, metadata.Genres);
        SyncDirectors(db, movie, metadata.Directors);
    }

    private static void SyncActors(LamuFlixContext db, Movie movie, IReadOnlyList<string>? actors)
    {
        if (actors == null) return;
        foreach (var name in actors)
        {
            var actor = db.Actors.SingleOrDefault(x => x.Name == name);
            if (actor == null)
            {
                actor = new Actor { Name = name };
                db.Actors.Add(actor);
            }
            if (movie.Actors.All(x => x.Actor.Name != name))
            {
                movie.Actors.Add(new MovieActors { Movie = movie, Actor = actor });
            }
        }
    }

    private static void SyncGenres(LamuFlixContext db, Movie movie, IReadOnlyList<string>? genres)
    {
        if (genres == null) return;
        foreach (var name in genres)
        {
            var genre = db.Genres.SingleOrDefault(x => x.Name == name);
            if (genre == null)
            {
                genre = new Genre { Name = name };
                db.Genres.Add(genre);
            }
            if (movie.Genres.All(x => x.Genre.Name != name))
            {
                movie.Genres.Add(new MovieGenre { Movie = movie, Genre = genre });
            }
        }
    }

    private static void SyncDirectors(LamuFlixContext db, Movie movie, IReadOnlyList<string>? directors)
    {
        if (directors == null) return;
        foreach (var name in directors)
        {
            var director = db.Directors.SingleOrDefault(x => x.Name == name);
            if (director == null)
            {
                director = new Director { Name = name };
                db.Directors.Add(director);
            }
            if (movie.Directors.All(x => x.Director.Name != name))
            {
                movie.Directors.Add(new MovieDirectors { Movie = movie, Director = director });
            }
        }
    }

    private async Task HandleFailureAsync(LamuFlixContext db, IModel channel, MovieEnrichmentMessage message, ulong deliveryTag, Exception ex)
    {
        _logger.LogDebug(ex, "Handling failure exception for movie {MovieId}.", message.MovieId);
        if (message.RetryCount < 3)
        {
            message.RetryCount++;
            var queueName = _configuration["RabbitMQ:QueueName"] ?? "task_queue";
            PublishMessage(channel, queueName, message);
            _logger.LogInformation("Republished message for movie {MovieId} with RetryCount {RetryCount}.", message.MovieId, message.RetryCount);
        }
        else
        {
            var movie = await db.Movies.SingleOrDefaultAsync(x => x.Id == message.MovieId);
            if (movie != null)
            {
                movie.Status = MovieEnrichmentStatus.Failed;
                await db.SaveChangesAsync();
            }

            var dlqName = _configuration["RabbitMQ:DlqName"] ?? "task_queue_dlq";
            PublishMessage(channel, dlqName, message);
            _logger.LogWarning("Max retries exceeded for movie {MovieId}. Forwarded to DLQ '{DlqName}' and marked Failed.", message.MovieId, dlqName);
        }

        channel.BasicAck(deliveryTag, false);
    }

    private static void PublishMessage(IModel channel, string targetQueue, MovieEnrichmentMessage message)
    {
        channel.QueueDeclare(queue: targetQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);
        var props = channel.CreateBasicProperties();
        props?.Persistent = true;
        channel.BasicPublish(exchange: "", routingKey: targetQueue, basicProperties: props, body: body);
    }
}