# Implementation Plan: DEV-388 Retire legacy Web and Worker hosts

**Branch**: `feature/DEV-388` | **Worktree**: `F:/Dev/LamuFlix.worktrees/DEV-388` | **Intake HEAD**: `48a94c38d2c0d7b0a59d967e66987b7607adda62` | **Date**: 2026-10-15 | **Spec**: `specs/DEV-388/spec.md` (provenance: merged from `feature/388-spec`)

**Input**: Feature specification from `specs/DEV-388/spec.md`, settled grill brief `specs/DEV-388/brief.md`, facts from `recon-DEV-388` and `recon-DEV-388-2`, Patron rulings in `specs/DEV-388/CONCLUSIONS.md`.

**Note**: Planning artifact only. This ask does not freeze the plan, open Gate 1, start implementation, clear a review, or authorize merge.

## Summary

Add metadata-only movie deletion on the existing movie port, Library feature, Infrastructure persistence, and Api movie endpoints, then generate the DELETE contract and a confirmed details action. After that parity exists, delete `src/LamuFlix.Web/` and `src/LamuFlix.Worker/`, and apply only the ruled test, project-reference, and README edits. Worker may go as soon as the named enrichment replacement evidence still holds. Web waits until delete and the existing P1 actions are mapped. No schema change, new layer, dependency, or LocalPlay change.

## Technical Context

**Language/Version**: C# / .NET 10 backend; TypeScript / React / Vite SPA for the details action and generated client only.

**Primary Dependencies**: None added. Existing stack only: ASP.NET Core minimal APIs, the existing handler pipeline, EF Core with Npgsql/PostgreSQL, xUnit, Shouldly, NSubstitute for handler tests, Testcontainers PostgreSQL, Verify for the OpenAPI snapshot, Vitest and React Testing Library for the SPA. This delete slice adds no validator.

**Storage**: PostgreSQL. Seven existing tables (`movies`, `actors`, `directors`, `genres`, `movie_actors`, `movie_directors`, `movie_genres`). Watchlist and metadata are columns on `movies`. Cascades on both foreign keys of the three join tables already remove movie-scoped join rows. No migration and no edit to `Initial`. Legacy MySQL cascade configuration is not claimed.

**Testing**: Handler tests in `LamuFlix.UnitTests`. PostgreSQL Testcontainers integration tests for persistence and the endpoint, including this endpoint's own invalid-id outcomes. Contract tests against the committed OpenAPI snapshot. Vitest/RTL for confirm, cancel, pending, non-404 error and retry, navigation, cache invalidation, and the DELETE 404 path (invalidate and return to the library, with no error and no retry). No EF InMemory. No mocked data-access driver for cascade behavior. No Gherkin lane.

**Target Platform**: Existing Api host plus the React SPA. Legacy Web and Worker hosts are removed, not replaced with new hosts.

**Project Type**: Web API (ports and adapters, feature-organised core) plus the existing `web/` SPA.

**Performance Goals**: No new target. One movie-row removal using cascades the schema already has.

**Constraints**: TreatWarningsAsErrors. No new NuGet or npm dependency. No new project, port, or layer. No schema or migration. No constitution edit. No solution, Docker, compose, or CI edit. No `harness.yml` change in this ticket's commits. No LocalPlay, secret, or `Process.Start` change. No new id rule, empty validator, id-specific 422, or authentication mechanism. The delete endpoint stays anonymous, matching GET. Web code consumes generated types only. Exact new type names follow existing Library conventions at implementation. Q3's external product-plan quotations stay Patron-cited.

