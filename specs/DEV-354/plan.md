# Implementation Plan: Multi-factor blend scoring with personal reason contract

**Branch**: `feature/354-spec` | **Date**: 2026-10-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/DEV-354/spec.md`, closed brief Q12 (controlling), CONCLUSIONS.md Q1–Q12, ASSUMPTIONS.md.

**Status**: Draft plan for post-grill specification. **Does not claim plan freeze or Gate 1.** Phase A remains specification-only until separate freeze/Gate 1 decisions after Keel read-only analysis/conformance and plan challenge.

**Evidence**: worktree `F:/Dev/LamuFlix.worktrees/feature-354-spec`; branch `feature/354-spec`; HEAD `d794a0e0643a012d877e736349e02a2af59d9691`; intake base `53f4205d901a1798f41077bbd67127305dff301c`.

## Summary

Implement multi-factor recommendation blend scoring and an end-to-end Structural/Personal reason contract for recommendation endpoints and UI consumers. Score eligible candidates with the literal raw formula `a*S + b*P - c*W - d*N` using configurable `RecommendationOptions`, DEV-338 selected similarity, and Q6 profile similarity over materialized TasteProfile + candidate embeddings. Exclude Not Interested before scoring. Acquire profile inputs through a narrow EF-backed port/adapter with a new PlayedAt read query. Migrate Reasons from string arrays to typed `{kind,text}` by extending the **existing** paired opt-in writer `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` / `WriteRegeneratedOutput` (`LAMUFLIX_REGENERATE_OPENAPI=true`), then final ordinary no-export checks, then web `generate:api` / `check:api`, then consumer migration. MMR stays DEV-416.

Phase B source work is gated on merged specs, Epic 7 sequencing, fresh bounded main drift/dependency recon, compatible landed DEV-338/348/353, and **real candidate AND contributor 384-vector acquisition**.

## Technical Context

**Language/Version**: .NET 10 / C# (backend); TypeScript / Vite / React (`web/`)

**Primary Dependencies**: Existing LamuFlix stack only — no new NuGet/npm dependency authorized. Relies on landed/compatible DEV-338, DEV-348, DEV-353 contracts at Phase B start.

**Storage**: Existing PostgreSQL mappings only (`movies.is_watched`, `movie_genres`, `playback_events.played_at`). No schema/migration/entity/config/writer edits. Vector storage/reader: **unavailable at evidence HEAD** — Phase B STOP until real acquisition lands (brief Q10/Q12).

**Testing**: xUnit unit/property (FsCheck), Testcontainers PostgreSQL for EF adapter, existing recommendation endpoint/E2E/OpenAPI contract tests, architecture tests, affected web consumer/mock tests.

**Target Platform**: LamuFlix API + web SPA (local/personal streaming library)

**Project Type**: Vertical-slice style recommendation feature in existing solution + web contract consumers

**Performance Goals**: Not specified in closed brief — not invented.

**Constraints**:
- Pure calculators: direct construction/calls; no artificial DI.
- Contract chain: C# DTO → OpenAPI → generated TS; no hand-edits of generated files.
- Features:LocalPlay, secrets, Process.Start: untouched.
- Gate commands/settings: pin from recon-DEV-354-3:24-51 as **prescribed, not executed** by this draft; Phase B must recheck harness/tool wiring. Do not invent rendered tool config, `stryker-config.json` contents, or unset `gates.mutation.runner` / `concurrency` values (recon-DEV-354-3:33,51).

**Scale/Scope**: M-tier; authorized file ledger Q8–Q11 only; MMR out of scope (DEV-416).

## Constitution Check

*GATE: Separate plan freeze / Gate 1 — not claimed here.*

Aligned with closed grill (no new constitution departure):
- §2.3 item 2: new narrow port `IRecommendationProfileSource` deliberately approved (Q9).
- §2.3 item 4: public Reasons shape change `{kind,text}` deliberately approved (Q7).
- §2.3 item 6: scoped file ledger only; no unrelated rewrites (Q9–Q11).
- Configuration isolation: blend weights on `RecommendationOptions` (Q5; Constitution VII cited in CONCLUSIONS Q5).
- No new dependency/project/layer/schema; no Features:LocalPlay / secrets / Process.Start touch.
- Contract chain preserved; web consumes generated types.

If a future fact forces ticket change or constitution departure → owner §2.3 escalation; not silently assumed.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-354/
├── brief.md             # Closed grill decisions (Q12 controlling)
├── CONCLUSIONS.md       # Full Q1–Q12 exchanges
├── ASSUMPTIONS.md       # [assumed] taste/policy log
├── spec.md              # This feature's specification
├── plan.md              # This file
└── tasks.md             # Implementation task ledger
```

