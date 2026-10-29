using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LamuFlix.Infrastructure.Persistence.Configurations;

public sealed class CollectionConfiguration : IEntityTypeConfiguration<CollectionRecord>
{
    public void Configure(EntityTypeBuilder<CollectionRecord> builder)
    {
        builder.ToTable("collections");
        builder.HasKey(collection => collection.Id);
        builder.Property(collection => collection.Id)
            .HasColumnName("id")
            .HasColumnType("integer")
            .ValueGeneratedNever();
        builder.Property(collection => collection.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();
    }
}
