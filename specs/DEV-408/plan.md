# Implementation Plan: DEV-408 Persist enrichment poster URL

**Branch**: `feature/408-spec` | **Date**: 2026-10-12 | **Spec**: `specs/DEV-408/spec.md`

**Input**: Feature specification from `specs/DEV-408/spec.md`, settled grill brief `specs/DEV-408/brief.md`, facts from `recon-DEV-408`, Patron rulings in `specs/DEV-408/CONCLUSIONS.md`.

**Note**: Planning artifact only (Phase 2 step 3). No design, plan challenge, gates, or implementation are started by this ask.

## Summary

Carry the OMDB `Poster` value through the shipped Infrastructure provider into an optional Core `MovieMetadata.PosterUrl`, persist/clear it with the existing `poster_url` column through the current repository null-metadata branch reached by a narrow `Movie.ClearMetadata()` intent operation, and expose it as an always-present nullable `posterUrl` on `GET /api/movies` items, with regenerated C#-derived contracts and real-Postgres plus WireMock proofs. The orphan Worker stays unchanged with its rationale recorded.

## Technical Context

**Language/Version**: C# / .NET 10 backend; TypeScript / React 19 / Vite web (contract artifacts only).

**Primary Dependencies**: None added. Existing stack only: EF Core (Npgsql/PostgreSQL), xUnit, Testcontainers PostgreSQL, WireMock OMDB stubs.

**Storage**: PostgreSQL. Existing nullable text `poster_url` column; no migration or schema edit.

**Testing**: xUnit unit/integration suites; property tests for new domain behavior (no opt-out recorded); real-Postgres Testcontainers proofs via existing `PostgresFixture`/`PostgresCollection` (reused, never redefined); WireMock OMDB stubs for provider and end-to-end tests. No EF InMemory or mocked data-access driver for translation/serialization behavior.

**Target Platform**: Linux/server API plus Vite SPA contract consumers.

**Project Type**: Web API (vertical slice / feature-folder) plus web contract artifacts.

**Performance Goals**: No change; no new query shape beyond projecting one existing column.

**Constraints**: TreatWarningsAsErrors; no new NuGet/npm dependency; no new project/layer/port/handler/route; no schema/migration; no legacy Worker/Web/Test edits; no LocalPlay/secret/`Process.Start` change; web code consumes generated types only, never hand-rolled duplicates or hand-edited generated artifacts.

**Scale/Scope**: Seven production files plus compile-forced or AC-forced caller/test adjustments, per brief Q7.

## Constitution Check

GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.

- Ticket Scope and AC are authoritative (PRODUCT.md Authority Rules): all three scope items and every AC clause are delivered, nothing added or dropped, so no `blocked: structural` and no owner checkbox (brief Q7). [Constitution I Ports/Adapters :93-126 — no new port/layer/project, FR-011; II Explicit handlers :128-145 — no new endpoint/handler/status, FR-007; IV Enrichment state machine :169-194 — status/timestamps/attempts and retry preserved, T002 stop/reopen Q2 safeguard]
- Structural care list: the one public API addition (`posterUrl` on browse) is decided by the ticket AC (brief Q3); the Core `PosterUrl` member is forced by the shipped carrier (brief Q1); clearing operation adds no port/handler/route (brief Q2); Worker retention is the ticket-permitted alternative (brief Q4). No dependency, project, schema, LocalPlay, secret, or `Process.Start` change. [Constitution III Typed query model :147-167 — MovieSummary projection, ordering/filter/pagination unchanged; V Validated inputs/RFC7807 :196-216 — no route/status/input change; VII Configuration isolation/deterministic time :241-260 — no secret/machine-path/config/clock change, poster opaque string; VIII Ubiquitous language :262-280 — PosterUrl/poster_url/posterUrl local terms, optional member, no new domain concept or glossary edit; Enrichment Reliability Rules :443-459 — retry untouched]
- Contract chain C# DTO to `web/src/api/openapi.json` to generated TypeScript is honored; generation tooling is never introduced here (brief Q3). [Technology Stack Constraints :311-342 — Npgsql/PostgreSQL, committed OpenAPI; API and Contract Rules :461-473 — C#-derived paired OpenAPI/snapshot, required-nullable posterUrl, conditional approved TS generation; Documentation Rules :475-485]
- No gate, threshold, or cap is changed to achieve a pass (brief Q6). [Constitution VI Observability :218-239 — no telemetry change; IX Test pyramid with real infrastructure :282-309 — Testcontainers PostgreSQL and WireMock retained, EF InMemory/mocked-driver prohibition explicit; PR Quality Gates :346-376; Static-Analysis Gates :378-398; Coding Conventions :400-441 — no new comment/SmartEnum policy; no Phase B gate result claimed]

## Project Structure

### Documentation (this feature)

