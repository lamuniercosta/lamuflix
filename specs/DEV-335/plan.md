# Implementation Plan: DEV-335 Add IMDb rating range filter

**Branch**: `feature/335-spec` | **Date**: 2026-10-26 | **Spec**: [spec.md](spec.md)

**Input**: `spec.md`; closed grill `brief.md` (Q1–Q8) including Plan challenge Round 1 Patron amendment; `CONCLUSIONS.md`; `ASSUMPTIONS.md`; notes `DEV-335` and `recon-DEV-335`; `specs/PRODUCT.md`; `.specify/memory/constitution.md`. Decisions only from brief Q1–Q8, the Round 1 amendment, and the rulings they rest on. No Gate 1, plan freeze, or implementation clearance is claimed by this plan.

**Identity**: Worktree `F:/Dev/LamuFlix.worktrees/feature-335-spec`; branch `feature/335-spec`; recon HEAD `52e443666b5e767edc93befc20eb55de2e77787b`; size M; tag `ui:`.

## Summary

Add typed `RatingRange(decimal? Min, decimal? Max)` on `MovieQuery.Rating` with inclusive 0.0–10.0 bounds (at most one decimal), paired canonical wire keys `minRating`/`maxRating`, layered HTTP 422 validation versus defensive empty catalog results, one `WhereRating` predicate chained in `EfMovieCatalog` before count/paging, regenerated OpenAPI → TypeScript/MSW contract chain, and live Filter Panel DualRangeField behavior that updates URL, API params, grid, and count. Dependencies DEV-322, DEV-297, and DEV-302 must already be on the authorized Phase B base; their specs being present is not current-main landing.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`net10.0`) backend; TypeScript strict + React 19 + Vite frontend (`web/`).

**Primary Dependencies**: Existing stack only (EF Core/Npgsql, FluentValidation, xUnit v3, FsCheck, Testcontainers, Verify, Vitest/RTL/MSW, OpenAPI → `openapi-typescript` / generate:api). **No new NuGet or npm dependency** (brief Q4/Q8; FR-011).

**Storage**: Existing PostgreSQL `imdb_rating` (`numeric(3,1)`); **no schema change or migration**. Unrated = null `ImdbRating`.

**Testing**: Existing xUnit/FsCheck unit + property suites; real Testcontainers PostgreSQL integration; Vitest for live web paths. No new test framework; no data-driver mocks for translated rating predicates; no redundant test-only codec work on `libraryUrl.ts` (brief Q6).

**Target Platform**: Local Api host + Vite SPA (same as repo).

**Project Type**: Existing `LamuFlix.Core` / `Infrastructure` / `Api` + `web/` feature slice. No new project, top-level folder, or architectural layer.

**Performance Goals**: Reuse existing browse/page/count path; no new SLA invented.

**Constraints**: Frozen file envelope (below); no sort/order change; no LocalPlay/secrets/`Process.Start`; no hand-edits to generated contract files; no threshold lowering; comment policy AAA/suppressions only; `TimeProvider` / banned `DateTime.Now` unchanged.

**Scale/Scope**: Size M. Frozen delivery = Q1–Q7 model, query/validation/predicate, paired API/URL keys, generated contract chain, live FilterPanel/LibraryView behavior, and enumerated Q6 proof only (brief Q8 loop discipline).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design. Cite principle numbers. Gate 1 / plan freeze remain separate and are not claimed here.*

