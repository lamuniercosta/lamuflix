# Implementation Plan: Explicit subtitle search and download (DEV-501)

**Branch**: `feature/501-spec` | **Date**: 2026-11-12 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/DEV-501/spec.md`; grill-closed `brief.md` Q1–Q9 + D1–D5 + Principle V clarification; `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Palette `design.md`; ADR 0041 (Proposed); `recon-DEV-501` (incl. 111–130).

## Summary

**PLAN FROZEN** — challenge round 1 repairs independently reread and consistency analysis closed by Keel; see `PLAN_ADJUDICATION.md`. Gate 1 is **not approved**. Runtime receipts remain **OPEN**.

Replace DEV-453 automatic prepare acquisition with free language search and explicitly selected download in the player CC Subtitles tab. Ship key-free `GET /api/subtitles/languages` from Core `SupportedSubtitleLanguages`. Reuse `ISubtitleProvider` (OpenSubtitles only). Bind opaque candidate IDs in concrete Singleton `SubtitleCandidateBinder` (no binder interface). Rank with concrete `SubtitleMatchScorer`. Publish validated SRT into Subs with streaming 2 MiB bound, leading-dot staging reuse, containment checkpoints, and configured publication-attempt ceiling. Surface truthful quota with TimeProvider Retry-After mapping and distinct search/download 429. Preserve Principle V: FluentValidation `ValidationException` → 422; malformed HTTP/domain/token prechecks outside that exception → 400; centralized `IExceptionHandler` + `AddProblemDetails()` + `traceId`; no global remap. Remove prepare-only artifacts per D4 after current-source verification. Post-Gate-1: constitution `1.15.0` → `1.16.0` with ADR 0041 acceptance; ADR 0038 Proposed prepare-wording relationship only.

## Technical Context

**Language/Version**: C# (net10.0, see `global.json`) + TypeScript React SPA (`web/`)

**Primary Dependencies**: ASP.NET Core minimal APIs, existing OpenSubtitles adapter, System.IO abstractions already used by `SubtitleSidecarStore`, FluentValidation (decorator → `ValidationException` → 422), openapi-fetch / MSW; media-chrome retained. **No new NuGet/npm packages.**

**Storage**: Filesystem Subs sidecars under authorized movie folders; no database/schema change; candidate bindings are process-local memory only.

**Testing**: xUnit v3 + NSubstitute + Shouldly (unit); FsCheck property tests for scorer/ranking (Category Property; mandatory applicable property gate); WebApplicationFactory / real filesystem fixtures; WireMock.NET provider HTTP fixtures; Vitest + RTL + MSW (web); Playwright smoke where SMOKE applies; home-cluster runtime OPEN.

**Target Platform**: Linux containers (Compose/home cluster) + Windows daily-driver library paths; `TimeProvider` mandatory; injected seeded `Random` for tokens (no crypto claims).

**Project Type**: Web application (ASP.NET Core API + React SPA)

**Performance Goals**: Interactive single-viewer search/download; no invented p95 SLA beyond truthful provider timeout mapping (504).

**Constraints**: PRODUCT Care list; Features:LocalPlay untouched; secrets never logged; Cache-Control private no-store on catalog/acquisition success and mapped errors; Q7 staging supersedes Q5 direct-create; never lower gate thresholds; recon:113–130 inventories incomplete — verify current paths/installer/snapshot before mutation.

