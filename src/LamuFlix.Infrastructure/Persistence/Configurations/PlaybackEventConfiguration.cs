using LamuFlix.Infrastructure.Persistence.Converters;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LamuFlix.Infrastructure.Persistence.Configurations;

public sealed class PlaybackEventConfiguration : IEntityTypeConfiguration<PlaybackEventRecord>
{
    public void Configure(EntityTypeBuilder<PlaybackEventRecord> builder)
    {
        builder.ToTable("playback_events");
        builder.Property(playbackEvent => playbackEvent.Id)
            .HasColumnName("id")
            .HasColumnType("integer")
            .ValueGeneratedOnAdd()
            .UseIdentityByDefaultColumn();
        ConvertUsing(
            builder.Property(playbackEvent => playbackEvent.MovieId)
                .HasColumnName("movie_id")
                .HasColumnType("integer")
                .IsRequired(),
            new MovieIdConverter());
        builder.Property(playbackEvent => playbackEvent.PlayedAt)
            .HasColumnName("played_at")
            .HasColumnType("timestamptz")
            .IsRequired();
        builder.HasOne<MovieRecord>()
            .WithMany()
            .HasForeignKey(playbackEvent => playbackEvent.MovieId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConvertUsing(PropertyBuilder property, ValueConverter converter) =>
        property.HasConversion(converter);
}
