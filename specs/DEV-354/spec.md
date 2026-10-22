# Feature Specification: Multi-factor blend scoring with personal reason contract

**Feature Branch**: `feature/354-spec`

**Created**: 2026-10-22

**Status**: Draft (grill closed at Q12; plan freeze and Gate 1 not granted)

**Input**: Closed decision brief at `specs/DEV-354/brief.md` Q12 (controlling), with Q1–Q11 ledger; rulings in `CONCLUSIONS.md`; taste/policy tags in `ASSUMPTIONS.md`. Authoritative ticket Overview / Scope & Technical Design / AC-1..AC-3 as reconciled in brief Q4 (source: task note DEV-354 and cited recon Part 2 lines). Evidence HEAD `d794a0e0643a012d877e736349e02a2af59d9691`; intake base `53f4205d901a1798f41077bbd67127305dff301c`.

**Controlling closure**: brief.md Q12 supersedes historical OPEN / missing-ticket / path-blocker / next-step text without rewriting those sections. This draft does not claim plan freeze, Gate 1, gate1: provisional, or runtime readiness.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Multi-factor blend ranking (Priority: P1)

When a user requests related/recommended titles for a selected movie, each eligible candidate receives a raw multi-factor blend score that combines selected-title similarity, personal taste-profile similarity, and watched / not-interested penalties from configuration, then results are ordered by that score.

**Why this priority**: Delivers the ticket’s primary scoring behavior (Scope item 1 / AC-1) that ranking and downstream consumers depend on.

**Independent Test**: On a fixed fixture of candidates with known selected similarity, profile similarity, and watched / not-interested indicators, the computed scores match `Score = a*S + b*P - c*W - d*N` with configured coefficients, without post-blend clamp, division, or outer renormalization.

**Acceptance Scenarios**:

1. **Given** bounded input terms S, P, W, N each in [0,1] and valid finite nonnegative coefficients a, b, c, d with `a > 0 || b > 0`, **When** blend scoring runs, **Then** each eligible candidate’s Score equals `a*S + b*P - c*W - d*N` as a raw finite value (may be negative or above 1).
2. **Given** handler-eligible candidates (Not Interested already excluded), **When** blend scoring runs, **Then** N is 0.0 for every scored candidate; pure formula unit tests may still exercise N=1.
3. **Given** negative or nonfinite blend members, **When** pure evaluation runs, **Then** it throws `ArgumentOutOfRangeException` (brief CONTROLLING POST-Q12 SCORER-ERROR-POLICY; recon-DEV-354-3:14; CONCLUSIONS Q5).
4. **Given** `a = 0` and `b = 0`, **When** options validation/startup runs, **Then** existing `IValidatableObject` produces a field-level `ValidationResult` via BindAndValidate / ValidateDataAnnotations / ValidateOnStart (recon-DEV-354-3:16-17); do not invent a separate validation path or validate via `a+b` sum.
5. **Given** otherwise-finite inputs that produce a nonfinite calculated raw Score, **When** blend evaluation runs, **Then** it throws `InvalidOperationException` without clamping (brief CONTROLLING POST-Q12 SCORER-ERROR-POLICY; CONCLUSIONS Q5; DEV-354:58,61).

---

### User Story 2 - Structural and Personal reasons on every result (Priority: P1)

Every returned recommendation carries at least one caller-safe typed reason. Structural reasons come from existing selected-similarity explanations; Personal reasons explain positive comparable profile channels with generic templates only; if nothing else applies, a truthful Structural fallback is emitted.

**Why this priority**: Ticket Scope 2 / AC-2 require every result to have a reason and forbid Personal attribution of unwatched movies; DEV-355/357 consume kind plus prebuilt text.

**Independent Test**: For cold-start, profile-match, structural-only, and empty-explanation cases, every result has nonempty `{kind,text}` reasons with lowercase `structural` / `personal`, deterministic Structural-then-Personal order, generic Personal templates only when `b > 0` and channels are positive/comparable, and Structural fallback `No shared features identified` when otherwise empty.

**Acceptance Scenarios**:

1. **Given** a scored eligible candidate with existing DEV-338 / DEV-348 structural explanations, **When** reasons are built, **Then** those explanations appear as Structural reasons in deterministic order before any Personal reasons.
2. **Given** `b > 0` and positive comparable genre overlap, **When** Personal reasons are built, **Then** one Personal reason `Matches your <genre> interest` is emitted per canonical positive-mass overlapping genre in OrdinalIgnoreCase then Ordinal order, using caller-safe sanitized genre display text; do not select only the first such genre; no named-title Personal template is used.
3. **Given** `b > 0` and a positive comparable profile plot match, **When** Personal reasons are built, **Then** a Personal reason `Matches your viewing history` is emitted.
4. **Given** no Structural or Personal explanation would otherwise be produced, **When** reasons are finalized, **Then** exactly one Structural reason `No shared features identified` is present so the result still has ≥1 reason.
5. **Given** the HTTP recommendation response, **When** Reasons are serialized, **Then** each item is `{kind,text}` with lowercase kind `structural` or `personal`, plain caller-safe text (DEV-338 sanitation; no IDs/paths/diagnostics), propagated C# DTO → normalized OpenAPI → generated TypeScript; UI/endpoint do not construct explanation strings.

