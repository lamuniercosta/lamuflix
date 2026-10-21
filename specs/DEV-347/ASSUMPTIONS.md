# DEV-347 — Taste & Minor Naming Assumptions

**Feature:** DEV-347 — Define `FeaturesRequested` message, topology, and publish after enrichment succeeds  
**Authority:** Patron (Proxy Owner)

- `[assumed]` **Features queue & routing key naming convention:** Following `enrichment.requested` / `enrichment.retry` / `enrichment.dead-letter` in `RabbitMqTopology` and the ticket's explicit queue name `features.requested` (`DEV-347:41`), the companion retry and dead-letter queues are named `features.retry` and `features.dead-letter`, and the routing keys on the shared `lamuflix.enrichment` direct exchange are `features.requested`, `features.retry`, and `features.dead-letter` (keeping `requested`, `retry`, and `dead-letter` untouched for enrichment).
- `[assumed]` **Initial attempt number:** When enrichment succeeds with a non-empty plot, `FeaturesRequested` is published with `Attempt = 1`, matching the 1-based attempt convention used by `EnrichmentRequested(movie.Id, Attempt: 1)` across all enqueue handlers (`RequestEnrichmentCommandHandler`, `ImportMovieFolderCommandHandler`, `MatchMovieImdbIdCommandHandler`, `RequeueStrandedMoviesCommandHandler`).
- `[assumed]` **Non-empty plot predicate:** "Non-empty" `FullPlot` (`MovieMetadata.Synopsis`) uses `!string.IsNullOrWhiteSpace(movie.Metadata?.Synopsis)` so whitespace-only plot strings are treated the same as empty/null and are not queued.