- **I. Ports and Adapters** — **PASS**. Extends existing `IMovieCatalog` / `EfMovieCatalog` browse path and Core `Library` types. No new port, project, or layer (Q5/Q8).
- **II. Explicit Handlers and Decorators** — **PASS**. No MediatR; no new handler bus. Rating validation rides existing FluentValidation / endpoint validation path (Q3/Q5).
- **III. Filters Are a Typed Query Model** — **PASS**. Adds typed `RatingRange` / `MovieQuery.Rating`; one small `WhereRating` predicate (decimal private helpers only if required; no generic range framework); no reflection or string-named sorts. Equality/hash/`HasSameCoreValues` include Rating. Canonical keys preserve ticket spelling (Q1/Q5). Existing MovieQueryString property round-trip infrastructure is extended, not replaced. No new shared parser layer/file.
- **V. Validated Inputs, Consistent Error Responses** — **PASS**. HTTP returns 422 for malformed, duplicate, out-of-domain, excess-precision, inverted, or partial (single-key) rating input under the Round 1 strict ASCII unsigned lexical rule in both parse paths; no silent rounding/clamping. Numeric domain via public `ImdbRating.TryCreate`. Distinct defensive empty result for invalid direct catalog calls (Q3 + Round 1 amendment).
- **VII. Configuration Isolation and Deterministic Time** — **PASS**. No secrets, machine paths, new options, LocalPlay, or `Process.Start`. No new wall-clock dependency for the slider (Q4/Q8).
- **IX. Test Pyramid with Real Infrastructure** — **PASS**. Q6 uses existing xUnit/FsCheck, Testcontainers PostgreSQL (no EF InMemory / driver mocks for rating predicates; existing suite translation posture; no invented client-evaluation harness switches), and Vitest. Conditional rating-specific survivor extensions only in the three recon-named survivor suites when mutation requires them. Mutation and other gates use current `harness.yml` thresholds without lowering and without literal threshold overrides (Q6/Q7 + Round 1).
- **API and Contract Rules** — **PASS**. Additive `BrowseMoviesRequest` string `MinRating`/`MaxRating` documenting canonical `minRating`/`maxRating`; no rename of existing parameters; no new DTO. OpenAPI/generated names stay PascalCase; raw wire keys stay camelCase. Contract chain: C# request/endpoint → committed `web/src/api/openapi.json` + Verify snapshot → generated `types.ts` / MSW via `LAMUFLIX_REGENERATE_OPENAPI=true` then `npm run generate:api`. Never hand-edit generated files; exact parameter-list equality retained; web consumes generated types only (Q5 + Round 1).
- **Technology Stack Constraints** — **PASS**. No package swap; DualRangeField reuse with native inputs and existing CSS (Q4).
- **Static-Analysis / PR gates** — **Planned for Phase B only** — Roslyn, complexity, InspectCode, property tests, vulnerable packages, format, `dotnet test`, pre-PR mutation; applicable web tests/lint/typecheck/build/API drift. Scripts read `harness.yml` (do not pass literal threshold overrides). Report exit 2 as SKIPPED (scope-empty) / SKIP / mutation N/A; Could not run is not PASS (Q7). Not claimed PASS in Phase 2 drafting.
- **§2.3 care list** — No new dependency; no new project/layer; no schema beyond ticket tables (none); public API shape is the ticket-named paired keys already in brief/spec; no LocalPlay/secrets; only the approved file envelope below; no `ImdbRating.cs` edit.

**Re-check after design**: Same PASS set. No Phase 0/1 research/contracts artifacts are required (see Documentation omissions). No constitution departure or owner checkbox (brief Q8).

## Project Structure

### Documentation (this feature)

```text
specs/DEV-335/
├── brief.md                 # Grill closed Q8/12 (Keel)
├── CONCLUSIONS.md           # Q1–Q8 rulings
├── ASSUMPTIONS.md           # Q4 taste [assumed]
├── spec.md                  # Feature specification
├── plan.md                  # This file
├── tasks.md                 # Task list (27 tasks; 11 FR / 6 SC)
├── checklists/requirements.md
└── DRAFTING_RECEIPT.md
```

**Omitted Spec Kit Phase 0/1 artifacts** (intentional; not deferred work):

- **`research.md`**: Not written. Brief Q1–Q8 plus `recon-DEV-335` already fix approach, bounds, keys, layers, and out-of-scope boundaries; inventing research would risk new decisions.
- **`data-model.md`**: Not written. No new entity or migration; `RatingRange` / `MovieQuery.Rating` and existing nullable `ImdbRating` are fully specified in brief Q1–Q2 and the Planned file envelope.
- **`contracts/`**: Not written. Public wire shape is additive parameters on existing `GET /api/movies`, owned by the regenerated OpenAPI baseline/snapshot and generated TS/MSW — not a separate hand-authored contract file.
- **`quickstart.md`**: Not written. Regeneration and gate commands are named in brief Q5/Q7 and tasks Phase 6; no new runbook is required for this filter delta.
- **`design.md`**: Palette-owned prerequisite before plan challenge (brief Q4/Q7); not authored by Quill.

