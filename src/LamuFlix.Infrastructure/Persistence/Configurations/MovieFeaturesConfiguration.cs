using LamuFlix.Infrastructure.Persistence.Converters;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LamuFlix.Infrastructure.Persistence.Configurations;

public sealed class MovieFeaturesConfiguration : IEntityTypeConfiguration<MovieFeaturesRecord>
{
    public void Configure(EntityTypeBuilder<MovieFeaturesRecord> builder)
    {
        builder.ToTable("movie_features");
        ConvertUsing(
            builder.Property(movieFeatures => movieFeatures.MovieId)
                .HasColumnName("movie_id")
                .HasColumnType("integer")
                .ValueGeneratedNever()
                .IsRequired(),
            new MovieIdConverter());
        builder.HasKey(movieFeatures => movieFeatures.MovieId)
            .HasName("PK_movie_features");
        builder.Property(movieFeatures => movieFeatures.Vector)
            .HasColumnName("vector")
            .HasColumnType("vector(384)")
            .IsRequired();
        builder.Property(movieFeatures => movieFeatures.ComputedAt)
            .HasColumnName("computed_at")
            .HasColumnType("timestamptz")
            .IsRequired();
        builder.HasOne<MovieRecord>()
            .WithMany()
            .HasForeignKey(movieFeatures => movieFeatures.MovieId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConvertUsing(PropertyBuilder property, ValueConverter converter) =>
        property.HasConversion(converter);
}
