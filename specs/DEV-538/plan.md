# Implementation Plan: For You page grouped by reason

**Branch**: `feature/538-spec` (worktree `F:/Dev/LamuFlix.worktrees/feature-538-spec`) | **Date**: 2026-11-21 | **Spec**: `specs/DEV-538/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q8); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-538`, `recon-DEV-538`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: PLAN FROZEN — Keel Phase 2 step 6. See [plan adjudication](PLAN_ADJUDICATION.md). Gate 1 remains closed pending the owner structural checkbox in `spec.md` and user merge of the Spec Kit PR. Build and runtime acceptance remain open.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `checklists/grouping.md`, `contracts/for-you-grouping-fields.md`, `DRAFTING_RECEIPT.md`. No `research.md` / `data-model.md` / `quickstart.md` — grill Q1–Q8 freeze inventable unknowns; no schema change; API shape documented under `contracts/`.

## Summary

Turn For You from one flat list into short reason-headed rows. Keep a single For You GET. Add owner-conditional `startedCollectionId` (per item) and `hasSeeds` (page envelope) so the client can build the series row and distinguish empty states 6a vs 6b. Group on the client with four seed-row / six card themed-row caps in one file; reuse `MovieCarousel` and hide the seed line on For You row cards; keep DEV-517 Load more / Swap on More for you only with themed rows frozen from the initial response. Regenerate the contract chain before web work; verify with backend, component, gate, and screenshot evidence under the Size L closing bar.

## Technical Context

**Language/Version**: C# / .NET 10 (`LamuFlix.Core`, `LamuFlix.Api`) + React 19 / TypeScript / Vite (`web/`) per `specs/PRODUCT.md` §1.

**Primary Dependencies**: Existing recommendation query/handler, For You page DTO, shared `RecommendedMovieDto`, `MovieCarousel`, `MovieCard`, DEV-517 For You paging/Swap hooks, OpenAPI → generated TypeScript chain. No new NuGet/npm dependency (`brief.md` Frozen scope; `PRODUCT.md` §5 item 1).

**Storage**: Existing movie collection membership and seed data already used by recommendations. No schema or migration change.

**Testing**: Existing xUnit recommendation unit/integration suites; Vitest + RTL for `ForYouPage`, `MovieCard`, `MovieCarousel`; web gates; desktop/phone screenshots as delivery evidence. No new test framework/dependency. Never mock the data-access driver for query-translation behaviour.

**Target Platform**: Local API + Compose SPA For You page (`/for-you`).

**Project Type**: Existing ports-and-adapters .NET API with feature-organised Core handlers; SPA consumes generated types only.

**Performance Goals**: One initial For You GET for grouping; no second request solely for series/seed-presence discovery. Preserve existing candidate set, ordering, scores, and paging semantics while computing the two additive values (`brief.md` Plan).

**Constraints**: Charter §2.3 care-list items; public API field-set change requires the owner checkbox amending Done when line 54. Contract chain C# DTO → OpenAPI → TypeScript. No LocalPlay/secrets/`Process.Start`. Honest gate labels; never lower thresholds. Size L requires Sentry, Ledger, and Compass. Closing bar: High/Medium and accepted in-scope Low closed with evidence; remaining Lows disposed; two review rounds; two fix commits per round (`brief.md` Q8).

**Scale/Scope**: Size L; For You response fields + client grouping UI + MovieCard opt-out + DEV-517 More for you integration + named tests/gates/screenshots. Parent DEV-490.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). Owner structural checkbox is present for the Done when line 54 amendment (`PRODUCT.md` §5 / brief Owner decision); not a silent assumption. This table cites **numbered principles only** (`constitution.md:962`); stack/API/workflow checks live in the following subsection.

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation; ArchitectureTests enforce (`constitution.md:511`) | **PASS (design)** — compute values in the existing handler. T001 must establish sufficient existing read paths before T004. The envelope adds no port or layer. |
| II (Explicit Handlers) | Handlers + AddHandler | **PASS** — extend existing For You query/handler and endpoint mapping; no MediatR. |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — read-only recommendation path; no Movie mutation. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — retain existing For You validation and error/Retry UX; no new error taxonomy. |
| VI (Observability) | Safe diagnostics | **PASS** — no new credential/logging surface; screenshots must not include secrets. |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **PASS** — no LocalPlay, secrets, or Process.Start change; caps are client presentation constants in one web file, not new options records. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS (design)** — spec-local vocabulary only; no CONTEXT.md expansion claimed. |
| IX (Test Pyramid) | Meaningful layers | **PASS** — unit/integration for response fields; component tests for grouping/a11y/empty states; screenshots for visual acceptance. |

### Stack / API / workflow checks (outside Constitution Check principle rows)