### Source Code (repository root) — approved file envelope (brief Planned file envelope + Q6)

**Production (Q1–Q5):**

```text
src/LamuFlix.Core/Library/RatingRange.cs                 # new: RatingRange(decimal? Min, decimal? Max)
src/LamuFlix.Core/Library/MovieQuery.cs                  # edit: Rating + equality/hash/HasSameCoreValues
src/LamuFlix.Core/Library/MovieQueryString.cs            # edit: minRating/maxRating codec
src/LamuFlix.Infrastructure/Library/MovieQueryValidator.cs
src/LamuFlix.Infrastructure/Persistence/MovieRecordQueryExtensions.cs  # WhereRating
src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs              # chain before count/paging
src/LamuFlix.Api/Endpoints/BrowseMoviesRequest.cs        # MinRating/MaxRating strings
src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs           # decimal parse / 422 / map to Rating
web/src/features/library/filterQuery.ts                  # live URL codec only
web/src/features/library/FilterPanel.tsx                 # DualRangeField after Release year
web/src/library/browseParams.ts
web/src/library/LibraryView.tsx                          # active-group count
web/src/api/openapi.json                                 # regenerated, never hand-edited
web/src/api/types.ts                                     # regenerated
web/src/mocks/handlers.ts                                # regenerated
```

**Tests (Q6; locations from recon 3.1–3.3 / tasks.md — do not invent paths):**

```text
tests/LamuFlix.UnitTests/Library/MovieQueryTests.cs
tests/LamuFlix.UnitTests/Library/MovieQueryStringTests.cs
tests/LamuFlix.UnitTests/Library/MovieQueryValidatorTests.cs
tests/LamuFlix.UnitTests/Library/MovieQueryFixture.cs
tests/LamuFlix.UnitTests/Library/MovieQuerySurvivorTests.cs              # conditional rating-specific only if mutation requires
tests/LamuFlix.UnitTests/Library/MovieQueryStringSurvivorTests.cs        # conditional rating-specific only if mutation requires
tests/LamuFlix.IntegrationTests/EfMovieCatalogBrowseTests.cs
tests/LamuFlix.IntegrationTests/EfMovieCatalogSurvivorTests.cs           # conditional rating-specific only if mutation requires
tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs
tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs
tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json
web/src/features/library/FilterPanel.test.tsx
web/src/features/library/filterQuery.test.ts
web/src/library/LibraryView.composed.test.tsx
```

**Structure Decision**: Extend the existing YearRange-style filter path across Core → Infrastructure → Api → live web codec/panel. No new abstraction, DI registration sweep, CSS package, or parallel `libraryUrl.ts` work.

**Explicitly out of envelope**: `ImdbRating.cs` / storage mapping/migrations; dependency manifests; sorting; CSS/slider packages; `libraryUrl.ts`; `libraryQueries.ts`; playback/secrets/`Process.Start`; unrelated rewrites; ADRs/`CONTEXT.md`; unrelated survivor-suite sweep (brief Q8 + Round 1).

## Design (from brief Q1–Q5 + Round 1 amendment)

### 1. Typed range and presence (Q1–Q2 + Round 1)

- `RatingRange(decimal? Min, decimal? Max)`; `MovieQuery.Rating`.
- Inclusive 0.0–10.0, ≤1 decimal place; numeric domain aligned with public `ImdbRating.TryCreate` / `numeric(3,1)` (no `ImdbRating.cs` edit).
- Canonical raw keys `minRating` / `maxRating` (ticket spelling); OpenAPI/generated names PascalCase `MinRating`/`MaxRating`.
- Absent group: `Rating` null → no predicate; unrated remain eligible; serialize omits both keys.
- Supplied group: both keys required; either value may be empty (open bound); lone key → 422 at HTTP while web preserves raw partial presence through URL/outgoing params.
- Any present range (including `minRating=&maxRating=`) excludes unrated. `minRating=7&maxRating=` → rating >= 7.0.
- Valid present serialization always emits both keys (`null`→empty, `0`→`0`, `10`→`10`); never runtime/year extent-elision.
- No include-unrated toggle. Preserve absent versus present-with-null-bounds across all codecs.
- Acceptance catalog proof uses `RatingRange(7m, null)` and pairs the empty maximum key on HTTP.