### Authorized source / test / web ledger (exact; Q8–Q11)

Only necessary feature and contract-migration edits. Presence does not require editing every path. No unrelated rewrites.

**Existing Core / API**

- `src/LamuFlix.Core/Options/RecommendationOptions.cs`
- `src/LamuFlix.Core/Features/Recommendations/Reason.cs`
- `src/LamuFlix.Core/Features/Recommendations/SimilarityScorer.cs`
- `src/LamuFlix.Core/Features/Recommendations/SimilarityResult.cs`
- `src/LamuFlix.Core/Features/Recommendations/RecommendedMovie.cs`
- `src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs`
- `src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs`
- `src/LamuFlix.Api/Endpoints/RecommendedMovieDto.cs`

**New Core / Infrastructure / ports** ([assumed] names; Q9)

- `src/LamuFlix.Core/Ports/IRecommendationProfileSource.cs`
- `src/LamuFlix.Core/Features/Recommendations/RecommendationProfileInputs.cs`
- `src/LamuFlix.Core/Features/Recommendations/BlendScorer.cs`
- `src/LamuFlix.Core/Features/Recommendations/ProfileSimilarityCalculator.cs`
- `src/LamuFlix.Infrastructure/Persistence/EfRecommendationProfileSource.cs`

**Composition roots (exact paths; Q11 / DEV-354:48)** — necessary wiring only

- `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` (scoped-compatible `EfRecommendationProfileSource`)
- `src/LamuFlix.Api/HandlerRegistration.cs`
- `src/LamuFlix.ServiceDefaults/Extensions.cs` (options binding)

**Evidence-only persistence references (NOT edit-ledger entries; Q10)**

- `MovieRecord` / `MovieConfiguration` (`is_watched`, genres / `movie_genres`)
- `GenreRecord`
- `PlaybackEventRecord` / `PlaybackEventConfiguration` (`playback_events.played_at`)
- `EfPlaybackHistory` (write-only; do not treat as reader; do not modify)

**Unit / property / options tests**

- `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerTests.cs`
- `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerPropertyTests.cs`
- `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerInvariantPropertyTests.cs`
- `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerGoldenTests.cs`
- `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerGoldenFixture.cs`
- `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerTests.cs`
- `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerPropertyTests.cs`
- `tests/LamuFlix.UnitTests/Options/RecommendationOptionsTests.cs`
- `tests/LamuFlix.UnitTests/Features/Recommendations/BlendScorerTests.cs` (new)
- `tests/LamuFlix.UnitTests/Features/Recommendations/BlendScorerPropertyTests.cs` (new)
- `tests/LamuFlix.UnitTests/Features/Recommendations/ProfileSimilarityCalculatorTests.cs` (new)
- `tests/LamuFlix.UnitTests/Features/Recommendations/ProfileSimilarityCalculatorPropertyTests.cs` (new)

**Integration / architecture / snapshots**

- `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs`
- `tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs`
- `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` (**extend existing** `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` / `WriteRegeneratedOutput`; single writer)
- `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` (paired write target with baseline)
- `tests/LamuFlix.IntegrationTests/EfRecommendationProfileSourceTests.cs` (new; real PostgreSQL)
- `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`

**Web**

