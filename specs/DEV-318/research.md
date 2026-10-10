# Research: DEV-318 End-to-End Import-to-Enrichment Integration Proof

**Date**: 2026-10-09
**Sources**: recon-DEV-318:18-136,146-182; specs/DEV-318/brief.md Q1-Q4, Q6-Q7.

Phase 0 has no open unknowns: the bounded recon supplement closed the one registration
question and Patron accepted the resolution without reopening Q3. Findings consolidated here.

## 1. Import path (decided: reuse as-is)

POST `/api/movies/import` (`ImportEndpoints.MapImportEndpoints`) dispatches
`ImportMovieFolderCommand` through the decorated handler, persists the Pending movie, and
enqueues `EnrichmentRequested` before returning 202 with `Location: /api/movies/{id}`.
GET `/api/movies/{id}` returns the existing `MovieDetailsResponse` contract.

## 2. Broker path (decided: real crossing)

The publisher starts the `Enrichment.Enqueue` activity and injects traceparent/tracestate
into headers (`TraceContextCarrier`); quorum topology `lamuflix.enrichment` /
`enrichment.requested`; the active `EnrichmentConsumer` hosted in the API consumes with a
per-delivery `AsyncServiceScope`, extracts the wire parent, and starts the consumer activity.

## 3. Handler registration seam (decided: scoped outer gate)

The `ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>` descriptor is
Scoped with an ImplementationFactory; composition resolves Tracing, Logging, Validation,
handler. `configureTestServices` (via `StartHostAsync`) runs after app registration under
ValidateOnBuild/ValidateScopes. The test captures the original descriptor and factory,
asserts the scoped factory form, and replaces only that interface descriptor with a scoped
outer gate invoking the captured factory with the same scoped provider.

## 4. OMDb path (decided: WireMock-backed production provider)

`AddMetadataProvider` registers the typed `OmdbMetadataProvider` against the WireMock base
URL with the sentinel key; `ProcessEnrichmentCommandHandler` calls `FindAsync` with title
and year. Existing stub body and measured-evidence assertions (method, path, apikey/title/
type query) are kept; exactly one request is asserted.

## 5. Trace capture seams (decided: listener + unique traceparent)

Existing listener patterns sample AllData and collect stopped spans; producer-to-consumer
trace sharing is proven but no test starts the trace from an HTTP request through broker
headers. This feature adds: disposable listener installed before host startup, unique W3C
traceparent/tracestate on the request, no ambient test Activity, correlation by request
TraceId, parent-span equalities, Consumer kind, tracestate, and zero-link assertions.

## 6. Alternatives rejected

- Resurrecting the retired worker project or adding a host: rejected; proves nothing about
  the actual broker crossing and adds an unauthorized project (Q1).
- Mocking queue/repository/provider or fixed delays: rejected; constitution IX requires the
  real stack (Q3).
- Stealing delivery with BasicGet for header inspection: rejected; proof must observe the
  live consumer span ancestry (Q4).
- Synthetic unit tests duplicating helper logic: rejected; the held-then-released real
  scenario exercises synchronization (Q6).
