# DEV-299 Assumptions

- [assumed] Q2: Name the return contract `EnrichmentFailureDecision` and its closed action set `EnrichmentFailureAction` (`Retry`, `RetryDelayed`, `DeadLetter`). Basis: DEV-299 Scope item 3; constitution II:130-141 and Enrichment Reliability:449-452.
- [assumed] Q5: Name the shared no-value handler result `Unit`. Basis: `ICommandHandler.cs:6-9`; DEV-299 Watchlist and Playback scope.
- [assumed] Q11: Title the ADR `Enrichment decisions in Core handlers; transport in the consumer`. Basis: constitution Enrichment Reliability:449-452 and ADR rule:477-480.
