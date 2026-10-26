namespace LamuFlix.Core.Ports;

public interface IEnrichmentOutbox
{
    void Stage(EnrichmentRequested request);
}