---

### User Story 3 - Not Interested never reaches scoring or output (Priority: P1)

Candidates marked Not Interested are removed in the recommendation handler before scoring or reason construction. A positive Not Interested penalty coefficient never substitutes for that exclusion.

**Why this priority**: Ticket Scope 3 / AC-3 and Q3/Q4 require exclusion over penalty substitution, with fixture and FsCheck proof.

**Independent Test**: Mixed, high-similarity, and all-excluded fixtures plus FsCheck over candidate flags prove flagged candidates are neither scored nor explained nor returned; remaining candidates keep Score descending, Movie.Id ascending, then Take, with no pool refill.

**Acceptance Scenarios**:

1. **Given** a candidate pool containing `Movie.IsNotInterested` items (including high selected-similarity ones), **When** `GetRecommendationsQueryHandler` runs, **Then** those candidates never enter scoring or reason construction and never appear in output.
2. **Given** every candidate is Not Interested, **When** the handler runs, **Then** the result set is empty (or otherwise contains zero Not Interested items) with no invented refill or query widening.
3. **Given** a positive `NotInterestedPenalty` (d), **When** exclusion is evaluated, **Then** d is never used as a substitute for pre-scoring removal of Not Interested candidates.

---

### Edge Cases

- Profile similarity with only genre, only plot, or neither channel comparable → mean of available channels, or 0 when none (does not renormalize outer a/b).
- Malformed nonempty 384-vectors → explicit rejection; empty/zero vectors are not comparable (after real vector acquisition exists).
- Watched-only movies with no launches → DEV-353 undated age-zero watched signal; launches when `IsWatched=false` are preserved; watched-plus-launch must not duplicate the undated signal.
- `b = 0` or nonpositive/unavailable profile channels → no Personal reasons; Structural path / fallback still covers every-result AC-2.
- Raw blend score outside [0,1] → allowed; callers must not assume a probability.
- Absent candidate or contributor 384-vector infrastructure at Phase B start → **STOP** for dependency recon; no always-empty profile, invented vector store, or selected-cosine reuse.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST compute eligible-candidate Score as the literal raw finite blend `a*S + b*P - c*W - d*N` with pre-weight terms in [0,1], no post-blend clamp, division, or missing-component renormalization of a/b (Q4–Q5; AC-1).
- **FR-002**: System MUST expose configurable coefficients on `RecommendationOptions` as `BlendSelectedWeight` (a, default 1.0), `BlendProfileWeight` (b, default 1.0), `AlreadyWatchedPenalty` (c, default 0.25), `NotInterestedPenalty` (d, default 1.0); each finite and `>= 0.0`, requiring `a > 0.0 || b > 0.0` without summing `a+b` ([assumed] names/defaults; Q5 / ASSUMPTIONS.md). Preserve existing `IValidatableObject` plus composition-root BindAndValidate / ValidateDataAnnotations / ValidateOnStart (recon-DEV-354-3:16-17; `ServiceDefaults/Extensions.cs:90`). Finite/nonnegative coefficient failures and the `a=b=0` cross-field condition MUST surface as field-level `ValidationResult`s on that path; negative/nonfinite members reaching pure evaluation throw `ArgumentOutOfRangeException`; nonfinite calculated raw Score from otherwise finite inputs throws `InvalidOperationException` without clamping (brief CONTROLLING POST-Q12 SCORER-ERROR-POLICY).
- **FR-003**: Selected similarity S MUST be DEV-338 selected-item similarity; profile similarity P MUST follow Q6: weighted Jaccard over case-insensitive union of trimmed nonempty candidate genres and unnormalized DEV-353 `GenrePreferences` using `GenreWeights.GetWeight` for present candidate genres (else 0), plus `clamp(max(0, cosine(candidateEmbedding, profile.PlotEmbedding)), 0, 1)` with stable scaled arithmetic on two finite nonzero 384-vectors; arithmetic mean of comparable channels, single channel if one, else 0. DEV-348 selected-candidate cosine MUST NOT substitute for profile-candidate cosine.
- **FR-004**: System MUST exclude every `Movie.IsNotInterested` candidate in `GetRecommendationsQueryHandler` before scoring or reason construction; d MUST NOT substitute for exclusion (Q3–Q4; AC-3).
- **FR-005**: After exclusion, system MUST order by Score descending, then Movie.Id ascending, then Take; MUST NOT refill or widen the query when exclusion shrinks the pool.
- **FR-006**: Every returned result MUST include ≥1 typed reason: Core `Reason` with `Kind` (`Structural` | `Personal`) and caller-safe `Text`; wire `RecommendedMovieDto.Reasons` as `{kind,text}` with lowercase `structural` / `personal` through C# DTO → OpenAPI → generated TS (Q7; §2.3 item 4; AC-2).
- **FR-007**: Reason emission MUST be deterministic Structural (existing DEV-338 feature reasons and DEV-348 Similar story) then Personal only when `b > 0` for positive comparable profile channels using only generic templates `Matches your <genre> interest` and `Matches your viewing history`; MUST emit one Personal genre reason per canonical positive-mass overlapping genre in OrdinalIgnoreCase then Ordinal order with caller-safe sanitized display text (not only the first such genre); MUST NOT emit `Because you watched <title>` or invent titles from aggregate profile / selected cosine (Q7–Q8; brief CONTROLLING C3; AC-2).
- **FR-008**: When no other explanation exists, system MUST emit Structural `No shared features identified` so every result still has a truthful reason (Q7; AC-2).
- **FR-009**: System MUST acquire actual materialized watched/launch/genre/vector inputs via `IRecommendationProfileSource` returning immutable `RecommendationProfileInputs`, implemented by `EfRecommendationProfileSource`, and build `TasteProfile` once per request with DEV-353 `TasteProfileCalculator` and `GenreWeights` (Q9–Q10).
- **FR-010**: `EfRecommendationProfileSource` MUST read existing `MovieRecord.IsWatched` / genres and add a NEW PlayedAt read query over `playback_events` grouping all launches; preserve every launch and DEV-353 watched-only age-zero fallback; MUST NOT modify entity/config/writer/schema/migrations (Q10).
- **FR-011**: Pure `BlendScorer`, `ProfileSimilarityCalculator`, and DEV-353 `TasteProfileCalculator` MUST be direct construction/calls with no artificial DI wrappers; I/O stays in adapter/handler (Q10–Q11). **Current facts (do not claim these are DEV-354 changes):** `GetRecommendationsQueryHandler` currently takes `GenreWeightsCalculator` among its dependencies and registers through `AddHandler` at `src/LamuFlix.Api/HandlerRegistration.cs:49` (DEV-354:59); `GenreWeightsCalculator` signature is `(IMovieCatalog catalog, GenreWeightsCache cache)` at `src/LamuFlix.Core/Features/Recommendations/GenreWeightsCalculator.cs:14`, with scoped `TryAdd` at `src/LamuFlix.Infrastructure/Persistence/MovieCatalogServiceCollectionExtensions.cs:14` (DEV-354:59; recon-DEV-354-3:12). **Proposed wiring:** add the approved `IRecommendationProfileSource` as a handler dependency; register scoped EF `EfRecommendationProfileSource` beside existing sources in `PersistenceServiceCollectionExtensions.cs`; keep pure scorers as direct construction/calls with no artificial DI; retain existing handler `AddHandler` registration and existing `GenreWeightsCalculator` scoped TryAdd.
- **FR-012**: Contract chain MUST update by extending the **existing** single writer `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` / `WriteRegeneratedOutput` in `OpenApiContractTests`, gated by case-insensitive `LAMUFLIX_REGENERATE_OPENAPI=true` (brief CONTROLLING POST-Q12 EXPORTER CLARIFICATION; recon-DEV-354-3:55-65; DEV-354:59). That writer MUST obtain the normalized registered `/openapi/v1.json` document and write **both** `web/src/api/openapi.json` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json`, reusing `RepositoryRoot` (walk up to 8 ancestors from `AppContext.BaseDirectory` for exactly one `Directory.Packages.props`; zero/multiple → `InvalidOperationException`), absolute `CommittedBaselinePath` / `DerivedSnapshotPath` resolution, and `IsWithinRoot` guard. Migrate the reasons assertion to authorized `{kind,text}`. Unset or non-true env MUST write neither; content equality alone is insufficient no-write proof. Clear the env even on failure. After DTO/assertion changes: opt-in paired regeneration → clear env → final ordinary no-export drift/equality/no-write checks → from `web/` `npm run generate:api` then `npm run check:api` → then consumer migration and dependent consumer tests. Generated `types.ts` and `mocks/handlers.ts` MUST NOT be hand-edited. No duplicate NEW exporter or `LAMUFLIX_OPENAPI_EXPORT` selector is authorized.
- **FR-013**: Phase B MUST NOT start source implementation until merged specs, Epic 7 sequencing, fresh bounded main drift/dependency recon, compatible landed DEV-338/348/353, and real candidate AND contributor 384-vector acquisition are established; otherwise STOP (Q10–Q12). Supported gate commands/settings from recon-DEV-354-3:24-51 are **prescribed for Phase B, not executed by this draft**; keep PASS / SKIPPED (scope-empty) / opt-out SKIP / mutation N/A / Could not run distinct; require Phase B recheck of harness/tool wiring. Do not invent rendered tool config or runner settings (recon-DEV-354-3:51).
- **FR-014**: MMR / diversity re-ranking remains DEV-416 and out of scope. Anything else is a follow-up issue, not a finding in this round (Q2/Q12).

### Key Entities

- **RecommendationOptions**: Blend coefficient configuration (a, b, c, d) with finite nonnegative validation and `a > 0 || b > 0` via existing `IValidatableObject` / BindAndValidate path; pure-eval and calculated-score error policy per FR-002.
- **Blend score inputs**: Per-candidate S (selected similarity), P (profile similarity), W (already watched 0/1), N (not interested 0/1; always 0 when handler-eligible).
- **TasteProfile / TasteProfileMovie**: DEV-353 contracts built once per request from materialized watched/launch/genre/vector rows.
- **RecommendationProfileInputs**: Immutable snapshot of TasteProfileMovie rows plus candidate-ID → 384-float vector map for the current candidate set.
- **Reason**: Core kind (`Structural` | `Personal`) plus caller-safe text; wire shape `{kind,text}`.
- **RecommendedMovie**: Domain recommendation carrying Score and typed Reasons; DTO mirrors wire contract.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001 / AC-1**: Fixture proves blend formula `a*S + b*P - c*W - d*N` with terms in [0,1] before weighting and raw finite final Score (no clamp/division/outer renormalization).
- **SC-002 / AC-2**: Every returned recommendation has ≥1 caller-safe typed reason; Personal reasons use only Q7/Q8 generic templates (never name an unwatched movie); cold-start/no-signal yields Structural fallback.
- **SC-003 / AC-3**: FsCheck (and mixed fixture) proves Not Interested candidates never appear in output and never reach scoring/reason construction; d never substitutes for exclusion.
- **SC-004**: Eligible output ordering remains Score desc, Id asc, Take, with no refill after exclusion.
- **SC-005**: Paired normalized registered `/openapi/v1.json` baseline (`web/src/api/openapi.json`) and drift snapshot match authorized `{kind,text}` Reasons; opt-in `LAMUFLIX_REGENERATE_OPENAPI=true` writes both via the single existing writer; unset/non-true writes neither (proof beyond byte equality); regenerated `types.ts` / `mocks/handlers.ts` follow after final ordinary no-export checks, then consumer migration.

## Assumptions

Taste and policy assumptions are authoritative only as logged in `specs/DEV-354/ASSUMPTIONS.md` (Q5–Q12). Summary:

- Coefficient field names/defaults (Q5).
- Profile similarity formulas and equal channel weighting (Q6).
- Kind spellings, templates, ordering, Structural fallback text (Q7).
- Generic-only Personal explanation policy; no named-title template (Q8).
- New type/file names; export via extension of existing paired writer / `LAMUFLIX_REGENERATE_OPENAPI=true` (Q9; post-Q12 exporter clarification).
- New PlayedAt reader in approved adapter; pure direct calculator construction (Q10).
- Q11 proof matrix and dependency-aware ordering (Q11).
- Q12 ratifies the above for drafting only; does not waive Phase B prerequisites or grant freeze/Gate 1.
- Scorer-error policy: `ArgumentOutOfRangeException` / `ValidationResult` / `InvalidOperationException` distinctions (post-Q12 scorer-error-policy clarification).

## Out of scope

- MMR / diversity (DEV-416).
- New NuGet/npm dependency, project, architectural layer, schema/migration, vector store, or entity/configuration/playback-writer edits.
- Named-title Personal explanations; separate contributor-provenance type.
- Always-empty runtime profile or selected-candidate cosine as substitute for missing profile/vector inputs.
- Plan freeze, Gate 1, implementation, generators, tests, or gates claimed by this draft.
- Delivery reduction or constitution departure (none approved; future cases follow §2.3 escalation).

## Closing bar and review caps (Q2 / Q12)

- Blocking: Critical, High, Medium. Non-blocking: Low.
- Maximum two review rounds and two fix commits per round; no third-round authorization or gate waiver.
- M-tier adjudication requires Sentry, Ledger, and Compass reports.
- Ship review, hosted checks, merge bar, and user-owned merge remain separate phases.
- Scope boundary: anything else is a follow-up issue, not a finding in this round.
