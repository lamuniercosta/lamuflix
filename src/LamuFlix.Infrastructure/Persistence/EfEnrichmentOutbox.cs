using System;
using System.Text.Json;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence.Records;

namespace LamuFlix.Infrastructure.Persistence;

public sealed class EfEnrichmentOutbox : IEnrichmentOutbox
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly LamuFlixDbContext db;
    private readonly TimeProvider timeProvider;

    public EfEnrichmentOutbox(LamuFlixDbContext db, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(timeProvider);
        this.db = db;
        this.timeProvider = timeProvider;
    }

    public void Stage(EnrichmentRequested request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var payload = JsonSerializer.SerializeToUtf8Bytes(request, SerializerOptions);
        db.OutboxMessages.Add(new OutboxMessageRecord
        {
            Payload = payload,
            CreatedAt = timeProvider.GetUtcNow(),
        });
    }
}