```text
specs/DEV-408/
  spec.md                # Feature specification (this plan's input)
  plan.md                # This file (/speckit-plan command output)
  tasks.md               # Phase 2 output (/speckit-tasks command output)
  brief.md               # Settled grill brief (decisions; authoritative with CONCLUSIONS.md)
  CONCLUSIONS.md         # Patron rulings Q1-Q7 (append-only; preserved)
```

Research, data-model, quickstart, and contracts/ side outputs are not produced: the brief already freezes approach, files, and ordering, and the ask writes only `spec.md`, `plan.md`, and `tasks.md`.

### Source Code (repository root)

```text
src/
  LamuFlix.Core/
    Domain/MovieMetadata.cs
    Domain/Movie.cs
    Ports/MovieSummary.cs
  LamuFlix.Infrastructure/
    Adapters/OmdbMetadataProvider.Mapping.cs
    Persistence/EfMovieRepository.cs
    Persistence/EfMovieCatalog.cs
  LamuFlix.Api/
    Endpoints/LibraryResponses.cs

tests/
  LamuFlix.UnitTests/              # focused domain unit/property tests (existing project)
  LamuFlix.IntegrationTests/       # EfMovieRepositoryTests, MetadataProviderLookupTests,
                                   # ApiEndToEndEnrichmentTests, ApiEndToEndBrowseTests,
                                   # ApiEndToEndImportTests, ApiEndToEndLibraryTests,
                                   # OpenApiContractTests, plus compile-forced
                                   # BrowseMoviesQueryHandlerTests, LibraryEndpointsTests,
                                   # ApiHostCompositionTests call sites

web/
  src/api/openapi.json             # regenerated from C# DTO, with Verify snapshot
```

**Structure Decision**: Existing layout is used unchanged. Production edits are limited to the seven files above; existing projection callers and tests receive only compile-forced or AC-forced adjustments with no handler behavior change. Generated scope is the OpenAPI document, its existing Verify snapshot, and TypeScript only through existing approved tooling when available. Excluded: schema/migrations, legacy Worker/Web/Test edits or deletion, packages, projects, flags, secrets, processes, UI, ADRs, glossary rewrites, and old feature-document edits (brief Q7).

## Design Overview

1. Core carrier: optional `string? PosterUrl` on `MovieMetadata` (added without breaking existing construction call sites unnecessarily), included in equality `State` and repository rehydration so poster-only changes save.
2. Provider mapping: deserialize OMDB `Poster`; trim present strings; map missing, null, blank/whitespace, and `N/A` to null; lookup succeeds without artwork; value stays opaque (no URI validation, download, or proxy).
3. Persistence: extend `ToRecord` / `CopyMetadata` / `ClearMetadata` / `ToDomain` to seven fields; null metadata clears all seven; null poster writes null (no fallback). Narrow `Movie.ClearMetadata()` nulls `Metadata` alone, preserves status/timestamps/attempts/unrelated state, is idempotent, and reaches the existing `ApplyMetadata` to `CopyMetadata(null)` to `ClearMetadata` branch via `SaveChangesAsync`. `MarkNotFound`/`MarkFailed`/retry semantics unchanged; no repository port method, handler, route, or layer added. Safeguard: if evidence establishes Enriched implies non-null Metadata, stop and reopen Q2 with bounded recon and Patron ruling before changing status semantics.
4. Browse: project `MovieRecord.PosterUrl` to Core `MovieSummary` to `MovieSummaryResponse` as always-present nullable `posterUrl` (explicit null when absent); item fields exactly `id`, `title`, `posterUrl`; details/metadata wire shapes unchanged.
5. Contracts: regenerate C#-derived OpenAPI and Verify snapshot via the existing opt-in `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` with `LAMUFLIX_REGENERATE_OPENAPI=true`, writing `web/src/api/openapi.json` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` together; verify `MovieSummaryResponse.posterUrl` is required AND nullable (brief:45/FR-006 always-present explicit-null contract). TypeScript only via existing approved tooling when available (required string-or-null property when generated). Update the old record-only preservation expectation for the poster alone; update exactly the two AC-forced browse-item field lists (`ApiEndToEndBrowseTests.cs:72`, `LibraryEndpointsTests.cs:365`); `ApiEndToEndImportTests`/`ApiEndToEndLibraryTests` details/metadata assertions stay unchanged, as do all other details/metadata wire assertions.
6. Proofs: real-Postgres repository/clearing/browse/API cases with fresh sibling-context reloads, plus a WireMock-plus-Postgres shipped enrichment-to-browse end-to-end; raw DbContext poster seeding is never the sole persistence proof and reflection/private-method bypass is never the clearing proof.
7. Retention: orphan `src/LamuFlix.Worker` poster code stays unchanged with the Q4 rationale; fallback semantics are not ported.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | No constitution departure identified by Patron (brief evidence limits) | — |