| Check | Plan response |
|---|---|
| Tech stack / API docs (OpenAPI chain) | **PASS (contingent on owner checkbox)** — additive For You fields flow C# → OpenAPI → generated TS; omit null `startedCollectionId` on the wire; Related stays free of the property when null (`brief.md` Q7; `PRODUCT.md` §5 item 4). |
| PR quality / ArchitectureTests (`constitution.md:511,773-775`) | Delivery PR **MUST** have `LamuFlix.ArchitectureTests` pass. |
| Static-analysis gates (`constitution.md:801-817`) | Every changed `.cs` task is complete only after Roslyn, cyclomatic complexity, and InspectCode pass on those files. Gauge owns T005a, the Phase 2 ArchitectureTests and three-static-gate checkpoint after T005 and before Phase 3; Phase 6 T014 re-runs them after Cog refactor (including complexity `-Threshold 6`) as part of the full matrix. |
| Cog / Gauge ownership (`task-pipeline:98-112`) | **Cog** owns `/refactor` after the build phases. **Gauge** owns the gate scripts: `scripts/run-roslyn-analyzers.ps1`, `scripts/run-cyclomatic-complexity.ps1`, `scripts/run-jetbrains-inspectcode.ps1` (no `-All`), `scripts/run-mutation.ps1` without `--since` whenever production C# under `src/` changed (script decides applicability: exit 0 = PASSED; exit 1 = FAILED blocking; exit 2 `NOT APPLICABLE` or `SKIPPED (scope-empty)` non-blocking, never PASS — do not pre-skip), `scripts/run-property-tests.ps1` (exit 2 accepted only when the task note records `propertyTests: opt-out — <reason>`), `scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, and `scripts/run-web-gates.ps1` (exit 0 if `/web` changed; exit 2 = `SKIPPED (scope-empty)` otherwise). Never fold non-pass into PASS; never lower thresholds. |
| Backend checkpoint before regeneration | Phase 2 US1 unit + HTTP tests **and** ArchitectureTests + the three static gates on Phase 2 changed `.cs` MUST pass before Phase 3 OpenAPI / TypeScript regeneration. |

### Complexity Tracking

No constitution violations requiring justification beyond the owner-checkbox API amendment already raised under `PRODUCT.md` §5. No new architectural layer.

## Approach

- **Phase A**: Quill drafts Spec Kit artifacts. Keel owns `speckit-analyze`, then the plan challenge (Sentry/Ledger/Compass) and adjudication. After plan challenge clearance and Patron `gate1: provisional`, Bernstein coordinates the Spec Kit PR; Rigger prepares/opens it; only the user merges after checking the owner checkbox; Keel records Gate 1 receipt. No Build before that receipt. Spec PR does **not** open before plan challenge / provisional clearance (`task-pipeline` Phase 2 steps 6–7).
- **Phase B pickup (after Gate 1)**: **Wisp** gathers pickup evidence for landed DEV-541 paging/exclusions, DEV-542 seed/evidence, DEV-543 strong-match selection, and DEV-517 Load more/Swap contracts against pickup HEAD, plus spec drift vs main. T001 records the exact existing watchlist-presence and watched-collection read paths and how T004 can reuse them without per-movie round trips. Verify DEV-542 best-seed movie ID and signal are machine-readable and that the landed title or label source supports both heading variants. Record DEV-517 follow-up lifecycle and Swap eligibility. **Keel** performs a read-only pickup analysis on that Wisp evidence before any production edits. Stop with `needs recon:` if a required contract or existing read path is missing. Obtain a cited Patron ruling before any change to the envelope (`brief.md` Plan step 1). Capture desktop and phone before screenshots after that analysis clears.
- **Task order (`brief.md` Q7 / Plan and file scope)**:
  1. Wisp evidence + Keel pickup analysis + before screenshots (desktop and phone).
  2. Use the existing read paths recorded by T001. Compute the fields in `GetForYouRecommendationsQueryHandler` and carry them through `ForYouRecommendationsPage` and `RecommendedMovie`. Qualify series by watched membership in the library independently of candidate eligibility and exclusions. Derive `HasSeeds` from liked/watched/watchlist presence before every empty return, including empty eligibility after exclusions, the ranking signal guard, and paging past the end. Preserve ranking and paging. Map both fields via `RecommendationEndpoints`, omitting null `StartedCollectionId` on the wire. Backend unit and HTTP tests pass before Gauge runs T005a. T005a must pass before regeneration.
  3. Regenerate OpenAPI snapshot → `web/src/api/openapi.json` → `web/src/api/types.ts` (+ MSW handlers via existing generate path) before web grouping work.
  4. Pure client grouping + single-file caps near `ForYouPage`; CSS/layout; `MovieCard` seed-line opt-out (default unchanged); `MovieCarousel` per populated row; empty states; DEV-517 More for you integration with frozen themed rows and one follow-up in flight at a time. Ignore follow-up responses from an obsolete page.
  5. Component/regression coverage per Done when; **Cog** `/refactor`; **Gauge** gate matrix; after screenshots; Size L Phase 4 (Gauge pre-pass → Sentry/Ledger/Compass → Keel adjudication → Cog remediate); Phase 5 (Rigger rebase → Gauge rebased-head gates → `/ship-review` → Quill PR body → Rigger PR publication → Keel merge-bar verification); attach and read back desktop/phone before-and-after pairs; execute or record disposition of SMOKE-001–003 (`task-pipeline:89-139`).
