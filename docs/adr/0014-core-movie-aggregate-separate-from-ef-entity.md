# 0014. Core Movie aggregate, separate from the EF entity

- Status: Proposed (Gate 1 closed; becomes Accepted when the DEV-294 spec PR merges)
- Date: 2026-09-26
- Ticket: DEV-294 (parent DEV-282)

## Context

The only `Movie` today is the EF entity `LamuFlix.Data.Models.Movie`, with its status enum
`MovieEnrichmentStatus`. Enrichment state changes happen by direct assignment in
`EnrichmentJobProcessor` and `MovieService`, so no single type owns the rules for which
transitions are legal. DEV-294 asks for a `Movie` aggregate root in `src/LamuFlix.Core/Domain/`
with private setters, state-transition methods, sealed-record value objects, and an
`InvalidTransitionException` mapped to HTTP 409. The ticket names no migration, no EF change,
and none of the existing Data, Worker, or Web files (§2.3 #3 and #6). Core must stay free of
EF Core, Npgsql, RabbitMQ, and Infrastructure references (`ArchitectureTests`). ADR-0010 bans
the ambient clock.

## Decision

- Add a Core domain model beside the EF entity, not in place of it: `LamuFlix.Core.Domain.Movie`
  (a `sealed class` with private setters), `EnrichmentStatus`, `EnrichmentFailureCategory`,
  `MovieMetadata`, `InvalidTransitionException`, and the value objects `MovieId`, `ImdbId`,
  `ImdbRating`, `Runtime`, `ReleaseYear`, `LibraryPath`, and `MediaFormat`. The model uses only
  the BCL.
- `Movie.Create(MovieId, title, LibraryPath, MediaFormat)` is the only way to create one. It
  starts `Pending` with zero attempts and is not in the watchlist.
- The aggregate enforces the transition table. `MarkEnriched`, `MarkNotFound`, and `MarkFailed`
  are legal only from `Pending`. `RequestEnrichment` is legal only from `Enriched`, `NotFound`,
  or `Failed`. `EnrichmentAttempts` never resets. The watchlist is independent of status, and a
  duplicate add or an absent remove throws. Any illegal call throws
  `InvalidTransitionException` and changes nothing.
- Time is passed in as `DateTimeOffset now` on every time-dependent call, including
  `ReleaseYear`. The domain never reads a clock.
- Invariants live in the value-object constructors. Each value object is a `sealed record` with
  a validating constructor, a get-only property (no `init`, so `with` cannot skip validation), a
  `TryCreate` factory with the same rules, and `ArgumentException`-family errors.
- The Core `EnrichmentStatus` is independent of `MovieEnrichmentStatus`, and Core `MovieMetadata`
  is independent of the Worker type of the same name. The duplication is accepted.
- `InvalidTransitionException` maps to HTTP 409. This ADR records that mapping; the type carries
  no XML doc. DEV-294 adds no endpoint, middleware, or `IExceptionHandler`.

## Consequences

- DEV-294 adds no persistence and no wiring. The EF entity, `LamuFlixContext`, the schema,
  `EnrichmentJobProcessor`, and `MovieService` stay unchanged. The aggregate lives in memory only
  and has no rehydration factory.
- Persistence, rehydration, and adoption of the aggregate are one follow-up ticket under
  DEV-282. Until it lands, two `Movie` types and two status enums coexist, and code that uses
  both needs qualified names or aliases.
- The HTTP 409 mapping is a follow-up. No `IExceptionHandler` exists under `src/`, so the
  constitution PR-gate line "new exception types are mapped in the single IExceptionHandler" is
  not met in DEV-294. It is a recorded deviation that the same follow-up carries.
- The domain's watchlist methods throw where `MovieService`'s are idempotent. This difference is
  intentional and recorded, not a defect.
- Tests for the domain can run on pure values with no database, container, or clock.
