using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.Infrastructure.Adapters;

public sealed class OllamaEmbeddingModelState
{
    private readonly object gate = new();
    private Snapshot snapshot = Snapshot.Empty;

    internal Snapshot Current
    {
        get
        {
            lock (gate)
            {
                return snapshot;
            }
        }
    }

    internal void PublishVerified(Uri endpoint, string model, int dimensions)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(model);

        lock (gate)
        {
            snapshot = new Snapshot(IsVerified: true, endpoint, model, dimensions, LastFailure: null);
        }
    }

    internal void RecordSuccess()
    {
        lock (gate)
        {
            snapshot = snapshot with { LastFailure = null };
        }
    }

    internal void RecordFailure(EnrichmentFailureCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);

        lock (gate)
        {
            snapshot = snapshot with { LastFailure = category };
        }
    }

    internal int? VerifiedDimensions(Uri endpoint, string model)
    {
        lock (gate)
        {
            return snapshot.IsVerified
                && string.Equals(snapshot.Model, model, StringComparison.Ordinal)
                && Equals(snapshot.Endpoint, endpoint)
                    ? snapshot.Dimensions
                    : null;
        }
    }

    internal sealed record Snapshot(
        bool IsVerified,
        Uri? Endpoint,
        string? Model,
        int? Dimensions,
        EnrichmentFailureCategory? LastFailure)
    {
        public static Snapshot Empty { get; } = new(
            IsVerified: false,
            Endpoint: null,
            Model: null,
            Dimensions: null,
            LastFailure: null);
    }
}