- `web/src/api/openapi.json` (export output; not hand-authored except via export)
- `web/src/api/types.ts` (generated)
- `web/src/mocks/handlers.ts` (generated; never hand-edit)
- `web/src/mocks/fixtures.ts`
- `web/src/features/movie-details/RelatedMovies.tsx`
- `web/src/features/movie-details/RelatedMovies.test.tsx`
- `web/src/mocks/recommendations.test.ts`
- `web/src/mocks/handlers.test.ts`
- `web/src/mocks/generate-api.test.ts`

**Structure Decision**: Extend existing recommendation vertical slice and persistence adapter surface; no new project or top-level folder beyond the authorized paths above.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| New Core port `IRecommendationProfileSource` (§2.3 item 2) | Runtime watched/launch/genre/vector acquisition absent from handler; needs cancellation-aware materialized snapshot | In-handler EF queries rejected: keeps I/O out of Core pure scoring and matches DEV-353 calculator boundary |
| Public Reasons shape change to `{kind,text}` (§2.3 item 4) | DEV-355/357 must consume kind + prebuilt text without UI inference | Keeping string[] cannot carry Structural vs Personal kind |

## Policy (implementation)

### Blend and profile evaluation

1. Preserve `RecommendationOptions` as `IValidatableObject` with existing BindAndValidate / ValidateDataAnnotations / ValidateOnStart at `src/LamuFlix.ServiceDefaults/Extensions.cs:90` (recon-DEV-354-3:16-17). Validate each of a,b,c,d finite and `>= 0` via field-level `ValidationResult`s; require `a > 0 || b > 0` as field-level `ValidationResult` for `a=b=0` (do not validate via `a+b` sum).
2. Negative or nonfinite blend members at pure evaluation: `ArgumentOutOfRangeException` (recon-DEV-354-3:14; brief CONTROLLING POST-Q12 SCORER-ERROR-POLICY).
3. Otherwise-finite inputs producing nonfinite calculated raw Score: `InvalidOperationException` without clamping (brief CONTROLLING POST-Q12 SCORER-ERROR-POLICY; CONCLUSIONS Q5; DEV-354:58,61).
4. Compute S via DEV-338 path; P via `ProfileSimilarityCalculator` over materialized inputs only.
5. W and N are 0.0/1.0; handler-eligible N is always 0.0. Pin W per candidate: `W = 1.0` iff candidate `Movie.IsWatched` is true, else `0.0`, taken from the existing candidate source/summary (`IRecommendationCandidateSource` / handler Summarize path; recon-DEV-354:13,16; recon-DEV-354-2:141). No new query for W. Contributor launch history via `IRecommendationProfileSource` remains separate (profile acquisition only). Pure `BlendScorer` tests supply W as a numeric input; handler fixtures prove the IsWatched→W mapping.
6. Final Score raw finite; no post-blend clamp, division, or outer renormalization; negative and above-one finite scores remain allowed.
7. Construct/call `TasteProfileCalculator`, `BlendScorer`, `ProfileSimilarityCalculator` directly; build profile once per request.

### Not Interested

Exclude in `GetRecommendationsQueryHandler` before scoring/reasons; independent of adapter; d never substitutes; no refill.

### Reasons

Structural then Personal; generic Personal templates only when `b > 0` and channels positive/comparable; one Personal genre reason per canonical positive-mass overlapping genre in OrdinalIgnoreCase then Ordinal order with caller-safe sanitized `Matches your <genre> interest` text (not only the first such genre); plot-channel Personal `Matches your viewing history` when positive comparable; Structural fallback when empty; migrate golden/unit Structural expectations without changing DEV-338 selected-score semantics or order.

