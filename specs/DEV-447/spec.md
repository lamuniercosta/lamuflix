# Feature Specification: OMDb Wrong-Match Validation (Memento, O Brother)

**Feature Branch**: `feature/447-spec`

**Created**: 2026-11-03

**Status**: `gate1: provisional` — Keel completed three-axis adjudication and read-only cross-artifact analysis on 2026-11-03 (brief Plan adjudication closure; note adjudication-DEV-447). Ready for Rigger spec PR; user Gate 1 merge remains pending. No implementation clearance.

**Input**: Ticket DEV-447 (bug under DEV-288; Size M, no ui tag); frozen `specs/DEV-447/brief.md` (Q1–Q8); `CONCLUSIONS.md`; Maestri notes `DEV-447`, `recon-DEV-447`; `specs/PRODUCT.md`. No decisions beyond the brief. No taste assumptions, new domain vocabulary, new ADR, or owner checkbox (brief Status; Q8).

## Scope

**In scope** (brief Frozen scope Q1–Q4; Files touched Q5; ticket Fix/Acceptance via recon-DEV-447:108,124–130):

1. Automatic OMDb match validation private/local to the existing OMDb adapter: explicit `Type=movie` (case-insensitive) on automatic direct responses and search candidates; reject episode, series, missing, and unknown types.
2. Before accepting an automatic direct response as Found: non-null existing `TitleMatcher.Score` ≥ configured shared `MatchThreshold` against the existing recleaned lookup, and non-suspicious mapped metadata.
3. Suspicious is exactly the conjunction of existing mapped values: `Rating` is null AND `Directors.Count == 0` AND `Runtime` is null. Reuse current mapper/domain parsing; no independent numeric confidence thresholds; missing poster alone or only one/two absent fields does not reject; no poster network probes.
4. Rejected direct response enters the existing finite fallback-query sequence; exclude its already-rejected IMDb ID from automatic candidate selection within that `FindAsync` call.
5. Filter movie candidates then preserve existing best-score/year selection; fetch at most one selected candidate detail per logical fallback; apply the same automatic type/title/year/completeness guard against the original recleaned lookup before Found.
6. Rejected fetched detail returns NotFound for existing chain fallthrough to TMDb when enabled; do not restart OMDb, page, fan out detail requests, or try lower-ranked candidates after that rejected detail.
7. Valid direct hits remain one request; preserve current transport/status/malformed-JSON/cancellation/rate-limit/quota outcomes and per-request quota gate — those failures must not become ordinary NotFound. Classify/Map failures precede automatic eligibility on direct and fetched-detail paths; unusable title retains Failed(InvalidResponse) and MissingTitle logging (no fallback/TMDb).
8. Preserve manual IMDb-ID bypass and its existing behavior; preserve stored-year precedence, matcher normalization, year tolerance/unknown-year handling, and configured shared `MatchThreshold`.
9. Deterministic Unit/WireMock AC1 proofs for `Memento[2000]` → `tt0209144` and `O Brother, Where Art Thou[2000]` → `tt0190590` (ticket-derived synthetic stubs, not captured incident payloads).
10. Mandatory Build-phase AC2 via existing `PUT /api/movies/{id}/imdb` / manual-match UI under test-portal-policy for `/api/movies/7` → `tt0209144` and `/api/movies/29` → `tt0190590`.

**Out of scope** (brief Frozen scope; Q5–Q8; §2.3):

- Changes to `TitleMatcher`, `MatchThreshold`/options, TMDb provider production code, provider-chain production code, API routes/DTO/status/transition semantics, OpenAPI/generated TypeScript, web UI, schema/migrations, dependencies, project/layer boundaries, recommendation algorithms, LocalPlay, secrets, or `Process.Start`.
- New endpoints, broader enrichment retry transitions for already-`Enriched` wrong matches, or ad-hoc SQL reset.
- Captured raw incident OMDb payloads or a claimed proven root-cause trace (recon line 94; brief Status) — use deterministic synthetic ticket-derived stubs only.
- New ADR, glossary/`CONTEXT.md` rewrite, empty ASSUMPTIONS artifact, or constitution amendment (Size M; Q8).
- Recommendation algorithm changes (correct enrichment is the scope; knock-on director-filter / related / For You effects are consequences of correct identity, not algorithm work).

**Production allowlist** (Q5): `src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.cs`, `src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.Mapping.cs`.

