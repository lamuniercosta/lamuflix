using System;
using LamuFlix.Infrastructure.Persistence.Converters;
using LamuFlix.Infrastructure.Persistence.ValueGenerators;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LamuFlix.Infrastructure.Persistence.Configurations;

public sealed class MovieConfiguration : IEntityTypeConfiguration<MovieRecord>
{
    public void Configure(EntityTypeBuilder<MovieRecord> builder)
    {
        builder.ToTable("movies");
        ConfigureKey(builder);
        ConfigureCoreScalars(builder);
        ConfigureMetadata(builder);
        ConfigureIndexes(builder);
        ConfigureSkipNavigations(builder);
    }

    private static void ConfigureKey(EntityTypeBuilder<MovieRecord> builder)
    {
        builder.HasKey(movie => movie.Id);
        ConvertUsing(
            builder.Property(movie => movie.Id)
                .HasColumnName("id")
                .HasColumnType("integer")
                .HasValueGenerator<MovieIdValueGenerator>()
                .ValueGeneratedOnAdd()
                .UseIdentityByDefaultColumn(),
            new MovieIdConverter());
    }

    private static void ConfigureCoreScalars(EntityTypeBuilder<MovieRecord> builder)
    {
        builder.Property(movie => movie.Title)
            .HasColumnName("title")
            .HasColumnType("text")
            .IsRequired();
        builder.Property(movie => movie.LibraryPath)
            .HasColumnName("library_path")
            .HasColumnType("text")
            .HasConversion(new LibraryPathConverter())
            .IsRequired();
        builder.Property(movie => movie.Format)
            .HasColumnName("format")
            .HasColumnType("text")
            .HasConversion(new MediaFormatConverter())
            .IsRequired();
        builder.Property(movie => movie.IsInWatchlist)
            .HasColumnName("is_in_watchlist")
            .HasColumnType("boolean")
            .IsRequired();
        builder.Property(movie => movie.Status)
            .HasColumnName("status")
            .HasColumnType("integer")
            .HasConversion(new EnrichmentStatusConverter())
            .IsRequired();
        builder.Property(movie => movie.EnrichedAt)
            .HasColumnName("enriched_at")
            .HasColumnType("timestamptz")
            .IsRequired(false);
        builder.Property(movie => movie.EnrichmentAttempts)
            .HasColumnName("enrichment_attempts")
            .HasColumnType("integer")
            .IsRequired();
        ConvertUsing(
            builder.Property(movie => movie.EnrichmentFailureCategory)
                .HasColumnName("enrichment_failure_category")
                .HasColumnType("varchar(32)")
                .IsRequired(false),
            new EnrichmentFailureCategoryConverter());
        builder.Property(movie => movie.LastAttemptAt)
            .HasColumnName("last_attempt_at")
            .HasColumnType("timestamptz")
            .IsRequired(false);
    }

    private static void ConfigureMetadata(EntityTypeBuilder<MovieRecord> builder)
    {
        builder.Property(movie => movie.MetadataTitle)
            .HasColumnName("metadata_title")
            .HasColumnType("text")
            .IsRequired(false);
        ConvertUsing(
            builder.Property(movie => movie.RuntimeMinutes)
                .HasColumnName("runtime_minutes")
                .HasColumnType("integer")
                .IsRequired(false),
            new RuntimeConverter());
        ConvertUsing(
            builder.Property(movie => movie.ReleaseYear)
                .HasColumnName("release_year")
                .HasColumnType("integer")
                .IsRequired(false),
            new ReleaseYearConverter(TimeProvider.System));
        ConvertUsing(
            builder.Property(movie => movie.ImdbRating)
                .HasColumnName("imdb_rating")
                .HasColumnType("numeric(3,1)")
                .IsRequired(false),
            new ImdbRatingConverter());
        ConvertUsing(
            builder.Property(movie => movie.ImdbId)
                .HasColumnName("imdb_id")
                .HasColumnType("text")
                .IsRequired(false),
            new ImdbIdConverter());
        builder.Property(movie => movie.RottenTomatoesRating)
            .HasColumnName("rotten_tomatoes_rating")
            .HasColumnType("smallint")
            .IsRequired(false);
        builder.Property(movie => movie.MetaScore)
            .HasColumnName("meta_score")
            .HasColumnType("smallint")
            .IsRequired(false);
        builder.Property(movie => movie.Plot)
            .HasColumnName("plot")
            .HasColumnType("text")
            .IsRequired(false);
        builder.Property(movie => movie.PosterUrl)
            .HasColumnName("poster_url")
            .HasColumnType("text")
            .IsRequired(false);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<MovieRecord> builder)
    {
        builder.HasIndex(movie => movie.Title);
        builder.HasIndex(movie => movie.ReleaseYear);
        builder.HasIndex(movie => movie.Status);
        builder.HasIndex(movie => movie.LibraryPath).IsUnique();
        builder.HasIndex(movie => movie.ImdbId)
            .IsUnique()
            .HasFilter("imdb_id IS NOT NULL");
    }

    private static void ConfigureSkipNavigations(EntityTypeBuilder<MovieRecord> builder)
    {
        builder.HasMany(movie => movie.Actors)
            .WithMany(actor => actor.Movies)
            .UsingEntity(
                "movie_actors",
                right => right.HasOne(typeof(ActorRecord)).WithMany().HasForeignKey("actor_id"),
                left => left.HasOne(typeof(MovieRecord)).WithMany().HasForeignKey("movie_id"));
        builder.HasMany(movie => movie.Directors)
            .WithMany(director => director.Movies)
            .UsingEntity(
                "movie_directors",
                right => right.HasOne(typeof(DirectorRecord)).WithMany().HasForeignKey("director_id"),
                left => left.HasOne(typeof(MovieRecord)).WithMany().HasForeignKey("movie_id"));
        builder.HasMany(movie => movie.Genres)
            .WithMany(genre => genre.Movies)
            .UsingEntity(
                "movie_genres",
                right => right.HasOne(typeof(GenreRecord)).WithMany().HasForeignKey("genre_id"),
                left => left.HasOne(typeof(MovieRecord)).WithMany().HasForeignKey("movie_id"));
    }

    private static void ConvertUsing(PropertyBuilder property, ValueConverter converter) =>
        property.HasConversion(converter);
}