**Reason.Kind adoption (atomic contract step before Phase 2 checkpoint):** Land Core `Reason.Kind`, every existing Structural producer including `SimilarityScorer.cs`, `RecommendedMovieDto` Reasons `{kind,text}` (lowercase wire kinds), and the `RecommendationEndpoints` mapper as one dependency-linked atomic migration, with only the minimum fixture/test adaptation required to compile and test. Do **not** add a Structural-defaulting constructor. Do **not** change DEV-338 selected-score semantics or reason order. Assign `RecommendedMovieDto.cs` once in that atomic step; later Personal/fallback emission (handler path) does not re-own the DTO file.

### Persistence adapter

- Include movies with `IsWatched` OR any launch.
- NEW read query groups all `PlaybackEventRecord` PlayedAt values; preserve every launch; add undated age-zero watched signal only when there are no launches for a watched movie.
- Do not modify records, mappings, `EfPlaybackHistory`, schema, or migrations.
- Vector roundtrip tests blocked until real candidate AND contributor acquisition lands; never mock the driver for that proof; never substitute empty profile or selected cosine.

### Wiring (current facts + proposed; do not claim changes already exist)

**Current facts** (DEV-354:59; recon-DEV-354-3:12):
- `GetRecommendationsQueryHandler` primary constructor currently takes `IMovieRepository`, `IRecommendationCandidateSource`, `GenreWeightsCalculator`, `IOptions<RecommendationOptions>` (recon-DEV-354-3:12).
- Handler registers through `AddHandler<GetRecommendationsQueryHandler, GetRecommendationsQuery, ImmutableList<RecommendedMovie>>()` at `src/LamuFlix.Api/HandlerRegistration.cs:49`.
- `GenreWeightsCalculator` signature `(IMovieCatalog catalog, GenreWeightsCache cache)` at `src/LamuFlix.Core/Features/Recommendations/GenreWeightsCalculator.cs:14`; scoped `TryAddScoped<GenreWeightsCalculator>()` at `src/LamuFlix.Infrastructure/Persistence/MovieCatalogServiceCollectionExtensions.cs:14`.
- Existing persistence registrations at `PersistenceServiceCollectionExtensions.cs:41-43` for movie repository, playback, candidate source; options BindAndValidate at `ServiceDefaults/Extensions.cs:90`.

**Proposed wiring**:
- Add approved `IRecommendationProfileSource` as a handler dependency; register scoped `EfRecommendationProfileSource` beside existing sources in `PersistenceServiceCollectionExtensions.cs`.
- Retain current `AddHandler` registration line and existing `GenreWeightsCalculator` scoped TryAdd; do not invent a second registration path.
- Pure scorers (`BlendScorer`, `ProfileSimilarityCalculator`, DEV-353 `TasteProfileCalculator`): direct construction/calls only; no artificial DI.

## Existing paired opt-in normalized-v1 OpenAPI writer (Quill-specified; Patron post-Q12)

Authority: brief CONTROLLING POST-Q12 EXPORTER CLARIFICATION; CONCLUSIONS.md Post-Q12 exporter clarification; recon-DEV-354-3:55-65; DEV-354:59. Extending the existing operation/helper as the **single** writer satisfies Q9–Q12. Duplicate NEW operation / `LAMUFLIX_OPENAPI_EXPORT` wording is superseded and unauthorized. Build implements and executes the following in Phase B.

### Discoverable selector / environment

| Piece | Value |
|-------|--------|
| Environment variable | `LAMUFLIX_REGENERATE_OPENAPI` |
| Required opt-in value | `true` (case-insensitive) |
| Existing test operation | `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` |
| Existing helper | `WriteRegeneratedOutput` |
| Host file | `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` |

Unset or any non-true value skips the writer. Ordinary tests write neither artifact and verify drift. Document route: `/openapi/v1.json` (recon-DEV-354-3:56).

### Root resolution and paired output paths

Reuse existing conventions (recon-DEV-354-3:56-59):
- `RepositoryRoot`: walk from `AppContext.BaseDirectory` up to **8** ancestors for exactly one `Directory.Packages.props`; zero or multiple matches throw `InvalidOperationException`.
- Absolute `CommittedBaselinePath` → `web/src/api/openapi.json` and `DerivedSnapshotPath` → `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` via `Path.GetFullPath(Path.Combine(RepositoryRoot(), relative))`.
- `IsWithinRoot` containment guard before writes.
- Single writer obtains the **normalized registered** `/openapi/v1.json` document and writes **both** paths together.