**Scale/Scope**: Size L; Size L review requires Sentry/Ledger/Compass.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I Ports & adapters**: Extend existing `ISubtitleProvider` / sidecar store ports and Core `Features/Subtitles` handlers only; concrete `SubtitleCandidateBinder` / `SubtitleMatchScorer` are internal Core collaborators — **no new Hexagonal port or Principle I port-list amendment**.
- **II Feature handlers**: Sealed search query + download command with existing AddHandler / Tracing → Logging → Validation pipeline.
- **III Filters (typed query model)**: **N/A** — no Movie filter/query-model change.
- **IV Enrichment state machine**: **N/A** — no enrichment status/transition change.
- **V Errors**: Preserve Principle V unchanged. FluentValidation `ValidationException` → **422** `invalid_request` via single centralized `IExceptionHandler` + `AddProblemDetails()` with `traceId` on every ProblemDetails. Malformed HTTP/route/query/JSON binding and domain/token prechecks outside `ValidationException` → **400** (including `stale_candidate`). D1 reason codes and optional quota extension on acquisition ProblemDetails. No global remap; no `BadRequest(ex.Message)` / raw endpoint exceptions. Catalog parameterless unless a validator applies.
- **VI Telemetry**: Prefer existing subtitle/provider signals; preserve conventions when retiring prepare-only signals; no vendor names in Core telemetry identifiers.
- **VII Configuration / secrets / deterministic time**: Candidate TTL, publication attempts, download byte cap, and Retry-After→`resetAtUtc` use injected `TimeProvider` / typed `SubtitlesOptions`. Retain `SubtitlesOptions` / `SubtitleProviderOptions` at `Subtitles:OpenSubtitles`; keep ValidateOnStart exception so missing acquisition settings do not fail host startup or local serving. **Post-Gate-1** amend prepare-specific Principle VII wording to search/download acquisition validation; version **1.15.0 → 1.16.0** together with ADR 0041 acceptance (Sync Impact Report + Last Amended). Retain `SaveEnabled` and empty-preferences-valid. Secret isolation mandatory. Patron Q2: consistent maintenance, no owner structural checkbox. ADR 0038 remains Proposed; supersede prepare-route wording only.
- **VIII Language**: Add qualified terms to `CONTEXT.md` — Subtitle Candidate, Candidate ID, Local Subtitle Track, Subtitle Match Percent, Subtitle Download Availability — without rewriting Movie, Library, or exact-basename sidecar serving.
- **IX Testing**: Unit + property (scorer/ranking) + real filesystem + provider fixtures + API (incl. 400/422 split) + UI; no mocking driver translation (**N/A** — no new DB); mutants killed by focused tests.
- **LocalPlay**: No `Process.Start` or `Features:LocalPlay` registration changes.
- **Static-analysis gates**: Roslyn / complexity / inspectcode / **mandatory applicable property gate** / web gates on changed scope; honest exit labels.

**Post-design re-check**: No new layer/dependency/schema; prepare removal authorized by ticket + D4; constitution amendment scoped and post-Gate-1; III/IV remain N/A; Principle V preserved.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-501/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── design.md                  # Palette (D5 active-indicator repair owned there)
├── PLAN_ADJUDICATION.md       # Keel
├── spec.md / plan.md / tasks.md
├── research.md / data-model.md / quickstart.md
├── contracts/subtitle-search-download.md
├── checklists/
└── DRAFTING_RECEIPT.md
docs/adr/0041-explicit-subtitle-acquisition.md   # Proposed during planning; accept post-Gate-1 with constitution bump
docs/adr/0038-subtitle-provider-and-sidecar-boundary.md  # remains Proposed; prepare-route wording relationship only
```

### Source Code — exact planned new / changed filenames

```text
src/LamuFlix.Core/
├── Features/Subtitles/
│   ├── SearchSubtitlesQuery.cs
│   ├── SearchSubtitlesQueryHandler.cs
│   ├── SearchSubtitlesQueryValidator.cs
│   ├── SearchSubtitlesResult.cs
│   ├── DownloadSubtitleCommand.cs
│   ├── DownloadSubtitleCommandHandler.cs
│   ├── DownloadSubtitleCommandValidator.cs
│   ├── DownloadSubtitleResult.cs
│   ├── SubtitleMatchScorer.cs                    # NEW concrete; no "helper equivalent"
│   ├── SubtitleCandidateBinder.cs                # NEW concrete Singleton; NO ISubtitleCandidateBinder
│   ├── SupportedSubtitleLanguages.cs             # NEW single catalog
│   ├── GetSubtitleLanguagesQuery.cs              # NEW parameterless catalog query
│   ├── GetSubtitleLanguagesQueryHandler.cs       # NEW
│   ├── GetSubtitleLanguagesResult.cs             # NEW
│   ├── PrepareSubtitleCommand.cs                 # SWEEP CANDIDATE only — classify via T025/T026
│                                                 # current verification; NOT on confirmed D4 delete list
│   ├── PrepareSubtitleCommandHandler.cs          # D4 DELETE after current-path verify
│   └── (existing list/content/sidecar handlers retained)
├── Options/SubtitlesOptions.cs                   # ADD CandidateTokenTtl, CandidateStoreMaxEntries,
│                                                 # MaxSubtitleDownloadBytes, MaxSubtitlePublicationAttempts
└── Ports/
    ├── ISubtitleProvider.cs                      # reuse
    ├── SubtitleCandidate.cs                      # EXTEND release-name/quota metadata (provider-neutral)
    └── ISubtitleSidecarStore.cs                  # EXTEND Subs publish / staging exclusion as required

