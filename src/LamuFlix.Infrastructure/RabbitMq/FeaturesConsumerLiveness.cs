namespace LamuFlix.Infrastructure.RabbitMq;

/// <summary>
/// Concrete feature-consumer liveness state. It is deliberately separate from the enrichment worker
/// liveness so a feature consumer disconnect cannot be masked by metadata worker liveness, and it
/// deliberately adds no second liveness interface.
/// </summary>
public sealed class FeaturesConsumerLiveness : IWorkerLiveness
{
    private volatile bool isListening;

    public bool IsListening => isListening;

    public void MarkListening() => isListening = true;

    public void MarkStopped() => isListening = false;
}