### Exact invocation

**Working directory (cwd)**: repository / worktree root  
(`F:/Dev/LamuFlix.worktrees/feature-354-spec` on this task; equivalent root on later Phase B worktrees.)

**Opt-in paired regeneration (PowerShell)** — after authorized DTO/assertion changes:

```powershell
Set-Location <repo-root>
$env:LAMUFLIX_REGENERATE_OPENAPI = 'true'
try {
  dotnet test tests/LamuFlix.IntegrationTests --filter "FullyQualifiedName~OpenApiContractTests.OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether" --nologo
}
finally {
  Remove-Item Env:LAMUFLIX_REGENERATE_OPENAPI -ErrorAction SilentlyContinue
}
```

Clear `LAMUFLIX_REGENERATE_OPENAPI` **even on failure**.

### Validation Build must prove

1. **Author export/assertion changes** for authorized `{kind,text}` Reasons (migrate string-items assertion at recon-DEV-354-3:63) while extending the existing paired writer — not a second exporter.

2. **Opt-in paired regeneration** — run the command above. Assert: **both** `web/src/api/openapi.json` and `OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` are written with the normalized registered document reflecting authorized Reasons `{kind,text}` (lowercase kinds).

3. **Clear env** (including on failure), then **final ordinary no-export** drift/equality/no-write checks from cwd repo root with `LAMUFLIX_REGENERATE_OPENAPI` unset (and any non-true value covered):

   ```powershell
   Remove-Item Env:LAMUFLIX_REGENERATE_OPENAPI -ErrorAction SilentlyContinue
   dotnet test tests/LamuFlix.IntegrationTests --filter "FullyQualifiedName~OpenApiContractTests" --nologo
   ```

   Assert: neither baseline nor derived snapshot is written by that ordinary run; host document, committed baseline, and snapshot remain equal for the authorized contract; Reasons shape is `{kind,text}`. **Byte equality alone is not no-write proof** — prove the writer path was not exercised (unset/non-true skip), not merely that file bytes match.

4. **Only after** steps 1–3: from cwd `web/`:

   ```powershell
   Set-Location <repo-root>/web
   npm run generate:api
   npm run check:api
   ```

   Then migrate consumers and run dependent consumer tests. Consumer tests depend on generation/freshness and migration. Never hand-edit `web/src/api/types.ts` or `web/src/mocks/handlers.ts`. Snapshot/export changes must reflect only the authorized DTO contract.

## Behavioral proof matrix (Q11)

| Area | Obligations |
|------|-------------|
| BlendScorer unit/property | Independent `a*S+b*P-c*W-d*N`; bounded terms; binary penalties; zero/independent weights; negative and >1 raw scores; invalid coefficients; nonfinite rejection; pure tests may use N=1; handler-eligible N=0. Pure tests supply W as a 0.0/1.0 input (not source-mapping tests). Handler fixtures separately prove W is mapped from candidate `Movie.IsWatched` via existing candidate source/summary (no new W query) |
| ProfileSimilarityCalculator unit/property | Case-insensitive weighted Jaccard on unnormalized preferences; 2/1/0 comparable channels; +/-/0 cosine and finite 384 extremes; malformed nonempty rejection; finite [0,1] output; no selected-cosine substitution |
| Handler fixtures + FsCheck | Mixed/high-similarity/all-excluded Not Interested never scored/explained/returned; existing seams/discriminating inputs; no spy-only abstraction; Score-desc/Id-asc/Take; no refill; profile once per request; deterministic Structural-then-Personal; generic Personal only when b>0; Structural fallback; migrate Structural/golden expectations without changing DEV-338 selected-score semantics |
| EfRecommendationProfileSource (PostgreSQL) | Watched-only; launch-only; all launches; watched+launch without duplicate undated signal; genres; cancellation. Vector roundtrip **blocked** until real acquisition |
| Endpoint/E2E/architecture/web | Eligible ordering; raw finite scores; nonempty lowercase `{kind,text}`; safe generic strings; dependency boundaries; consumers use generated objects without constructing explanations |

