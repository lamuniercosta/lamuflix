namespace LamuFlix.Infrastructure.RabbitMq;

public sealed class FeaturesConsumerLiveness : IWorkerLiveness
{
    private volatile bool isListening;

    public bool IsListening => isListening;

    public void MarkListening() => isListening = true;

    public void MarkStopped() => isListening = false;
}
