# C4 Component diagram (LamuFlix.Api container)

Ports live in Core, adapters in Infrastructure (ADRs 0001, 0002, 0015).
Every use case is a sealed handler behind the fixed decorator chain
Tracing -> Logging -> Validation -> handler (ADR 0015). The single
`IExceptionHandler` maps `ValidationException` -> 422, `NotFoundException`
-> 404, and `InvalidTransitionException` -> 409 (ADRs 0014, 0015, 0017).

```mermaid
flowchart TB
    subgraph Api["LamuFlix.Api container"]
        ep["Minimal API endpoints<br/>/api library, import, enrichment,<br/>watchlist, playback, facets<br/>/health/live, /health/ready<br/>OpenAPI + Scalar (Development only)"]
        exc["GlobalExceptionHandler<br/>422 / 404 / 409 ProblemDetails"]
        handlers["Core feature handlers<br/>Library, Import, Enrichment,<br/>Watchlist, Playback<br/>+ Tracing/Logging/Validation decorators"]
        ports["Core ports<br/>IMovieRepository, IMovieCatalog,<br/>IEnrichmentQueue, IMetadataProvider,<br/>IMediaLibraryScanner, IMediaPlayerLauncher"]
        consumer["EnrichmentConsumer<br/>owns transport: retry republish,<br/>delayed republish, dead-letter, ack"]
        sweeper["StrandedMovieSweeper<br/>re-enqueues stranded Pending<br/>without claiming"]
        repo["EF adapters<br/>EfMovieRepository, EfMovieCatalog<br/>validating converters"]
        mqpub["RabbitMQ publisher<br/>confirms + W3C propagation"]
        omdba["OMDb metadata provider<br/>sentinel-redacted telemetry"]
        scan["FileSystem scanner"]
        play["Launcher adapter<br/>LocalPlay-gated;<br/>disabled launcher otherwise"]
        sd["ServiceDefaults<br/>OTel composition, health checks,<br/>validated options binding"]
    end

    pg[("PostgreSQL")]
    rmq[("RabbitMQ")]
    omdb[("OMDb API")]
    fs[("Media folders")]
    player(["Media player process")]

    ep --> handlers
    handlers --> exc
    handlers --> ports
    ports -.-> repo
    ports -.-> mqpub
    ports -.-> omdba
    ports -.-> scan
    ports -.-> play
    consumer --> handlers
    consumer --> mqpub
    sweeper --> mqpub
    repo --> pg
    mqpub --> rmq
    rmq --> consumer
    omdba --> omdb
    scan --> fs
    play --> player
    sd -.- ep
    sd -.- handlers
```
