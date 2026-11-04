# Implementation Plan: OMDb Wrong-Match Validation (Memento, O Brother)

**Branch**: `feature/447-spec` | **Date**: 2026-11-03 | **Spec**: `specs/DEV-447/spec.md`

**Input**: Frozen `brief.md` (Q1–Q8); `CONCLUSIONS.md`; Maestri notes `DEV-447`, `recon-DEV-447`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (gates cite only; no constitution or ADR edits — none authorized).

**Status**: PLAN FROZEN — Keel completed three-axis adjudication and read-only cross-artifact analysis on 2026-11-03 (brief Plan adjudication closure; note adjudication-DEV-447). Ready for Patron provisional Gate 1 and Rigger spec PR; user Gate 1 merge remains pending. No implementation clearance or production/test edits in this Spec workspace.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md`, or `contracts/` — every technical choice is ruled in the brief, and the public contract chain is untouched (precedent DEV-426 / DEV-439). No ASSUMPTIONS.md / glossary / ADR (brief Status; Q8).

## Summary

Stop automatic OMDb enrichment from accepting wrong episode/incomplete title hits by validating automatic direct and search-detail responses inside `OmdbMetadataProvider` (+ Mapping Type binding): require Type=movie, TitleMatcher.Score ≥ MatchThreshold against Recleaned(stored), and non-suspicious mapped metadata (Rating null ∧ empty Directors ∧ Runtime null). Rejected directs enter existing finite fallback with rejected-ID exclusion; one detail fetch; rejected detail → NotFound for TMDb fallthrough. Preserve ManualImdbId, TitleMatcher, MatchThreshold, APIs, chain production code, and failure/quota semantics. Prove AC1 with deterministic ticket-derived WireMock stubs; prove AC2 via existing manual IMDb path under test-portal-policy.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`TreatWarningsAsErrors=true`, nullable enabled, CPM).

**Primary Dependencies**: Existing pins only. **No new packages** (brief §2.3; recon-DEV-447:80–82).

**Storage**: N/A — no schema/migration (recon:84; Q5).

**Testing**: xUnit v3, Shouldly; existing WireMock harness; UnitTests for provider/fallback; IntegrationTests for lookup contract and chain composition. Deterministic synthetic ticket-derived stubs. No live OMDb for AC1. TitleMatcherTests regression-run unchanged. Forced other OMDb fixture payload edits only when documented.

**Target Platform**: .NET server (ASP.NET Core Minimal API host hosting enrichment).

**Project Type**: Infrastructure adapter/mapping guard only (private/local).

**Performance Goals**: Valid direct hit remains one request; rejected direct may incur existing finite fallback cost (accepted Q1 trade-off); at most one selected detail fetch per logical fallback (Q3; ADR 0031 quota bounds).

**Constraints**: No explanatory comments outside allowed exemptions; do not delete or rewrite existing comments/docstrings unrelated to the change (no unrelated comment cleanup; existing comment rules still govern new/changed comments — no blanket exemption). Build and refactor cyclomatic-complexity thresholds derive from `harness.yml` only (do not hardcode either value; brief:96); never lower thresholds; no public API/secret/LocalPlay; do not edit TitleMatcher or MatchThreshold; do not convert failure/quota outcomes to NotFound; preserve unrelated harness.yml branch modification and do not stage it with this task. Mutation hygiene: guard literals are `const` (never `static readonly`); no new static initialisers in `OmdbMetadataProvider`. Tests use deterministic `TimeProvider`/`FakeTimeProvider` and seeded `Random` when needed; no `DateTime.Now`, `Task.Delay`, or wall-clock waits. Private helper extraction for complexity stays strictly inside the two Q5 production files — no new type, interface, sibling partial file, or layer; unmet complexity gate requires a new Patron ruling, not allowlist expansion (plan-challenge clarification 3).

**Scale/Scope**: personal library; Size M; two named regressions plus general automatic eligibility.

## Constitution Check

Gates checked against `.specify/memory/constitution.md` (current 1.10.0 in worktree). No constitution amendment and no ADR (Size M; Q8). No owner Gate 1 checkbox.

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | No new ports/layers | PASS — private/local guard inside existing OMDb adapter; no new port, project, or layer. |
| II (Explicit Handlers and Decorators) / V (Validated Inputs, Consistent Error Responses) | Caller-safe domain failures via existing exception mapping; public API / error-response stability | PASS — no public route/DTO/status/transition change; NotFound remains the existing provider outcome for chain fallthrough; failure categories for transport/quota stay mapped as today. |
| IV (Enrichment state machine) | Existing statuses/transitions | PASS — no EnrichmentStatus or retry-transition widening; Enriched wrong matches still use existing manual IMDb path (Q6). |
| VI (Observability) | Safe structured logging | PASS — preserve existing never-log-api-key invariants (recon:86); no new secret handling. |
| VII (Configuration / time) | Validated options; TimeProvider | PASS — MatchThreshold/options untouched; Recleaned continues to use existing cleaner + TimeProvider path. |
| VIII (Ubiquitous Language) | No invented Core vendor types | PASS — no new Core types/glossary; adapter-local validation only. |
| IX (Test Pyramid) | WireMock for HTTP; no mocked driver translation where SQL matters | PASS — WireMock for OMDb HTTP; no DB schema work; reuse existing assertion/mocking conventions (Q5). |
| Tech Stack | Typed HttpClient + resilience for metadata providers | PASS — existing OMDb client/quota gate per request retained (Q3, Q5). |
| Governance / Static-Analysis Gates | Harness thresholds; no lowered gates | PASS — Phase B only; see Gates; unrelated harness.yml change preserved/excluded. |

### Complexity Tracking

No constitution violations requiring justification. No amendment path.

## Approach

- **Phase A (this draft)**: Quill drafts spec/plan/tasks + checklist + receipt from brief/CONCLUSIONS/recon facts only; Keel runs read-only `/speckit-analyze` and plan challenge; **Patron provisional Gate 1 → Rigger opens the spec PR → user merges Gate 1 → Keel T000 merge receipt**. No Build code/gates here (Q7; Compass F3).
- **Phase B pickup (after Gate 1)**: Wisp fresh recon drift check against main for the approved file/contract set, including actual visibility recon that Mapping Response/SearchHit DTOs stay adapter-local (no guessed `internal` modifier or public contract-chain change); when OMDb credentials exist, record nonsecret live Type/title/year/rating/director/runtime for incident wrong IDs `tt24735934` / `tt24456948` (or `t=Memento&y=2000` equivalents) — mark unavailable as unknown, never fabricate, never read/print the API key; observed contract mismatch stops for reanalysis without changing AC1/AC2. Keel blocking reanalysis if drift changes contract/plan; Gauge baseline before code edits (Q7; plan-challenge clarification 2; Ledger L7).
- **Tests before behavior where feasible**: Anvil lands **new-behavior** regressions red (type/threshold/suspicious rejection into fallback, rejected-ID exclusion, detail-revalidation → NotFound, AC1 selections) against current unchecked direct acceptance. Existing ManualImdbId, failure/quota/cancellation, and valid-direct one-request proofs are **green regression locks** that must stay green — not absolute-red for the full matrix (Q7; Compass F2). Unit matrix uses the existing HTTP-stub pattern in the unit project; WireMock cells land in `MetadataProviderLookupTests` — no second HTTP-stub approach (Ledger M2). Both AC1 incident titles remain in unit **and** WireMock (Q5); later tasks reuse those cases rather than copying them. Then implement private adapter/mapping guard on the two production files; repair only forced fixtures with recorded reason; add WireMock/chain/request proof (Q5, Q7).
- **Guard placement (automatic-only entry points)**: Bind Type on Response/SearchHit mapping as needed; keep eligibility private/local in `OmdbMetadataProvider` (+ Mapping). **Classify/Map run before automatic eligibility** on both automatic direct `t=` and automatic fetched-detail `i=` paths: unusable/missing title retains `Failed(InvalidResponse)` + `MissingTitle` logging (no fallback/TMDb); only mapped Found rejected by the guard routes to fallback/NotFound (plan-challenge clarification 1). Apply the same guard at automatic direct and automatic fetched-detail entry points against Recleaned(stored); exclude rejected direct imdbID in BestCandidate. **Shared `Classify` / `FoundOutcome` / `FetchByIdAsync` MUST NOT unconditionally enforce automatic guards** — ManualImdbId sparse-metadata authority stays exempt (Sentry F2; Q2). Reuse TitleMatcher.Score unchanged (Q2–Q5).
- **Helper extraction**: Private helpers for complexity stay strictly inside `OmdbMetadataProvider.cs` and `OmdbMetadataProvider.Mapping.cs`; no new type/interface/sibling partial/layer. A blocked complexity gate needs a new Patron ruling, not silent allowlist expansion (plan-challenge clarification 3).
- **Refactor**: Cog runs `/refactor` once over the completed implementation (task-pipeline Phase 3 step 3). Code edits may be conditional when complexity/InspectCode already green; the refactor stage itself is required (Q7).
- **Verification**: Gauge full configured Phase 3 gates on task diff (including `dotnet format --verify-no-changes` per brief Q7 / task-pipeline Phase 3 step 5); then mandatory Q6 live AC2; canonical Phase 4 three-axis review with Keel adjudication and Cog remediation; canonical Phase 5 fresh rebased gates + hosted checks before awaiting-merge (Q7–Q8).

## Project Structure

```text
specs/DEV-447/
  brief.md, CONCLUSIONS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  checklists/requirements.md