## Gate expectations (Q11; prescribed from recon-DEV-354-3:24-51, not executed here)

**Phase A (now)**: specification drafts only. No production tests or gates run for pass claims. After this draft: Keel read-only speckit-analyze + brief conformance → plan challenge → **separate** plan-freeze and Gate 1 decisions.

**Harness settings** (`harness.yml` at evidence HEAD; recon-DEV-354-3:24-33):
- `baseBranch: main`; `tracker: youtrack`
- `gates.complexity.implement: 15`; `gates.complexity.refactor: 6`
- `gates.mutation.threshold: 80`; `wholeProject: LamuFlix.Core`; `maxKillShare: 50`; `maxKillFileSpan: 15`; exclusions: `LamuFlix.Api`, `LamuFlix.Infrastructure` (Infrastructure adapter is not mutated by the gate; Core scorers/handler are)
- `gates.analyzers`: mode `All`; `warningsAsErrors: true`
- `gates.vulnerablePackages`: `fail: true`; `includeTransitive: true`
- `gates.inspectCode.enabled: true`; `gates.propertyTests.enabled: true`; `gates.web.enabled: true`
- `gates.mutation.runner` and `gates.mutation.concurrency` are **unset** in harness.yml (optional script defaults apply) — do not invent values (recon-DEV-354-3:33)

**Prescribed commands** (recon-DEV-354-3:35-48; Phase B executes after STOP; this draft does not run them):
- `./scripts/run-roslyn-analyzers.ps1` `[-BaseRef] [-Files "a.cs","b.cs"] [-Solution] [-All]` — exit 0/1/2 (SKIPPED scope-empty)
- `./scripts/run-cyclomatic-complexity.ps1` `[-BaseRef] [-Files] [-Threshold n] [-Solution] [-All]` — implement default from config/fallback 15; refactor gate `-Threshold 6`
- `./scripts/run-jetbrains-inspectcode.ps1` `[-BaseRef] [-Files] [-MinSeverity ...] [-Solution] [-All] [-NoBuild]`
- `./scripts/run-mutation.ps1` `[-BaseRef] [-DryRun] [-WholeProject] [-EvaluateReport path] [-ScopeFile] [-Project name[]] [-OutputRoot dir] [-Help]` — exit 0 PASSED / 1 FAILED / 2 SKIPPED or NOT APPLICABLE
- `./scripts/run-property-tests.ps1` `[-Project <single .csproj>] [-Category Property] [-Help]`
- `./scripts/run-vulnerable-packages.ps1` `[-Severity ...] [-IncludeTransitive <bool>] [-Help]`
- `./scripts/run-web-gates.ps1` `[-BaseRef] [-All] [-Help]` — in `web/`: lint, `tsc --noEmit`, `test:run`, build when web scope touched
- `dotnet test` / focused `--filter "FullyQualifiedName~<TestClass>" --nologo -v q`; `dotnet format --verify-no-changes`

**Not invented / unread** (recon-DEV-354-3:51): contents of `CodeMetricsConfig.txt`, `.editorconfig`, `_gate-common.ps1`. Verified (recon-DEV-354-3:51,79; DEV-354:66): `.config/dotnet-tools.json` and `stryker-config.json` are byte-identical HEAD vs origin/main (`dotnet-stryker` 5.0.0; test-runner `mtp`, coverage-analysis `off`, additional-timeout `30000`) — not unread; does not clear T009 STOP / Phase B NOT READY. **Phase B must recheck** harness/tool wiring before claiming executable readiness. Do not certify readiness from this draft alone.