- **Do not**: change scoring/ranking/candidate pool; add schema/deps/layers; page themed rows; redesign home; claim Gate 1 or runtime acceptance from plan adjudication; lower gate thresholds; touch LocalPlay/secrets/`Process.Start`; open the Spec Kit PR before plan challenge / provisional clearance.

## Verification Approach

- Backend unit tests cover watchlist-only, playback-history-only, and watched membership despite exclusion or dislike, alongside each empty-return path. HTTP/integration: watched vs unwatched collection membership; no collection; populated `startedCollectionId`; omitted null `startedCollectionId` on the wire; `hasSeeds` true/false with empty page; seed presence unaffected by paging/exclusions; Related JSON free of `startedCollectionId` when null; required `hasSeeds` on successful responses. OpenAPI and generated-type checks: schema/type shape only (required boolean `hasSeeds`, optional nullable item `startedCollectionId`) — not Related omission.
- Web: Q3 precedence (series first); seed-group count/rank ties; liked vs watched signal split; retained API order in watchlist and More including folded overflow; empty-row omission; null/omitted release years and API-rank ties; duplicate initial IDs; best-seed uniqueness; folding/overflow; four/six caps; hidden seed lines with retained evidence; `h2`/named regions including zero-card More for you; carousel keyboard/controls; both empty messages with library links; loading/error/Retry; zero items with `hasMore` selects the empty state, while a nonempty initial page with an empty More for you bucket exposes Load more; Swap only when DEV-517 prerequisites hold; Load more and Swap empty-follow-up regressions; deduplication; rapid repeated or mixed actions with a pending response; suppression of obsolete responses after page reset; stable themed rows; one initial GET. Use existing Vitest/RTL example tests for grouping determinism.
- Gates: T005a schedules ArchitectureTests and three static gates on backend changed `.cs`, plus a read-path review, before regeneration (so Phase 2 completion does not depend on Phase 6); Phase 6 Cog refactor then Gauge full matrix (re-run static gates after refactor; run `run-mutation.ps1` whenever production C# under `src/` changed and let the script decide applicability; web gates, package scan, property-test opt-out rule); rebased-head re-run; Size L ship-review and merge-bar. Screenshots and smoke dispositions per Frozen scope / Q8; honest verdicts only.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-538/
├── brief.md
├── PLAN_ADJUDICATION.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── ticket-text.txt
├── spec.md
├── plan.md              # This file (also mirrored to private IMPL_PLAN)
├── tasks.md
├── contracts/
│   └── for-you-grouping-fields.md
├── DRAFTING_RECEIPT.md
└── checklists/
    ├── requirements.md
    └── grouping.md
```

No `research.md` / `data-model.md` / `quickstart.md` — grill decisions freeze Spec Kit research unknowns; no schema change; pickup + test plan live in brief/tasks.

### Source Code (authorized responsibility envelope — brief Plan; ticket Where it lands; recon)

```text
src/LamuFlix.Core/Features/Recommendations/
├── GetForYouRecommendationsQueryHandler.cs   # compute StartedCollectionId + HasSeeds
├── ForYouRecommendationsPage.cs            # carry HasSeeds
└── RecommendedMovie.cs                     # carry StartedCollectionId

src/LamuFlix.Api/Endpoints/
├── RecommendationEndpoints.cs                # map page + item fields (For You projection only for StartedCollectionId)
├── ForYouRecommendationsPageDto.cs           # add HasSeeds
└── RecommendedMovieDto.cs                    # add StartedCollectionId with WhenWritingNull

tests/LamuFlix.UnitTests/Features/Recommendations/
├── GetForYouRecommendationsQueryHandlerTests.cs
└── ForYouRecommendationsPageTests.cs

tests/LamuFlix.IntegrationTests/
├── RecommendationEndpointTests.cs
├── OpenApiContractTests.cs
└── Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json

web/src/
├── api/openapi.json                          # regenerated
├── api/types.ts                              # regenerated
├── mocks/handlers.ts                         # regenerated
├── mocks/fixtures.ts                         # hand-maintained For You fixtures
├── mocks/forYou.test.ts                      # MSW envelope regression
├── test/handlers.ts                          # hand-maintained test handlers
├── features/for-you/
│   ├── ForYouPage.tsx
│   ├── ForYouPage.css
│   ├── forYouApi.ts
│   ├── forYouApi.test.ts
│   ├── ForYouPage.test.tsx
│   └── (pure grouping + caps module colocated per brief)
├── library/MovieCard.tsx
├── library/MovieCard.test.tsx
└── ui/MovieCarousel.tsx                      # reuse; existing tests remain regression anchors
```

The envelope includes existing Core result constructor call sites and corresponding test builders, including Related, solely for compatibility with the new result members where required. T001 pins their paths on the landed head before edits. Other changes follow the existing envelope ruling process.

**Structure Decision**: Extend existing recommendation path and For You feature folders only. Caps and pure grouping stay next to the For You feature. Shared `MovieCard` gains an opt-out prop without changing default callers. No new project or architectural layer.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| _(none)_ | — | — |
