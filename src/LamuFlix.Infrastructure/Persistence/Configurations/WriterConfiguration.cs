using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LamuFlix.Infrastructure.Persistence.Configurations;

public sealed class WriterConfiguration : IEntityTypeConfiguration<WriterRecord>
{
    public void Configure(EntityTypeBuilder<WriterRecord> builder)
    {
        builder.ToTable("writers");
        builder.Property(writer => writer.Id)
            .HasColumnName("id")
            .HasColumnType("integer")
            .ValueGeneratedOnAdd()
            .UseIdentityByDefaultColumn();
        builder.Property(writer => writer.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();
    }
}