**Status discipline**: Report PASS, SKIPPED (scope-empty), opt-out SKIP, mutation N/A, and Could not run as distinct; never fold non-PASS into PASS. Surviving mutants → fix with tests; never lower thresholds. Focused unit/property, real infrastructure, API/architecture, and affected web checks accompany implementation. M-tier: Sentry, Ledger, Compass; Q2 bar/caps; ship/hosted/merge bar separate; user owns merge.

## Dependency-aware order (Q11 / Q12)

1. Phase A drafting, read-only analysis/conformance, plan challenge; separate freeze/Gate 1.
2. Phase B prerequisite/drift recon against main — **STOP** if required inputs/predecessors unavailable.
3. Options; **atomic** Reason.Kind + Structural producers (`SimilarityScorer.cs`) + DTO + endpoint-mapper contract (minimum fixture/test adaptation; compile-safe) **before** the Phase 2 checkpoint; then pure calculators + focused tests.
4. Profile-input port/snapshot + real EF adapter + PostgreSQL tests.
5. Handler prefilter, profile-once, blend/reasons/order (W from candidate `Movie.IsWatched`), minimal composition wiring.
6. Endpoint/integration tests against the already-migrated `{kind,text}` wire shape (DTO/mapper owned by step 3).
7. Author OpenAPI export/assertion changes → opt-in paired regeneration (`LAMUFLIX_REGENERATE_OPENAPI=true`) of baseline **and** snapshot → clear env (even on failure) → final ordinary no-export drift/equality/no-write checks → from `web/` `npm run generate:api` then `npm run check:api` → then consumer migration and dependent consumer tests (consumer tests depend on generation/freshness and migration).
8. Full backend/web gates, pre-PR mutation, required reviews, hosted checks; user-owned merge bar.

Focused tests accompany each implementation step. No permanent partial delivery, new schema/dependency, missing-vector replacement, or generated hand edits.

## Phase B STOP prerequisites (mandatory)

Before any source implementation:

1. Merged specifications for this feature’s required contract set.
2. Epic 7 sequencing respected.
3. Fresh bounded main drift/dependency recon.
4. Compatible landed DEV-338 / DEV-348 / DEV-353 contracts.
5. Real candidate **AND** contributor 384-vector acquisition available.

If any item fails: stop for dependency recon. Forbidden substitutes: invented vector store, always-empty profile, selected-cosine reuse, readiness waiver, silent scope reduction. Q6 per-movie missing-channel handling cannot replace absent infrastructure.

## Open drafting items (not inventable here)

- **needs recon (Phase B only)**: Confirm real candidate AND contributor vector acquisition surfaces and compatible landed DEV-338/348/353 shapes against main (closed brief establishes unavailability at evidence HEAD `d794a0e`). Fresh bounded main drift/dependency recon remains mandatory STOP input.
- **needs recon (Phase B recheck only; do not invent)**: Rendered/tool config still unread at recon-DEV-354-3:51 — `CodeMetricsConfig.txt`, `.editorconfig`, `_gate-common.ps1`, and any resolved `gates.mutation.runner` / `concurrency` defaults. `.config/dotnet-tools.json` and `stryker-config.json` are verified byte-identical HEAD vs origin/main (recon-DEV-354-3:51,79; DEV-354:66) — not unread; T010 partial recheck for those two does not clear T009 STOP. Prescribed harness commands/settings above are evidence-backed from recon-DEV-354-3:24-51 but not certified executable until Phase B recheck.
- **Resolved (do not re-open as needs recon)**: Options validation / scorer-error policy (brief CONTROLLING POST-Q12 SCORER-ERROR-POLICY); handler registration call and `GenreWeightsCalculator` signature/registration (DEV-354:59); existing paired OpenAPI writer / `LAMUFLIX_REGENERATE_OPENAPI=true` (brief CONTROLLING POST-Q12 EXPORTER CLARIFICATION; recon-DEV-354-3:55-65).

Unresolved semantic product choices beyond this brief → `needs decision` to Keel/Patron; do not silently assume or widen scope.