src/LamuFlix.Infrastructure/
├── Adapters/OpenSubtitlesSubtitleProvider.cs     # EXTEND metadata/quota/Retry-After mapping; streaming body cap
└── FileSystem/SubtitleSidecarStore.cs (+ discovery)
    # EXTEND Subs naming, leading-dot staging reuse, containment checkpoints, exclusion tests

src/LamuFlix.Api/
├── Endpoints/SubtitleEndpoints.cs                # ADD languages/search/download; REMOVE prepare
├── Endpoints/SubtitlePrepareResponse.cs          # D4 DELETE after verify
├── Endpoints/SubtitleLanguagesResponse.cs        # NEW
├── Endpoints/SubtitleLanguageOptionDto.cs        # NEW
├── Endpoints/SubtitleSearchResponse.cs           # NEW
├── Endpoints/SubtitleDownloadRequest.cs          # NEW
├── Endpoints/SubtitleDownloadResponse.cs         # NEW
└── HandlerRegistration.cs                        # register new handlers; drop prepare; register Singleton binder

tests/ (pin new names; verify existing paths at pickup — recon lists are incomplete)
├── LamuFlix.UnitTests/Features/Subtitles/
│   ├── SubtitleMatchScorerTests.cs               # NEW
│   ├── SubtitleMatchScorerPropertyTests.cs       # NEW (bounds, hash override, ordering, overlap symmetry/empty)
│   ├── SubtitleCandidateBinderTests.cs           # NEW
│   ├── SupportedSubtitleLanguagesTests.cs        # NEW / with catalog preference ordering
│   ├── SearchSubtitlesQueryHandlerTests.cs       # NEW
│   ├── DownloadSubtitleCommandHandlerTests.cs    # NEW
│   └── PrepareSubtitleCommandHandlerTests.cs     # DELETE after verify
├── LamuFlix.UnitTests/FileSystem/SubtitleSidecarStoreTests.cs          # EXTEND (verify path)
├── LamuFlix.IntegrationTests/
│   ├── SubtitleSearchDownloadEndpointTests.cs    # NEW (GET/POST matrix incl. 400/422/429 split, no-store, cancel≠timeout)
│   ├── SubtitlePrepareServeEndpointTests.cs      # DELETE after verify
│   ├── SubtitleEndpointTests.cs                  # UPDATE shared only (verify path)
│   ├── OpenSubtitlesSubtitleProviderTests.cs     # EXTEND (verify path)
│   └── SubtitleSidecarStoreConcurrencyTests.cs   # EXTEND (verify path)

web/src/
├── features/playback/BrowserPlayer.tsx / SubtitleSelector.tsx  # after dependency pickup
├── api/openapi.json + types.ts                   # REGENERATE only (final after prepare removal)
└── mocks/handlers.ts                             # update via generate conventions

.specify/memory/constitution.md                   # post-Gate-1 Principle VII 1.15.0→1.16.0 + ADR 0041 accept
CONTEXT.md                                        # additive qualified glossary only
harness.yml                                       # remove prepare-only killFileSpanExempt entry after verify;
                                                  # preserve pre-existing dirty edit; regenerate via discovered installer