**Primary test allowlist** (Q5): `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderFallbackUnitTests.cs`, `tests/LamuFlix.UnitTests/Adapters/OmdbMetadataProviderUnitTests.cs`, `tests/LamuFlix.IntegrationTests/MetadataProviderLookupTests.cs`, `tests/LamuFlix.IntegrationTests/MetadataProviderChainCompositionTests.cs`. Run existing `tests/LamuFlix.UnitTests/Adapters/TitleMatcherTests.cs` unchanged as regression. Forced payload compatibility edits in other existing OMDb fixtures only when an automatic success fixture lacks movie type or usable Q4 metadata — record exact file and AC/gate reason before editing; not permission to weaken assertions.

**Precedence**: Ticket Fix/Acceptance text (recon) plus Patron Q1–Q8 in `brief.md` / `CONCLUSIONS.md` are authoritative. Phase A produces documents only and claims no implementation gate evidence.

## What the User Will See *(mandatory)*

- **Where**: Movie details for a library entry whose automatic OMDb match would previously have been a wrong episode or incomplete wrong title (incident examples: Memento, O Brother).
- **What the user sees or does**: After correct enrichment, the details view shows the correct identity with usable poster, rating, and director data. Already-wrong `Enriched` entries are corrected through the existing Match Manually / `PUT /api/movies/{id}/imdb` path (no new UI).
- **What it needs to show up**: Running app with enrichment and OMDb credentials available; for AC2, the two incident library entries `/api/movies/7` and `/api/movies/29` and the existing manual-match workflow under test-portal-policy.

### Smoke Test *(maps to Build AC2 — existing path, not new e2e suite)*

- **SMOKE-001**: **Given** movie `/api/movies/7` is wrong-matched or awaiting correction, **When** the operator corrects it to `tt0209144` via existing Match Manually / `PUT .../imdb` and enrichment completes, **Then** details show the correct Memento identity with poster, rating, and director evidence retained per test-portal-policy.
- **SMOKE-002**: **Given** movie `/api/movies/29` is wrong-matched or awaiting correction, **When** the operator corrects it to `tt0190590` via the same existing path and enrichment completes, **Then** details show the correct O Brother identity with poster, rating, and director evidence retained per test-portal-policy.

Automatic-selection proof remains AC1 Unit/WireMock tests; SMOKE/AC2 must not claim manual override exercised the automatic matcher (Q6).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Automatic direct-hit eligibility (Priority: P1)

Automatic OMDb direct title responses are accepted as Found only when they are explicitly movies, score at or above the configured threshold against the recleaned lookup, and are not suspicious under the Q4 conjunction. Episodes, series, missing/unknown types, below-threshold scores, year-vetoed scores, and fully incomplete rating/director/runtime records are rejected into the existing fallback sequence (or NotFound when no eligible path remains). Manual IMDb-ID lookup stays exempt.

**Why this priority**: Closes the unchecked direct `t=` acceptance path that is the recon-backed wrong-match candidate (recon:24–30; Q1–Q2, Q4).

**Independent Test**: Unit/WireMock cases for movie vs episode/series/absent/unknown Type; score at threshold vs below; year veto and unknown-year preserved; each single usable rating/director/runtime field vs all three absent/N/A/invalid; valid direct hit exactly one request; ManualImdbId still Found without automatic guards.

**Acceptance Scenarios**:

1. **Given** an automatic direct response with explicit Type=movie, non-null score ≥ MatchThreshold against Recleaned(stored), and not suspicious, **When** `FindAsync` runs, **Then** the result is Found after exactly one request.
2. **Given** an automatic direct response that is episode, series, missing Type, or unknown Type, **When** `FindAsync` runs, **Then** it is not accepted as Found and enters the existing finite fallback sequence (or NotFound if no eligible fallback).
3. **Given** an automatic direct movie response whose TitleMatcher.Score is null or below MatchThreshold, **When** `FindAsync` runs, **Then** it is rejected from Found and enters fallback (or NotFound).
4. **Given** mapped Rating null AND Directors empty AND Runtime null, **When** the automatic guard runs, **Then** the record is suspicious and rejected; a missing poster alone or only one/two of those fields absent does not reject.
5. **Given** a ManualImdbId lookup, **When** `FindAsync` runs, **Then** automatic type/score/suspicious guards do not apply and existing bypass behavior is preserved.

---

### User Story 2 - Bounded fallback, rejected-ID exclusion, detail revalidation (Priority: P1)

