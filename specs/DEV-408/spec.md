# Feature Specification: DEV-408 Persist enrichment poster URL so library browse tiles show posters

**Feature Branch**: `feature/408-spec`

**Created**: 2026-10-12

**Status**: gate1: provisional

**Input**: Ticket DEV-408 (bug, size M, ui none, parent DEV-286); settled grill brief `specs/DEV-408/brief.md` (grill closed 7/12); facts from `recon-DEV-408`; Patron rulings in `specs/DEV-408/CONCLUSIONS.md`. No decisions beyond `brief.md`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse tiles show enrichment posters (Priority: P1)

A user who enriched their library opens the library browse list and sees a poster image on each enriched tile.

**Why this priority**: This is the ticket's user-visible outcome: after a successful enrichment that returns a poster, `GET /api/movies` returns it as `posterUrl` so tiles can show posters.

**Independent Test**: Enrich a movie against a WireMock OMDB stub that returns a poster, then `GET /api/movies` and confirm the item carries the poster value.

**Acceptance Scenarios**:

1. **Given** an OMDB stub response containing a `Poster` value, **When** shipped enrichment succeeds, **Then** `poster_url` is populated and `GET /api/movies` returns it as `posterUrl`.
2. **Given** a stored non-null poster, **When** `GET /api/movies` is called, **Then** the item shape is exactly `id`, `title`, `posterUrl` with the stored value.
3. **Given** a movie with no poster, **When** `GET /api/movies` is called, **Then** `posterUrl` is present as an explicit JSON null.

---

### User Story 2 - Clearing metadata clears the poster (Priority: P2)

A metadata clear removes the poster along with the other metadata fields, so a cleared movie shows no stale artwork.

**Why this priority**: The ticket AC requires clearing metadata to clear `poster_url`, proven by a real-database test.

**Independent Test**: Persist metadata with a non-null poster through the supported enrichment/repository path, invoke the narrow `Movie.ClearMetadata()` intent operation, save, and reload through a fresh context.

**Acceptance Scenarios**:

1. **Given** persisted metadata with a non-null poster, **When** metadata is cleared via `Movie.ClearMetadata()` and saved, **Then** all seven metadata fields including `poster_url` are null on reload and unrelated fields are unchanged.
2. **Given** persisted metadata with a non-null poster, **When** a re-enrichment returns no poster, **Then** the stored poster is replaced with null (snapshot semantics, no old-poster fallback).

---

### User Story 3 - Orphan Worker poster code disposition recorded (Priority: P3)

A reader of the spec understands why `src/LamuFlix.Worker` poster code is retained unchanged.

**Why this priority**: Ticket Scope item 3 explicitly requires deciding the fate of the orphaned Worker poster code (delete it or record why it stays).

**Independent Test**: Inspect the spec and confirm it records retention with its basis and states the snapshot rule governs instead of the Worker `?? movie.Poster` fallback.

**Acceptance Scenarios**:

1. **Given** the shipped path carries posters, **When** the Worker poster code is reviewed, **Then** it remains unchanged because it is outside the solution, has no ProjectReference, and refers to absent `LamuFlix.Data` namespaces, so it cannot satisfy the AC.

### Edge Cases

- OMDB `Poster` missing, JSON null, blank/whitespace-only, or `N/A` maps to null; lookup still succeeds without artwork.
- A present `Poster` string is trimmed before storage.
- A null incoming poster overwrites an older stored poster with null (metadata snapshot replaces previous metadata, including artwork).
- Repeated `ClearMetadata()` is idempotent and preserves enrichment status, timestamps, attempts, and all unrelated state.
- Poster-only metadata change is detected as a change (poster participates in equality `State` and rehydration) so it is saved.
- The poster value is an opaque provider string: no URI validation, download, proxy, or new dependency.
- If later evidence establishes an Enriched-implies-non-null-Metadata invariant, work stops and Q2 reopens with bounded recon/decision; status semantics are not silently changed.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Shipped Infrastructure OMDB provider MUST deserialize the `Poster` response field and map present values (trimmed) through Core `MovieMetadata.PosterUrl`.
- **FR-002**: Shipped OMDB mapping MUST convert missing, null, blank/whitespace, and `N/A` poster values to null without failing the lookup.
- **FR-003**: Core `MovieMetadata` MUST carry an optional `string? PosterUrl`, included in equality `State` and repository rehydration.
- **FR-004**: Repository plumbing (`ToRecord` / `CopyMetadata` / `ClearMetadata` / `ToDomain`) MUST write all seven metadata fields on non-null metadata and clear all seven on null metadata; a null incoming poster writes null, never a fallback.
- **FR-005**: Domain MUST expose a narrow `Movie.ClearMetadata()` intent operation that nulls `Metadata` alone, preserving enrichment status, timestamps, attempts, and unrelated state; idempotent on repeat.
- **FR-006**: Browse projection MUST return exactly `id`, `title`, `posterUrl` per item (`MovieRecord.PosterUrl` to Core `MovieSummary` to `MovieSummaryResponse`), with `posterUrl` always present (explicit null when absent).
- **FR-007**: System MUST NOT add a poster field to `MovieMetadataResponse` or `MovieDetailsResponse`, and MUST NOT change browse route, status codes, filtering, pagination, or ordering.
- **FR-008**: `src/LamuFlix.Worker` poster code MUST remain unchanged; the shipped snapshot replacement semantics govern, not the Worker `?? movie.Poster` fallback.
- **FR-009**: Generated contract MUST be regenerated from the C# DTO (`web/src/api/openapi.json` plus the `OpenApiContractTests` Verify snapshot); `web/src/api/types.ts` MUST come only from existing approved generation tooling, never hand-edited or introduced in this ticket.
- **FR-010**: Real-PostgreSQL (Testcontainers) tests MUST prove persistence (insert, poster update, poster-only replacement, null replacement, preserve-on-unrelated-update) and clearing (non-null poster persisted via the supported path, then `ClearMetadata()` plus `SaveChangesAsync`, verified via a fresh sibling context), plus the WireMock-plus-Postgres enrichment-to-browse end-to-end.
- **FR-011**: System MUST NOT add a dependency, project, layer, port, handler, route, migration, feature flag, secret, process launch, UI feature, ADR, or shared-glossary change in this ticket.

### Key Entities

- **MovieMetadata**: Core value object; gains optional `PosterUrl` (opaque provider string, null when absent).
- **MovieRecord.poster_url**: Existing nullable text column; written/cleared with the other six metadata fields, never independently.
- **MovieSummary / MovieSummaryResponse**: Browse projection item; exactly `id`, `title`, `posterUrl` with `posterUrl` always present (explicit null when absent).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After a successful shipped enrichment returning a poster, `poster_url` is populated and `GET /api/movies` returns it as `posterUrl`.
- **SC-002**: Clearing metadata via `Movie.ClearMetadata()` clears `poster_url` alongside the other six fields, proven against a real database with a fresh-context reload.
- **SC-003**: Browse items carry exactly `id`, `title`, `posterUrl` with explicit null when absent; details shapes are unchanged.
- **SC-004**: Orphan Worker poster code is retained unchanged with the recorded rationale, and no legacy Worker/Web/Test file is edited or deleted.

## Assumptions

- None. The grill closed with no taste assumptions and no owner checkbox (brief Q7). Anything not decided in `brief.md` is routed as `needs decision:`, never assumed here.