### 2. Codec, validation, defensive predicate (Q3 + Round 1)

- Shared lexical rule in BOTH `MovieQueryString` and endpoint parse paths: empty = open; otherwise ASCII unsigned `[0-9]+(?:\.[0-9])?` anchored; leading zeros accepted (`07.0`); reject signs (`-0`/`+7`), whitespace, exponents, commas/thousands, `NaN`, `.5`, `7.`, `7.00`, `7.10` before numeric acceptance. Invariant culture; decimal-point style only. No new parser layer/file.
- Typed numeric `7.00m` ≡ `7m`; strict textual scale is a wire concern.
- Format emits shortest invariant ≤1 fractional digit (`7`, `7.1`, `0`, `10`); no silent rounding/clamping of raw input.
- `MovieQueryString.TryParse` rejects invalid/duplicate rating input.
- `MovieQueryValidator` and defensive predicate reuse `ImdbRating.TryCreate` for domain/precision; null bounds valid; min <= max only when both exist.
- HTTP 422 for malformed, duplicate, out-of-domain, excess-precision, inverted, or partial keys.
- Direct catalog invalid/inverted range → defensive empty set (total 0, empty page; distinct from HTTP 422).

### 3. Catalog predicate (Q5 + Round 1)

- One small `WhereRating` in `MovieRecordQueryExtensions`, mirrored after `WhereYear`; decimal-specific private helpers only if required; no generic range framework.
- Chain in `EfMovieCatalog.BrowseAsync` before count/paging; AND with existing filters.
- Exclude null ratings whenever range is present. Existing ordering unchanged.
- Real PostgreSQL proof retains existing suite translation posture; do not invent client-evaluation harness switches.

### 4. API and generated contract (Q5 + Round 1)

- `BrowseMoviesRequest` adds string `MinRating`/`MaxRating` documenting canonical query names; leave other parameter names unchanged; keep new properties inside existing scoped ReSharper suppression; no new comments; no new DTO.
- Endpoint decimal parse path shares the Round 1 lexical rule + paired-key semantics + MarkSeenOnce duplicates → 422.
- Regenerate OpenAPI baseline/Verify snapshot with `LAMUFLIX_REGENERATE_OPENAPI=true`, then `npm run generate:api`. Consume generated types only. Exact parameter-list assertion remains equality.

### 5. Live UI (Q4–Q5 + Round 1; see also `design.md`)

- Reuse local `DualRangeField`, extent 0..10, step 0.1, non-crossing thumbs.
- Absent filter displays `[0,10]` inactive; first thumb change activates; return to `[0,10]` stays active (unrated still excluded) until Clear all; serialize full-span as `0`/`10`, not elided.
- Open-bound hydration shows 0/10 ends without rewriting empty URL values; interaction materializes finite bounds.
- Normalize slider float noise once in live `filterQuery.ts` on interaction; never repair raw URL text.
- Invalid/partial/domain/precision/order URL input: preserve raw key presence/values through URL and outgoing params; display safe non-crossing `[0,10]`; existing 422 invalid-filter UI; deliberate rating interaction replaces BOTH bounds or Clear all removes; no `libraryQueries.ts` edit.
- Extend only live `filterQuery.ts`, `FilterPanel.tsx`, `browseParams.ts`, `LibraryView.tsx`. Leave `libraryUrl.ts` untouched.
- Reuse page reset, URL ownership, fetch/query key, grid/count, empty-state machinery. Rating = one active filter group when present.
- Taste (label `IMDb rating`, a11y names, one-decimal captions, after Release year): `ASSUMPTIONS.md`. Palette `design.md` exists and carries the Patron invalid-state/formatting/casing amendments.

## Proof strategy (Q6 + Round 1)

Backend unit: `MovieQueryTests` (equality/hash); `MovieQueryStringTests` (round trips, absent/open/full-span, lexical matrix including accepted `07.0` and rejected `7.00`/`7.10`/sign/whitespace/exponent/comma/leading-trailing-dot); `MovieQueryValidatorTests` (null/open bounds valid; TryCreate domain; order only when both); `MovieQueryFixture` for valid ranges in the existing property suite. Conditionally extend only `MovieQuerySurvivorTests.cs` / `MovieQueryStringSurvivorTests.cs` / `EfMovieCatalogSurvivorTests.cs` with rating-specific proof if mutation requires them — no new task IDs, no unrelated sweep.