After a rejected direct hit, search uses the existing finite fallback-query sequence, keeps only movie-typed candidates under existing score/year selection, excludes the already-rejected direct IMDb ID within that call, fetches at most one selected detail, and re-applies the same automatic guard against the original recleaned lookup before Found. A rejected detail returns NotFound so the existing chain may try TMDb; no restart, paging, or lower-ranked fan-out. Transport/quota/cancellation failures stay their existing outcomes.

**Why this priority**: Enforces ticket Fix bullets 1–3 with bounded quota cost and closes the unchecked detail-body hole (Q3; recon:29–30,54).

**Independent Test**: WireMock fallback matrices for rejected-ID exclusion, movie-only candidate filtering, one detail fetch, detail revalidation fail → NotFound, chain composition proving TMDb can succeed after OMDb NotFound, and failure/quota/cancellation preservation.

**Acceptance Scenarios**:

1. **Given** a rejected direct IMDb ID, **When** search candidates are scored in the same `FindAsync`, **Then** that ID cannot win BestCandidate selection.
2. **Given** search hits mixing movie and episode/series/missing/unknown types, **When** candidates are filtered, **Then** only explicit movie types are eligible for scoring/selection.
3. **Given** a selected movie candidate, **When** its detail is fetched, **Then** at most one detail fetch occurs for that logical fallback and the detail must pass the same automatic guard against Recleaned(stored) before Found.
4. **Given** a fetched detail that fails the automatic guard, **When** the provider finishes, **Then** the outcome is NotFound (not a restart or lower-ranked retry), allowing existing chain fallthrough to TMDb when enabled.
5. **Given** transport, HTTP status, malformed-JSON, cancellation, rate-limit, or quota failures, **When** they occur on an actual request, **Then** existing failure outcomes are preserved and are not converted to ordinary NotFound.

---

### User Story 3 - Ticket regressions select the correct IMDb IDs (Priority: P1)

Deterministic synthetic Unit/WireMock fixtures derived from the ticket prove automatic matching for both incident folder titles selects the correct IMDb IDs, including punctuation/year use. These stubs are not claimed captured incident payloads and do not assert unknown live-incident payload causality (brief Status; recon:94; plan-challenge round 1 Patron clarification 2).

**AC1 guidance (Q4 residual identity risk)**: Q4 suspiciousness is strictly `Rating is null && Directors.Count == 0 && Runtime is null`. A movie-typed same-title/same-year wrong IMDb id that still carries at least one usable Q4 field (for example a valid Runtime) remains eligible under Q4 and MUST NOT be universally rejected, denylisted, or excluded by a new heuristic. AC1 remains mandatory synthetic selection proof for the two ticket titles → correct IDs; AC2 remains mandatory live remediation. Neither AC1 stubs nor AC2 claims prove the exact reject path the unknown live incident bodies would have taken. Document this residual wrong-identity risk; do not invent capture causality.

**Why this priority**: Ticket Acceptance 1 is mandatory Build verification evidence for automatic selection (Q5–Q6).

**Independent Test**: Named regression tests in the primary allowlist files: `Memento[2000]` → `tt0209144` (wrong candidate `tt24735934` must not win); `O Brother, Where Art Thou[2000]` (missing `?`) → movie `tt0190590` (episode `tt24456948` must not win). Both titles remain required in unit **and** WireMock per Q5.

**Acceptance Scenarios**:

1. **Given** lookup title/year corresponding to folder `Memento[2000]` and synthetic OMDb payloads offering wrong `tt24735934` and correct `tt0209144`, **When** automatic `FindAsync` runs, **Then** the Found identity is `tt0209144` (ticket-derived synthetic proof only — not a claim of live incident payload causality).
2. **Given** lookup title/year corresponding to folder `O Brother, Where Art Thou[2000]` (missing question mark) and synthetic payloads offering episode `tt24456948` and movie `tt0190590`, **When** automatic `FindAsync` runs, **Then** the Found identity is `tt0190590` (same synthetic-only provenance rule).
3. **Given** those fixtures, **When** tests are named/planned, **Then** provenance is identified as ticket-derived synthetic regressions, not captured real OMDb incident bodies.

---

### User Story 4 - Live remediation proof without new API scope (Priority: P2)

After code gates in the Build workspace, AC2 remains mandatory: correct the two incident movies through the existing manual IMDb path, await enrichment, and verify displayed posters, ratings, and directors with environment/HEAD/timestamp and before/after evidence under test-portal-policy. If runtime or credentials are unavailable, AC2 is reported pending/blocked — never silently waived. Manual correction does not prove automatic selection.