src/LamuFlix.Infrastructure/Adapters/
  OmdbMetadataProvider.cs              (edit: automatic eligibility + rejected-ID exclusion + detail revalidation)
  OmdbMetadataProvider.Mapping.cs      (edit: bind Type where needed for Response/SearchHit)

# Explicitly unchanged production (among others):
#   TitleMatcher.cs, OmdbOptions.cs, TmdbMetadataProvider.cs, ChainedMetadataProvider.cs
#   Enrichment endpoints/handlers, OpenAPI/TS, web UI, schema

tests/LamuFlix.UnitTests/Adapters/
  OmdbMetadataProviderFallbackUnitTests.cs   (primary)
  OmdbMetadataProviderUnitTests.cs           (primary)
  TitleMatcherTests.cs                       (regression-run unchanged)

tests/LamuFlix.IntegrationTests/
  MetadataProviderLookupTests.cs             (primary)
  MetadataProviderChainCompositionTests.cs   (primary)
```

**Layout decisions**:

- No research/data-model/contracts artifacts — public contract chain untouched; decisions frozen in brief.
- Quill owns spec/plan/tasks/receipt/checklist. Quill does not edit `brief.md` / `CONCLUSIONS.md` / ADRs / constitution / production code. Keel owns analyze/plan challenge.
- Preserve and exclude unrelated `harness.yml` modification from this task's staging (Q5, Q7).

## Test Strategy

| Surface | Coverage |
|---|---|
| Ticket regressions (US3 / AC1) | Synthetic stubs in **unit and WireMock**: `Memento[2000]` wrong `tt24735934` → correct `tt0209144`; `O Brother, Where Art Thou[2000]` episode `tt24456948` → movie `tt0190590`; punctuation/year use; provenance in test naming/planning prose (no explanatory code comments). Not claimed incident causality. |
| Type filtering | Direct + search + fetched detail: movie vs episode/series/absent/unknown. Unit: existing HTTP-stub matrix in Fallback/UnitTests. WireMock: named cells in `MetadataProviderLookupTests` (no second stub approach). |
| Score / year | Non-null score at threshold accepted; below rejected; year veto and unknown-year preserved; mutation-killing boundary at exact threshold |
| Suspicious (Q4) | Each single usable rating/director/runtime vs all three absent/N/A/invalid under existing mapping; no universal rejection of eligible same-title/year wrong movies |
| Bounds | Valid direct = one request (green lock; repair `Detail`/direct fixtures with Type=movie + ≥1 usable Q4 field while keeping `Requests.Count==1`); finite fallback; at most one detail; rejected-ID exclusion; rejected mapped detail → NotFound; no restart |
| Chain | Separate cases: (a) valid automatic OMDb Found → zero TMDb requests (primary short-circuit preserved); (b) rejected mapped detail → NotFound → TMDb invoked. Do not invert (a) when repairing Type. |
| Classification preservation | Green preservation tests on **both** automatic direct `t=` and automatic fetched-detail `i=` paths: `Response=True` unusable/missing Title → `Failed(InvalidResponse)` + `MissingTitle` logging, no fallback/NotFound conversion, **no TMDb invocation**; existing direct invalid-response assertions stay intact (FR-004a) |
| Manual / failures | ManualImdbId exempt (green lock); transport/status/malformed-JSON/cancellation/rate-limit/quota preserved (green locks); API-key non-disclosure regressions remain green |
| Mapper edges | Primary-allowlist mapper tests may use ManualImdbId exemption or eligible automatic fixtures so identical mapper assertions remain; outside primary four files: payload-only documented repairs, zero assertion weakening |
| Red vs green | New-behavior cases red where feasible; existing manual/failure/one-request/classification preservation locks stay green (Compass F2) |
| Forced fixtures | Other existing OMDb automatic-success fixtures missing Type or Q4 fields: document file+reason before edit; do not weaken assertions |
| CI browser smoke (US4) | T021 and T022 exercise Match Manually on isolated movies awaiting correction. The OMDb stub supplies deterministic IMDb-ID metadata and Playwright serves the poster fixtures. Scope authority is the Phase B decision amendment in `CONCLUSIONS.md`. |
| Live AC2 | Q6 procedure under test-portal-policy after code gates; pending/blocked if environment missing |

No Gherkin. Property tests: reuse existing suite; no new framework or contrived invariant; exit-2 opt-out only with recorded task-note reason (Q5, Q7).

## Gates (after Gate 1 and implementation — Gauge owns configured Phase 3 gates)

Thresholds for build and refactor cyclomatic complexity come only from `harness.yml` (brief:96).

1. Explicit changed-C# scope: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness.yml, `./scripts/run-jetbrains-inspectcode.ps1` (never `-All`).
2. Affected unit/integration suites (including TitleMatcherTests regression), then full `dotnet test`.
3. Property / vulnerable-packages / format per harness; distinguish PASS, failure, SKIPPED (scope-empty), SKIP, mutation N/A, Could not run.
4. Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` (no `--since` from worktree) for changed production C#.
5. Gauge runs the applicable web gates and `pwsh -NoProfile -File ./scripts/run-e2e-smoke.ps1` for T021 and T022. Confirm both scenarios run in the hosted `e2e-smoke` job at the delivery head.
6. Mandatory live AC2 (Q6); Size M review (canonical Phase 4): Sentry, Ledger, Compass with Keel adjudication and Cog remediation; Q8 closing bar (Critical/High/Medium block; Low explicit disposition unless cited mandatory rule); two rounds / two fix commits per round max.
7. Fresh rebased gates + current-head review evidence + watched hosted checks before awaiting-merge (canonical Phase 5): DEV-447 (#n); user merges.

Phase A runs none of these and claims no gate result. Do not edit or stage the unrelated harness.yml change to pass gates.

## Risks

- **False negatives from fail-closed Type**: malformed/missing Type rejects automatic Found — accepted Q2 cost; ManualImdbId remains escape hatch.
- **Extra OMDb requests on rejected directs**: finite fallback cost accepted (Q1); bounded by existing variants + one detail (Q3; ADR 0031).
- **Rejected detail without lower-ranked retry**: may NotFound when a lower-ranked OMDb candidate would have been valid — accepted Q3 trade-off; chain may try TMDb.
- **Shared MatchThreshold left unchanged**: TMDb matching unchanged by design; do not "fix" threshold here (Q1, Q5; ADR 0030).
- **Q4 residual wrong-identity risk** (Sentry F1; plan-challenge clarification 2): a movie-typed same-title/same-year wrong IMDb id with ≥1 usable Q4 field (e.g. valid Runtime) remains eligible under Q4. Do not universally reject such records, add an ID denylist, or invent a new heuristic. AC1 stays mandatory synthetic selection proof and AC2 mandatory live remediation; neither proves unknown incident payload causality. Mitigate via Wisp nonsecret pickup observation when credentials exist (unknown when unavailable); observed mismatch stops for reanalysis.
- **Classification vs eligibility ordering**: if eligibility ran before Classify/Map, unusable-title bodies could flip from `Failed(InvalidResponse)` to fallback/NotFound and unlock TMDb — forbidden; Classify/Map precede the guard on automatic direct and fetched-detail paths (Compass F1; Sentry F4).
- **Shared-path ManualImdbId footgun**: putting automatic guards inside shared Classify/FoundOutcome/FetchByIdAsync would break sparse manual corrections — automatic-only entry points required (Sentry F2).
- **AC2 does not prove automatic selection**: mitigated by mandatory independent AC1 stubs (Q6).
- **Unavailable live environment**: AC2 pending/blocked, never silent pass (Q6).
- **Pickup drift**: mandatory Phase B recon/reanalysis before coding (Q7).
- **Fixture churn / short-circuit inversion**: forced Type repairs could invert "valid OMDb Found skips TMDb" into "OMDb miss → TMDb Found" — keep those cases separate (Sentry F3). Require file+reason record; no assertion weakening (Q5).
- **Complexity extraction pressure**: refactor gate may push for a third production file — extraction stays inside the two allowlisted files; unmet gate needs a new Patron ruling (Ledger M1; plan-challenge clarification 3).

## Ordering

See `tasks.md`. Numbered local phases below are **LOCAL `tasks.md` phase IDs** for `/implement Phase N` dispatch — **not** canonical `task-pipeline` phases. Canonical Build/review/delivery remain Phase 3 / Phase 4 / Phase 5 (brief:87–94; task-pipeline Phase 3 steps 2–6). Seat map: Wisp/Keel/Gauge Pickup (local Phase 1); Anvil code phases 2–4 only; Cog local Phase 5; Gauge local Phase 6; local Phases 8–9 ↔ canonical Phase 4/5.

```text
Phase A (Spec)     Quill draft → Keel analyze/challenge → Patron provisional Gate 1 → user merges Gate 1
Phase 0            T000 Keel Gate 1 merge receipt (blocks Build)
Phase 1            T001 Wisp drift recon → Keel reanalysis → Gauge baseline (blocks implementation)
Phase 2            T002 → T003 → T004 Anvil sequential regression/request proofs (new-behavior red where feasible; green locks; unit + MetadataProviderLookupTests AC1)
Phase 3            T005 → T006 → T007 Anvil guard on OmdbMetadataProvider (+ Mapping) + green AC1
Phase 4            T008 Anvil forced fixture repairs only as documented
Phase 5            T009 Cog /refactor once (required stage; code edits may be conditional) — canonical Phase 3 step 3
Phase 6            T010 → T016 Gauge configured gates — canonical Phase 3 steps 5–6
Phase 7            T017 Live AC2
Phase 8            T018 Sentry/Ledger/Compass; Keel adjudication; Cog remediation (cap 2) — canonical Phase 4
Phase 9            T019 → T020 rebased gates + hosted checks → awaiting-merge — canonical Phase 5
```