Backend integration: `EfMovieCatalogBrowseTests` on real PostgreSQL under existing suite translation posture (min 7 open max; inclusive 0/7/10 and fractional/equal; unrated exclude/retain; defensive invalid-empty with total 0 / empty page; AND; count-before-paging); `LibraryEndpointsTests` (paired keys / same lexical+422 matrix); `OpenApiContractTests` (exact PascalCase parameter-list equality, drift, regeneration).

Web: `FilterPanel.test.tsx`, `filterQuery.test.ts`, `LibraryView.composed.test.tsx` — step/extents/captions/keyboard/non-crossing; absent vs active full-span/both-empty; open-bound hydration; Clear all; page=1; paired shortest ≤1dp URL/API params; interaction-only float normalization; raw-invalid/partial round trips and safe `[0,10]` display without URL mutation; preserved other filters/unknown keys; invalid URLs fetch 422 invalid-filter UI without rewrite. T020 uses test-local `server.use` rating-dependent responses and asserts actual outgoing paired canonical `minRating`/`maxRating` plus visible grid/count; generated handlers unchanged by hand.

## Phase order, pickup boundary, and gates (Q7)

**Phase A (this drafting lane)**: Quill drafts → Keel analyze → **Palette design** → challenge/adjudication → plan freeze → Patron provisional Gate 1 process. Neither this plan nor provisional Gate 1 authorizes implementation; the user retains spec-PR merge ownership.

**Phase B entry (pickup; not Phase A work)**:

1. Authorized base has landed dependencies **DEV-322**, **DEV-297**, and **DEV-302**.
2. Recheck **current-main drift** for the planned file envelope.
3. If either check fails, stop and report; do not begin production edits.
4. Specs for those dependencies being present in-tree is **not** current-main landing or Phase B drift clearance.

**Build order (Phase B)**:

1. Rating model, equality, codec, validator, unit/property fixture coverage.
2. `WhereRating`, `EfMovieCatalog` chain, PostgreSQL integration proof.
3. Endpoint parsing / 422 / OpenAPI contract tests.
4. Generate baseline/snapshot then TypeScript/MSW.
5. Live web state, parameter builder, slider, active-group count, Vitest proof.
6. Configured backend/web gates and all three M review axes (Sentry, Ledger, Compass).

**Gate verdicts**: Enforce current `harness.yml` thresholds without lowering and without passing literal threshold overrides — scripts read configured `harness.yml` values. Expected backend: Roslyn, complexity, InspectCode, property tests, vulnerable-package/security, `dotnet test`, format, pre-PR mutation. Applicable web: tests, lint, typecheck, build, API drift. Use documented/configured commands and scopes at that phase. Exit 2 = SKIPPED (scope-empty) / SKIP / mutation N/A as reported — never fold into PASS. Could not run is not PASS. Author tests before or with production changes and keep passing phase checkpoints; do **not** add a universal observed-red gate. **No gates are run during Phase 2 drafting.**

## Loop discipline — closing bar and caps (Q8)

- **Closing bar** (code-review / ship-review): resolve every confirmed Critical, High, and Medium finding; record Low as non-blocking.
- **Axes**: all three M axes mandatory (Sentry, Ledger, Compass); a missing axis is blocking.
- **Caps**: maximum **two** formal review/fix rounds and **two** plan challenge/fix rounds. At the cap, unresolved blockers stop for adjudication; no silent third round. Round 1 repairs do not imply a new formal axis round.
- **Frozen scope**: only Q1–Q7 IMDb rating-range model, query/validation/predicate, paired canonical API/URL keys, generated contract chain, live FilterPanel/LibraryView behavior, and the enumerated regression proof (refined by Round 1 amendment). Anything else is a follow-up issue, not a finding in this round.
- **Phase A status**: Plan freeze and Gate 1 remain pending Keel verification of these Round 1 doc repairs. No production/tests/gates/commit/PR in this repair ask.

## Complexity Tracking

> Constitution Check has no violations requiring justification.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

No complexity exception or constitution departure is requested.