**Why this priority**: Ticket Acceptance 2 is mandatory and must not expand API/retry/SQL scope (Q6–Q7).

**Independent Test**: Running-app receipt per Q6 steps 1–3; AC1 remains the independent automatic-selection proof.

**Acceptance Scenarios**:

1. **Given** Build code gates are green and the app/credentials are available, **When** `/api/movies/7` is corrected to `tt0209144` and `/api/movies/29` to `tt0190590` via existing `PUT .../imdb` / Match Manually, **Then** completed enrichment shows correct posters, ratings, and directors with retained evidence.
2. **Given** wrong matches already in Enriched state, **When** ordinary `POST .../enrichment` is considered, **Then** it remains refused; no new endpoint, broader transition, or SQL reset is introduced.
3. **Given** unavailable app or provider credentials, **When** AC2 cannot run, **Then** it is recorded pending/blocked and missing environment facts route through recon — not folded into a silent pass or substituted with fixture evidence alone.

### Edge Cases

- **Classification/map before automatic eligibility** (plan-challenge round 1 clarification 1; Compass F1; Sentry F4): On both automatic direct `t=` and fetched-detail `i=` paths, Classify/Map failures precede the automatic eligibility guard. A `Response=True` body whose title is unusable/missing still logs `MissingTitle` and returns `Failed(InvalidResponse)` — it MUST NOT enter fallback or unlock TMDb. Only a successfully mapped Found record that the automatic guard then rejects routes to fallback (direct) or NotFound (fetched detail).
- Fail-closed on missing or unknown Type for automatic paths (Q2) — applies only after a mapped Found reaches the guard.
- Score exactly at MatchThreshold is accepted when other guards pass; below threshold is rejected (Q2, Q5).
- Unknown-year scoring semantics stay unchanged (TitleMatcher YearAdjustment 0.0) (Q4).
- Year veto beyond existing tolerance still rejects via null score (Q2, Q5).
- Suspicious requires all three mapped fields unusable; one usable field among rating/directors/runtime is enough to clear suspicion if type/score pass (Q4). Residual: a movie-typed same-title/same-year wrong ID with ≥1 usable Q4 field may still be accepted under Q4 — documented residual identity risk; no universal rejection, denylist, or new heuristic (plan-challenge clarification 2).
- Rejected direct ID exclusion is zero extra request cost within the same FindAsync (Q3).
- After rejected fetched detail: NotFound only — no OMDb restart, no paging, no lower-ranked detail fan-out (Q3).
- Quota/rate-limit/provider-unavailable/cancellation/malformed-JSON/HTTP failures never become ordinary NotFound (Q3, Q5).
- ManualImdbId remains fully exempt from automatic eligibility; shared Classify/FoundOutcome/FetchByIdAsync MUST NOT unconditionally enforce automatic guards so manual sparse-metadata authority is preserved (Q2, Q5; Sentry F2).
- Forced fixture payload repairs must record file + AC/gate reason; must not weaken assertions (Q5). Outside the four primary test files, repairs stay payload-only (plan-challenge clarification 4).
- Unrelated existing `harness.yml` modification on the branch is preserved and excluded from this task staging (Q5, Q7).
- Anything outside frozen Q1–Q7 scope is a follow-up issue, not a finding in this round (Q8).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Preserve ManualImdbId bypass and its existing behavior unchanged (Q1–Q2).
- **FR-002**: Automatic matching MUST use the existing Recleaned(stored) lookup; preserve stored-year precedence, TitleMatcher normalization, year tolerance/unknown-year handling, and configured shared MatchThreshold without changing TitleMatcher or OmdbOptions (Q1–Q2, Q5).
- **FR-003**: Automatic direct responses and search candidates MUST require explicit Type=movie case-insensitively; reject episode, series, missing, and unknown types (Q2).
- **FR-004**: Before accepting an automatic direct response as Found, require non-null TitleMatcher.Score ≥ configured MatchThreshold against Recleaned(stored) and non-suspicious mapped metadata under FR-005 (Q2, Q4). The automatic eligibility guard evaluates only a successfully mapped Found record (FR-004a).
- **FR-004a**: On both automatic direct `t=` and automatic fetched-detail `i=` paths, Classify/Map MUST run before automatic eligibility. A `Response=True` body that cannot map (unusable/missing title) MUST retain `Failed(InvalidResponse)` and existing `MissingTitle` logging — MUST NOT enter fallback or unlock TMDb. Only mapped Found records rejected by the automatic guard route to fallback (direct) or NotFound (fetched detail) (plan-challenge clarification 1; Compass F1; Sentry F4).
- **FR-005**: Suspicious MUST be exactly `metadata.Rating is null && metadata.Directors.Count == 0 && metadata.Runtime is null` using existing mapper/domain validity; no new numeric confidence thresholds; no poster probes; missing poster alone or partial field absence MUST NOT reject (Q4). Do not add an IMDb-ID denylist or extra heuristic to close residual same-title/year wrong-identity risk under Q4 (plan-challenge clarification 2).
- **FR-006**: A rejected automatic direct response (mapped Found that fails the guard) MUST enter the existing finite FallbackQueries sequence; exclude that rejected IMDb ID from automatic BestCandidate selection within the same FindAsync call (Q2–Q3).
- **FR-007**: Search MUST filter to movie candidates then preserve existing best-score/year selection; fetch at most one selected candidate detail per logical fallback; apply the same automatic type/title/year/completeness guard against the original Recleaned(stored) before Found (Q3). Automatic guards apply only at automatic direct and automatic fetched-detail entry points — shared Classify/FoundOutcome/FetchByIdAsync MUST NOT unconditionally enforce them (Sentry F2; plan-challenge clarification 4).
- **FR-008**: Rejected fetched detail (mapped Found that fails the guard) MUST return NotFound for existing chain fallthrough to TMDb when enabled; MUST NOT restart OMDb, page, fan out details, or try lower-ranked candidates after that rejection (Q3).
- **FR-009**: Valid direct hits MUST remain one request; preserve existing transport/status/malformed-JSON/cancellation/rate-limit/quota outcomes and per-request quota gate; those failures MUST NOT convert to ordinary NotFound (Q3, Q5). Preserve `Failed(InvalidResponse)` / `MissingTitle` for unmappable Found-flag bodies per FR-004a.
- **FR-010**: Production edits MUST stay on `OmdbMetadataProvider.cs` and `OmdbMetadataProvider.Mapping.cs` (bind Type where needed; keep guard private/local). Private helper extraction for complexity stays strictly inside those two files — no new type, interface, sibling partial file, or layer without a new Patron ruling (Q5; §2.3; plan-challenge clarification 3). TitleMatcher.cs, TMDb, chain production code, APIs, OpenAPI/TS, web, schema, options/threshold, dependencies, and recommendation logic MUST NOT change.
- **FR-011**: Primary tests MUST cover the Q5 matrix in the four named allowlist files using deterministic synthetic ticket-derived stubs (Memento / O Brother regressions in unit **and** WireMock); run TitleMatcherTests unchanged; forced other OMDb fixture edits only with recorded file+reason and payload-only outside the primary four (Q5; plan-challenge clarification 4). Mapper-edge cases in the primary four may use ManualImdbId exemption or eligible automatic fixtures so mapper assertions remain tested.
- **FR-012**: AC1 Unit/WireMock tests MUST prove automatic selection of `tt0209144` and `tt0190590` for the two incident folder titles without live OMDb; they remain mandatory and do not claim unknown incident payload causality (Q5–Q6; plan-challenge clarification 2).
- **FR-013**: AC2 MUST remain mandatory Build-phase running-app verification via existing PUT `/api/movies/{id}/imdb` / Match Manually for movies 7 and 29 under test-portal-policy, with environment/HEAD/timestamp and before/after evidence; never silently waive; never claim AC2 proves automatic matching; no new API/retry/SQL reset (Q6).
- **FR-014**: Phase B verification MUST run configured harness gates against the task diff (Roslyn, cyclomatic complexity, InspectCode never `-All`, property tests, vulnerable packages, `dotnet format --verify-no-changes` per brief Q7 and task-pipeline Phase 3 step 5, `dotnet test`, mutation for changed production C# without `--since` in worktrees, web applicability). Thresholds only from harness.yml; do not edit the unrelated harness.yml change; exit 1 and Could not run block; exit 2 SKIPPED (scope-empty) / SKIP / mutation N/A never recorded as PASS; property exit 2 only with recorded task-note opt-out reason. Size M three-axis review (Sentry, Ledger, Compass) under Q8 closing bar; round cap two / two fix commits per round; missing axis blocks (Q7–Q8).

### Key Entities

- **Automatic OMDb eligibility guard**: Private/local adapter rule requiring Type=movie, non-null TitleMatcher score ≥ MatchThreshold against Recleaned(stored), and non-suspicious mapped metadata before Found on automatic direct and automatic fetched-detail paths only (after Classify/Map succeeds).
- **Suspicious metadata conjunction**: Rating null AND empty Directors AND Runtime null under existing mapping validity.
- **Rejected direct IMDb ID**: Identity excluded from BestCandidate within the same FindAsync after a rejected mapped direct response.
- **Manual IMDb identity**: Operator-supplied IMDb id that bypasses automatic eligibility unchanged (shared Classify/FoundOutcome/FetchByIdAsync remain unguarded for this path).
- **Ticket regression stubs**: Deterministic synthetic OMDb payloads for Memento and O Brother folder titles — not captured incident traffic; residual Q4 same-title/year wrong-identity risk remains documented.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Unit/WireMock tests for both incident folder titles automatically select `tt0209144` and `tt0190590` (ticket AC1).
- **SC-002**: Automatic paths reject episode/series/missing/unknown types and suspicious (all-three-absent) records; accept threshold-passing non-suspicious movies (Q2, Q4).
- **SC-003**: Rejected direct IDs cannot re-win search in the same call; rejected details become NotFound with at most one detail fetch and no OMDb restart; chain can obtain TMDb metadata instead of a bad OMDb Found (Q3).
- **SC-004**: Valid direct hits remain one request; failure/quota/cancellation semantics and ManualImdbId bypass remain green (Q3, Q5).
- **SC-005**: After Build gates, live AC2 corrects movies 7 and 29 via the existing manual path and shows correct posters, ratings, and directors with retained evidence — or reports pending/blocked without silent waiver (ticket AC2; Q6).
- **SC-006**: No public API/DTO/status/OpenAPI/TS/web/schema/dependency/TitleMatcher/MatchThreshold/TMDb/chain-production change is required for this ticket (Q5; §2.3).
- **SC-007**: Applicable configured gates pass or report SKIPPED/SKIP/N/A/Could not run distinctly — never as PASS; review closing bar and round cap hold (Q7–Q8).

## Assumptions

- No new taste assumptions. Brief Status: do not create empty ASSUMPTIONS.md / glossary / ADR artifacts.
- Recon-backed unchecked direct response is a defect candidate, not a measured incident trace; stubs are synthetic and ticket-derived (brief Status; recon:94). AC1/AC2 do not claim unknown incident payload causality (plan-challenge clarification 2).
- Shared MatchThreshold remains the existing configured value and continues to apply to TMDb unchanged because this ticket does not edit it (ADR 0030 cited in recon; Q1).
- Phase B pickup must refresh recon for main/contract drift; Phase A recon baseline is not clearance (Q7). When OMDb credentials exist at pickup, Wisp records nonsecret live Type/title/year/rating/director/runtime for the two incident wrong IDs (or marks unavailable as unknown); never fabricate; never read/print the API key; observed contract mismatch stops for reanalysis without changing AC1/AC2.

## Clarifications

All grill categories were resolved by Patron Q1–Q8 before drafting. `/speckit-clarify` scan: Functional Scope, Domain/Data, Interaction, NFR, Integration/Dependencies, and Out-of-scope — **Clear**. No `[NEEDS CLARIFICATION]` markers remain. No owner Gate 1 checkbox (Q8).

## Traceability

| Source | Encoded as |
|---|---|
| Ticket Fix 1–3 / Acceptance 1–2 | Scope; US1–US4; FR-003–FR-013; SC-001, SC-005 |
| Q1 scope + suspicious conjunction | Scope; US1; FR-005; Edge Cases |
| Q2 automatic eligibility + ManualImdbId | US1; FR-001–FR-004, FR-006–FR-007 |
| Q3 detail validation + bounds + ID exclusion | US2; FR-006–FR-009; SC-003 |
| Q4 suspicious-field semantics + residual identity risk | US1; US3 AC1 guidance; FR-005; Edge Cases |
| Q5 file manifest + test matrix | Scope allowlists; FR-010–FR-012; US3 |
| Q6 running-app AC2 | What the User Will See; US4; FR-013; SC-005 |
| Q7 gates/ordering | FR-014; plan Ordering; tasks phases |
| Q8 closing bar / frozen scope / round cap | FR-014; SC-007; plan Gates; Edge Cases |
| Plan-challenge round 1 clarifications 1–4 | FR-004a; FR-007; FR-010–FR-011; Edge Cases; Assumptions |
| recon-DEV-447 paths / ticket text | Scope; US3; Assumptions |
