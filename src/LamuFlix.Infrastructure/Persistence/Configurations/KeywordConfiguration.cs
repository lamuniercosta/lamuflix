using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LamuFlix.Infrastructure.Persistence.Configurations;

public sealed class KeywordConfiguration : IEntityTypeConfiguration<KeywordRecord>
{
    public void Configure(EntityTypeBuilder<KeywordRecord> builder)
    {
        builder.ToTable("keywords");
        builder.HasKey(keyword => keyword.Id);
        builder.Property(keyword => keyword.Id)
            .HasColumnName("id")
            .HasColumnType("integer")
            .ValueGeneratedNever();
        builder.Property(keyword => keyword.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();
    }
}
