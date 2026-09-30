using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LamuFlix.Infrastructure.Persistence.Configurations;

public sealed class DirectorConfiguration : IEntityTypeConfiguration<DirectorRecord>
{
    public void Configure(EntityTypeBuilder<DirectorRecord> builder)
    {
        builder.ToTable("directors");
        builder.HasKey(director => director.Id);
        builder.Property(director => director.Id)
            .HasColumnName("id")
            .HasColumnType("integer")
            .ValueGeneratedOnAdd()
            .UseIdentityByDefaultColumn();
        builder.Property(director => director.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();
    }
}
