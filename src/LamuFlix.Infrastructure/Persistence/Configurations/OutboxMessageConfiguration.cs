using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LamuFlix.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessageRecord>
{
    public void Configure(EntityTypeBuilder<OutboxMessageRecord> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id)
            .HasColumnName("id")
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            .UseIdentityByDefaultColumn();
        builder.Property(message => message.Payload)
            .HasColumnName("payload")
            .HasColumnType("bytea")
            .IsRequired();
        builder.Property(message => message.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();
        builder.Property(message => message.ProcessedAt)
            .HasColumnName("processed_at")
            .HasColumnType("timestamptz")
            .IsRequired(false);
        builder.HasIndex(message => new { message.CreatedAt, message.Id })
            .HasFilter("processed_at IS NULL");
    }
}