```

**Structure Decision**: Stay inside existing Api / Core / Infrastructure / web feature folders. Exact planned `.cs` filenames above replace prior “or equivalent” / `+Handler` shorthand. T003/T004/T006/T025/T026 must verify current existence/shared usage/installer/snapshot workflow before modifying because recon:116/119/125/128 explicitly lacks complete inventories; missing facts → gated pickup `needs recon`, not product guesses. Installer/snapshot commands are pickup discoveries (recon:128 did not establish `scripts/install.ps1`) — do not invent paths.

## Phase 0 — Research findings

See [research.md](./research.md).

## Phase 1 — Design & contracts

See [data-model.md](./data-model.md), [contracts/subtitle-search-download.md](./contracts/subtitle-search-download.md), [quickstart.md](./quickstart.md).

## Approach (implementation ordering)

1. **Owner Gate 1 (prerequisite; not claimed here)**: No implementation until Gate 1 is recorded. Plan freeze is recorded in `PLAN_ADJUDICATION.md` and does not grant owner Gate 1 or implementation approval.
2. **Phase B pickup**: After Gate 1, verify CC panel/Subs discovery readiness vs current main; executable read-only `speckit-analyze` drift receipt; amend tasks only where authorized. Treat recon:116/119/125/128 incompleteness as gated pickup checks, not product blockers.
3. **Post-Gate-1 governance (T005 before feature code)**: Principle VII `1.15.0` → `1.16.0` + ADR 0041 acceptance together; ADR 0038 Proposed prepare-wording relationship only; additive `CONTEXT.md` glossary.
4. **Serialize shared options/validators/endpoints**, then catalog + binder + scorer: typed `SubtitlesOptions` bounds first; then `SupportedSubtitleLanguages` / exact catalog query files / languages route; Singleton `SubtitleCandidateBinder`; `SubtitleMatchScorer` + property tests on disjoint files once shared edits settle.
5. **Provider metadata + quota**: Retry-After via TimeProvider; streaming body cap (coordinate shared options with step 4).
6. **Search query**: Canonical normalization; empty preferences valid; 400/422 split via centralized handler.
7. **Guarded Subs store**: Containment checkpoints; staging reuse; publication-attempt ceiling; cancellation cleanup.
8. **Download command**: POST precheck order movie → binding → read-only → config → containment → known-zero 429; then paid download.
9. **Prepare removal then final backend contracts (T025–T027)**: After US2 shared endpoint/registration edits; verify/classify current sources (D4 delete list + sweep candidates such as `PrepareSubtitleCommand.cs`); coordinate dirty harness; discover installer/snapshot workflow at pickup; regenerate harness outputs; final OpenAPI/TS regeneration and snapshot review/approval — no concurrent shared-file edits; never hand-edit generated artifacts.
10. **Generator/client validation then UI (T022–T024)**: After T027; web-implement per `design.md` (Palette D5); generated clients only.
11. **Gates + Size L review**: Including mandatory applicable property gate; honest labels.
12. **Runtime receipts**: Home cluster after prerequisite; `Could not run` when blocked.

## Test Strategy

| Layer | Proves |
|---|---|
| Unit | Hash 100; Jaccard/empty/ties/midpoint; binder expiry/cap/FIFO eviction/restart/cross-movie; language/catalog normalization; empty preferences valid; no automatic paid retries |
| Property (mandatory applicable) | Scorer/ranking: bounds, hash override, deterministic ordering, overlap symmetry/empty — `SubtitleMatchScorerPropertyTests.cs`; run `./scripts/run-property-tests.ps1` |
| Provider HTTP fixtures | Metadata, selected download, quota/reset/Retry-After, missing data, failure, timeout, streaming oversize |
| Real filesystem | Escape/symlink/reparse refusal at create/pre-stage/pre-publication; malformed SRT before staging; Subs creation; collision `(N)` with staged reuse; concurrent no-overwrite; staging discovery exclusion; attempt ceiling cleanup; cancellation cleanup |
| API | Catalog 200; GET/POST envelopes; POST precheck order; known-zero 429; search vs download 429; 400 vs 422; private no-store on success+errors; cancel≠timeout; prepare 404; local serving when acquisition config invalid |
| Vitest/RTL | Ordering; bilingual labels; language default/change; stale discard; session activation; D5 active-check local-only; distinct 429 copy; read-only Get disabled |
| Runtime OPEN | Configured-key pt-BR; BS Player recognition; quota/timeout/read-only on home cluster |

## D4 delete / update / retain (authorized surface)

| Disposition | Planned surface |
|---|---|
| Delete (after current-path verify) | `PrepareSubtitleCommandHandler.cs`, `SubtitlePrepareResponse.cs`, `PrepareSubtitleCommandHandlerTests.cs`, `SubtitlePrepareServeEndpointTests.cs` (older recon:86) |
| Update shared only | `SubtitleEndpoints.cs`, `HandlerRegistration.cs`, `harness.yml` preserving dirty edits, OpenAPI contract tests/snapshots, regenerated `web/src/api/openapi.json` / `types.ts`, `web/src/mocks/handlers.ts`; additional live references require T026 sweep |
| Retain | `ISubtitleProvider`, `OpenSubtitlesSubtitleProvider`, `SubtitleSidecarStore`, `SubtitleSidecarDiscovery`, `ListSubtitleTracksQueryHandler`, `GetSubtitleSidecarQueryHandler`, `SubtitlesOptions` (incl. SaveEnabled, PreferredLanguages + new typed bounds), `SubtitleProviderOptions` |
| Glossary add | `CONTEXT.md` qualified terms listed above |
| Post-Gate-1 | constitution 1.15.0→1.16.0 + ADR 0041 acceptance; ADR 0038 prepare-route wording relationship only |

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none anticipated) | — | Reuse existing ports/store/provider. Measured complexity gates; do not invent types solely for predicted scores. |
