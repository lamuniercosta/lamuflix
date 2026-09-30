using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LamuFlix.Infrastructure.Persistence.Configurations;

public sealed class GenreConfiguration : IEntityTypeConfiguration<GenreRecord>
{
    public void Configure(EntityTypeBuilder<GenreRecord> builder)
    {
        builder.ToTable("genres");
        builder.HasKey(genre => genre.Id);
        builder.Property(genre => genre.Id)
            .HasColumnName("id")
            .HasColumnType("integer")
            .ValueGeneratedOnAdd()
            .UseIdentityByDefaultColumn();
        builder.Property(genre => genre.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();
    }
}