**Scale/Scope**: One delete slice on existing seams, two host-tree deletions, the six ruled legacy test files plus the mixed portions of `EnrichmentTests.cs`, two project references, and three README lines.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Product authority: the ticket Scope and AC are authoritative (`specs/PRODUCT.md` Authority Rules). Delete stays because the AC names it. List-plus-filters is the existing P1 set under Patron Q3, which supersedes Q1 only on director and collection. No owner checkbox (brief Q9). [Constitution Governance :533-536 — the constitution owns HOW; the locked product plan owns WHAT, and that plan's P2 lines were not verified in recon roots; single fresh `Initial` :342]
- Ports and handlers: extend `IMovieRepository` and add one Library command and handler. Adding a member is not adding a port, so no port ADR is required. No new project or layer. Library does not reference Watchlist; the watchlist column leaves with the row. [Principle I :115-141; Principle II :149-162 — sealed command and handler, `NotFoundException` for absence, existing `AddHandler` decorator order]
- Filters and enrichment: browse query shape is unchanged. Director and collection predicates are not added. Enrichment behavior is unchanged; only the legacy Worker host and the Web publisher are removed after the Api replacement is confirmed. [Principle III :168-184; Principle IV and Enrichment Reliability Rules :465-481 — no second pipeline]
- Errors and contract: 204 with no body on removal; 404 problem details with `traceId` for absence and for an id less than or equal to zero, matching GET; 400 for a non-integer id, matching the existing route constraint. No id-specific 422, no empty validator, no new authentication, and no `BadRequest(ex.Message)`. The host stays anonymous. OpenAPI is regenerated and snapshot-tested; TypeScript is generated. [Principle V :217-234; API and Contract Rules :483-494]
- Time, secrets, language, tests: no clock, secret, or machine path is introduced. No new glossary term. Cascade proof uses Testcontainers PostgreSQL, not a mocked driver. [Principle VII :263-282; Principle VIII :284-302; Principle IX :304-331; Technology Stack Constraints :333-353]
- Documentation and debt: `docs/adr/0021-retire-legacy-web-and-worker-hosts.md` is formatted at spec stage. Its status is `Proposed (becomes Accepted when the DEV-388 Gate 1 spec PR merges)`. Later work may change only that status line to `Accepted`. README loses only the dead-host claims bounded recon-DEV-388 section 5 records at lines 10, 23, and 26. The constitution's historical follow-up list is left intact. Retiring Web and Worker is the acknowledged transitional-layout debt, not a new exception. [Documentation Rules :497-507; Known Technical Debt :509-517]
- Gates: configured thresholds stay. Labels stay literal. This plan claims no gate result. [Pull Request Quality Gates :368-398; Static-Analysis Gates :400-420]

Post-design re-check: the same gates hold. The DELETE shape, the file deletions, and the test list are the rulings already in the spec. No constitution departure and no unjustified violation.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-388/
├── spec.md              # /speckit-specify
├── plan.md              # This file (/speckit-plan)
├── tasks.md             # /speckit-tasks
├── checklists/          # specify quality checklist and checklist command
├── brief.md             # Settled grill brief
├── CONCLUSIONS.md       # Patron rulings Q1-Q9 (append-only)
└── ASSUMPTIONS.md       # Taste copy, including the spec-stage recommendation
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not separate files. The brief already freezes the approach, the contract, the entities, and the test order, and this plan's design overview records them. The ask's drafted set is `spec.md`, `plan.md`, and `tasks.md`. The agent-context edit to `CLAUDE.md` is not done: that file is outside the approved file boundary (brief Q5 and Q9).

### Source Code (repository root)

```text
src/LamuFlix.Core/Ports/                         # existing IMovieRepository member
src/LamuFlix.Core/Features/Library/              # delete command and handler
src/LamuFlix.Infrastructure/Persistence/         # atomic single-row delete
src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs   # DELETE /api/movies/{id:int}
tests/LamuFlix.UnitTests/Features/Library/       # handler outcome tests
tests/LamuFlix.IntegrationTests/                 # Testcontainers, endpoint, OpenApiContractTests
tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json
web/src/api/openapi.json
web/src/api/types.ts
web/src/features/movie-details/                  # confirmation, mutation, navigation
web/src/library/                                 # existing library and watchlist cache readers
src/LamuFlix.Web/                                # delete the tree
src/LamuFlix.Worker/                             # delete the tree
tests/LamuFlix.Test/                             # ruled files and project references only
README.md                                        # dead-host claims at lines 10, 23, 26; PR #175 overlap re-locates by content and stops for recon on drift
docs/adr/0021-retire-legacy-web-and-worker-hosts.md  # spec-stage format; delivery may change only the status line
```

**Structure Decision**: Existing layout. New delete code stays in the Library feature, the existing movie repository port, Infrastructure persistence, and `LibraryEndpoints`. Generated contract files are updated only by the existing tooling. Legacy host trees are removed in full. `tests/LamuFlix.Test` stays on disk and outside the solution.

## Design Overview

1. Port: add one delete member on `IMovieRepository` that reports removed versus absent. No new port.
2. Persistence: one atomic `movies` row removal. Absence is the not-removed result, not a preceding query. Join rows leave by the cascades in `Initial.cs:94-153` and the model snapshot. Shared principals and other movies stay. `library_path` is deleted with the row; the file at that path is not opened. No `ExecuteDelete` versus tracked-remove choice is frozen here beyond that atomic report. No migration.
3. Application: a sealed Library command and sealed handler, registered with the existing handler decorator helper (Tracing, Logging, Validation, handler). Absence becomes `NotFoundException`. Do not add an id rule, an empty validator, or an authentication mechanism. Watchlist feature code is not called.
4. HTTP: `DELETE /api/movies/{id:int}` beside the existing get-by-id route. 204 with an empty body on removal. 404 problem details on absence and on an id less than or equal to zero, matching GET. 400 when the id is not an integer, matching the existing route constraint. No request DTO. No id-specific 422. Anonymous, with no new auth mechanism. Delete tests verify those invalid-id outcomes on this endpoint.
5. Contract: regenerate `web/src/api/openapi.json` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` through the tooling `OpenApiContractTests` already uses. Do not assume a flag the brief and recon do not name. Generate `web/src/api/types.ts` with the existing generator. Do not hand-edit either generated file.
6. SPA: a destructive control on `web/src/features/movie-details/MovieDetailsView.tsx` using the assumed copy. Cancel sends nothing. Pending blocks a second submit. Both 204 and 404 invalidate the details cache key `['movieDetails', movieId]` from `web/src/features/movie-details/movieDetailsApi.ts` line 66 on the local QueryClient that view owns (`MovieDetailsView.tsx` lines 78-86), and invalidate browse and watchlist-filtered browse through `browseMoviesQueryKey` in `web/src/library/libraryQueries.ts` (standing key at line 18, browse and watchlist-filtered key at line 160, `browseMovies` prefix update at line 257, invalidation at line 262). Do not use the deleted `web/src/library/useBrowseMovies.ts` or a separate watchlist cache. Invalidating only an app-level key does not prove details invalidation. Both outcomes return to the library. A 404 shows no error banner and offers no retry. Any other failure stays on details with the assumed error and retry. No restyle.
7. Parity gate: before deleting Web, map list and the existing P1 filters, search, details, play, folder import, watchlist add, remove, and view to the endpoints and screens in recon sections 5–7, plus the new delete proof. Confirm import and enrichment replacements in that same section. Before retiring Worker, also confirm the Api-hosted `EnrichmentConsumer`, its registration and Api `Program.cs` wiring, and the existing proofs in `EnrichmentConsumerTests`, `ApiEndToEndEnrichmentTests`, and `RabbitMqServiceCollectionExtensionsTests` (bounded recon-DEV-388 section 3). Do not add a consumer. If a named replacement is missing, stop for recon.
8. Retirement: Worker tree, `WorkerTests.cs`, the four `OmdbMetadataProvider_*` tests and five `EnrichmentJobProcessor_*` tests bounded recon-DEV-388 section 2 names, their exclusively supporting Worker and Data declarations, and the Worker project reference may go once the enrichment evidence in step 7 holds. Keep the assembly fixture and unrelated tests. Web tree, `FilmesService_CriarFilme_InsertsAsPendingAndPublishesMessage`, the remaining exclusively supporting Web and Data declarations, and the Web project reference go only after step 7 and User Story 1. Remove only those two project references. README edits follow the dead-host sentences bounded recon-DEV-388 section 5 records at lines 10, 23, and 26. PR #175 overlaps those exact lines: if it merges first, re-locate the dead-host claims by that content, not by line number, and do not stack textual edits blindly. The edit stays inside the dead-host content boundary. Drift of that content stops for recon.
9. Inventories: recheck solution, Docker, compose, and CI for zero C# Web/Worker references; confirm `docker/Dockerfile.web` remains; confirm no `Pomelo`, `UseMySql`, or `LamuFlix.Data` under `src/` or `tests/`; keep `harness.yml` out of the commit. Do not edit the constitution. The delivery change to the ADR is only the status line, to `Accepted`.
10. Final evidence map, owned by polish: each ticket acceptance criterion and SC-001 through SC-007 is mapped to explicit evidence. An unmapped criterion blocks delivery. This adds no behavior.
11. Review limits, recorded for later stages and not executed here: closing bar is no open Critical, High, or Medium inside the frozen scope; two correction rounds; three-axis PR-posted adjudication; Low is one noted-no-ticket line; no seat merges.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | No constitution departure. Patron Q9 records no owner checkbox. | A new repository or a schema change was rejected by Q3 and Q6. |
