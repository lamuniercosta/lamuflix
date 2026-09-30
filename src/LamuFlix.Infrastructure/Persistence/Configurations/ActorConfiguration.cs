using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LamuFlix.Infrastructure.Persistence.Configurations;

public sealed class ActorConfiguration : IEntityTypeConfiguration<ActorRecord>
{
    public void Configure(EntityTypeBuilder<ActorRecord> builder)
    {
        builder.ToTable("actors");
        builder.Property(actor => actor.Id)
            .HasColumnName("id")
            .HasColumnType("integer")
            .ValueGeneratedOnAdd()
            .UseIdentityByDefaultColumn();
        builder.Property(actor => actor.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();
    }
}
