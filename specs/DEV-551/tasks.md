---
description: "Task list for DEV-551 multi-provider subtitle acquisition seam"
---

# Tasks: Multi-provider subtitle acquisition seam

**Input**: Design documents from `specs/DEV-551/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q12), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `contracts/search-response.md`; Maestri notes `DEV-551`, `recon-DEV-551`; `specs/PRODUCT.md`.

**Status**: Draft (plan-challenge fix round applied)

**Tests**: Required (`brief.md` Test strategy; FR-001–FR-016; SC-001–SC-008). TDD optional. Use existing UnitTests / IntegrationTests / web homes. No new test framework/dependency.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence tasks are never `[P]`.

**Conventions**: Edit only the authorized envelope in `plan.md` / brief Planned file scope; STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` / `needs decision` if a required fact/decision is missing; no new NuGet/npm; never lower harness thresholds; no explanatory comments on changed C# beyond AGENTS exemptions. Otherwise-unassigned implementation tasks are owned by the generic Build lane. Named seats own Spec PR, pickup, refactor, gates, review, and ship per `task-pipeline`. Closing bar and caps: `brief.md` Q11–Q12 (single source). Round caps unchanged: two plan-challenge rounds and two implementation review rounds; at most two fix commits per implementation round (Q12).

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: After plan challenge clearance, Bernstein coordinates; Rigger prepares/opens the Spec Kit PR; only the user merges; Keel records Gate 1 merge receipt. Ordinary Gate 1 remains **separate** from ADR acceptance. No Build tasks (T001+) before Gate 1 and T030. ADR 0052 may ship Proposed on Spec PR; Accept is **first in the delivery task sequence after Gate 1** and the before-deliverable-merge bar is retained (Q11; brief.md:19).

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger open; user merges; Keel Gate 1) — blocks T030+; Gate 1 separate from ADR Accept
Phase 1   T030          (ADR Accept — first in delivery task sequence after Gate 1; before-deliverable-merge bar retained)
Phase 2   T001          (Wisp pickup recon + DEV-525 Done verify; Keel reanalysis) — blocks production edits; DEV-525 before Build
Phase 3   T002–T007, T017–T019
            Foundational: Core port + Core-side lookup, options, outside-Core typed-client composition,
            request policy, OpenSubtitles adapt, green typed-client contract suite — before fan-out
Phase 4   T013          (US2 binder: ranked batch binding) — before search issues Candidate IDs or claims parity
Phase 5   T008–T012     (US1 aggregate search + additive API) — after binder; no generate:api yet
Phase 6   T014–T016     (US2 download routing + full Q7 proofs) — after binder + search binding path
Phase 7   T020–T022     (US4 registration/spacing/config honesty) — after catalog/options
Phase 8   T023–T025     (US5 per-provider health) — after search bookkeeping shared path
Phase 9   T026–T029     (US6 glossary, setup docs, OpenAPI/TS) — after T018 adapter-contract receipt AND T010 endpoint-parity/isolation receipt with complete wire assertions
Phase 10  T031–T032     (Polish: exact-head / ship-review split / rebased-head gates, Size L review) — after all in-scope receipts incl. T015/T024/T028; ADR already Accepted in Phase 1
```

Ordering per `brief.md` Approach: Gate 1 separate; ADR Accept first in delivery task sequence after Gate 1 (before-deliverable-merge bar retained); DEV-525 Done before Build; Core-side lookup + outside-Core typed-client composition + OpenSubtitles adapt + green shared suite before fan-out; ranked batch binding before search issues Candidate IDs or claims parity; T018 adapter contract proof and T010 endpoint parity plus isolation (complete wire assertions including request-level branch and simultaneous-fault first-match) before T026 generate:api; Phase 10 waits for all frozen story/proof receipts (T015, T024, T028 included); ship-review split and rebased-head gate verification for Size L.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1+)

**Purpose**: Produce/open the Spec Kit PR, then confirm ordinary owner Gate 1 merge before delivery tasks. Gate 1 is separate from ADR acceptance.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No delivery/Build tasks before that receipt.

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-551 Spec Kit PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 Spec Kit PR merge receipt for DEV-551 in `specs/DEV-551/CONCLUSIONS.md`. Receipt only. T030–T032 MUST NOT proceed without verified merge (`brief.md` Approach; no structural owner checkbox on this ticket). ADR 0052 may remain Proposed on Spec PR; Accept is first in the delivery task sequence after Gate 1 (T030; Q11; brief.md:19).

---

## Phase 1: ADR acceptance (first in delivery task sequence after Gate 1)

**Purpose**: Accept ADR 0052 immediately after separate Gate 1, before pickup/Build. Retains the before-deliverable-merge bar (Q11).
**Checkpoint**: ADR 0052 status Accepted; ADR 0041 intact with precedence in 0052.
**Order**: After T000. Before T001 and all Build tasks.

- [ ] T030 **Build lane / Keel**. After Gate 1 (T000) and **before** pickup/Build (T001+): Record ADR 0052 **Accepted** (Keel-owned file); leave ADR 0041 intact with precedence documented in 0052 (FR-014; Q10–Q11; brief.md:19 — ADR acceptance first in delivery task sequence after separate Gate 1; before-deliverable-merge bar retained). Spec PR may have carried Proposed only. This task does not substitute for Gate 1 and is not deferred to final polish.

**Checkpoint**: ADR Accepted; unlocks T001 (DEV-525 verify + pickup) then Build.

---

## Phase 2: Pickup recon and DEV-525 verify (blocks production edits)

**Purpose**: Inventory exact current seam paths on pickup HEAD; verify DEV-525 Done before Build; recheck DEV-550 serialization on shared registration file. Read-only.
**Checkpoint**: Written pickup receipt cites authorized paths; DEV-525 Done verified; Keel read-only reanalysis clear.

- [ ] T001 After T030: **Wisp** obtains Phase B pickup recon in the Build worktree inventorying (1) `src/LamuFlix.Core/Ports/ISubtitleProvider.cs` and related `SubtitleSearch` / `SubtitleCandidate` / result types, (2) `SearchSubtitlesQueryHandler`, `DownloadSubtitleCommandHandler`, `SubtitleLookup`, `SubtitleCandidateBinder`, (3) `SubtitlesOptions` / `SubtitleProviderOptions` / validators and `HandlerRegistration.cs` subtitle wiring, (4) `OpenSubtitlesSubtitleProvider`, `SubtitleProviderServiceCollectionExtensions`, `SubtitleRequestUriPolicy`, `SubtitleProviderHealthCheck`, (5) `SubtitleEndpoints` / `SubtitleSearchResponse` and ProblemDetails mapping sites, (6) existing IntegrationTests/UnitTests subtitle surfaces including `OpenSubtitlesSubtitleProviderTests` and `SubtitleSetupDocumentationTests`, (7) README subtitle setup sections and `CONTEXT.md` glossary anchors, (8) ADR 0052 Accepted status and ADR 0041 intactness, (9) serialization touch on `SubtitleProviderServiceCollectionExtensions.cs` vs DEV-550, (10) **verify DEV-525 is Done** (status only; no runtime claim), (11) **concrete reuse seams** for existing Enumeration base/derived types at the Core subtitle outcome boundary, JSON `Name` conversion in the approved API DTO scope, and the single centralized ProblemDetails/error-mapping handler (Principle V; Ledger S2/S3) — record concrete touch-point paths in the pickup receipt. **Keel** reanalyzes brief/spec/plan/tasks against pickup HEAD **read-only**. **STOP** `needs recon:` on missing seams; **STOP** for a cited **Q10 Patron file ruling** before any edit outside the approved envelope (including if Enumeration/JSON Name/central-handler reuse requires an unnamed tracked file). **Strictly read-only reconnaissance and analysis**.

**Checkpoint**: T002+ unlocks only with a passing Wisp receipt, DEV-525 Done verification, and Keel pickup analysis clear.

---

## Phase 3: Foundational — Core lookup, policy, OpenSubtitles adapt, shared contract suite

**Purpose**: Typed port extensions, Core-side ordered provider lookup (host-lifetime spacing/observation state), per-adapter validation under ValidateOnStart exception, outside-Core typed HTTP client composition, outbound request policy (dual HTTP opt-in; response-independent destinations; redacted warnings), OpenSubtitles adapter adapt, and green shared WireMock contract suite — before fan-out/download wiring (`brief.md` Approach; Constitution I; plan-challenge H1–M3/S4).
**Independent Test**: Core lookup resolves by provider id; unsafe URL marks provider unusable without host abort (redacted warning); four independent typed-client HTTP gates proven (permit credential-free+opt-in; refuse missing opt-in; refuse missing credential-free declaration despite opt-in; refuse credential/login despite both); OpenSubtitles passes shared suite through real typed client; existing `OpenSubtitlesSubtitleProviderTests` green; no fan-out required yet.
**Order**: After T001. Blocks search/download wiring that depends on these types. OpenSubtitles adapter work and green typed-client contract proof sit in this foundation path.

- [ ] T002 **Build lane**. After T001: Extend `src/LamuFlix.Core/Ports/ISubtitleProvider.cs`, `SubtitleSearch.cs`, `SubtitleCandidate.cs` (and related result types) with provider identity, declared search capabilities, opaque file reference, and provider outcomes per Q5/Q8/Q10 (FR-006, FR-009). No new port/layer. Preserve ArchitectureTests port naming contract.
- [ ] T003 [P] **Build lane**. After T001: Add/extend typed options under `src/LamuFlix.Core/Options/` for adapter-local `Enabled`, existing `RequestTimeout` validation (`> TimeSpan.Zero`), `MinimumRequestGap` (`>= TimeSpan.Zero`, no invented positive safety default), credentials, credential-free **plain-HTTP opt-in** (default disabled), and `Subtitles:ProviderOrder` on the appropriate options record; preserve `Subtitles:OpenSubtitles` section path; validators document valid ranges (FR-009; Q8; Patron S5). Honour Constitution VII subtitle acquisition **ValidateOnStart exception**: invalid timing / missing credentials classify provider-local off; unsafe URL classifies unusable; never abort host startup (FR-002; M3/S1). Unknown/duplicate ProviderOrder → safe warning, ignore, first wins, never startup failure.
- [ ] T004 **Build lane**. After T002–T003: Implement one ordered concrete provider **lookup on the Core side** of the existing seam under `src/LamuFlix.Core/Features/Subtitles/` shared by search, download, SubtitleLookup, and health (FR-009; Q8; Constitution I — not a new port/layer). The lookup **owns** process-local per-provider spacing and health-observation state as **one instance-owned state object per provider for the host lifetime** (Ledger S4). Keep named typed HTTP client registration and composition **outside** Core in `src/LamuFlix.Infrastructure/Adapters/SubtitleProviderServiceCollectionExtensions.cs` and `src/LamuFlix.Api/HandlerRegistration.cs`. Serialize with DEV-550 if that branch also edits the Infrastructure registration file. Not `[P]` with T005 on the same registration/policy files.
- [ ] T005 **Build lane**. After T002: Adapt `src/LamuFlix.Infrastructure/Adapters/SubtitleRequestUriPolicy.cs` (and OpenSubtitles typed-client handler configuration) so each client enforces its allow-list from **adapter declarations or explicit typed configuration independently of upstream responses** on the initial URL and every redirect hop **before I/O**; disable automatic redirect following; validate each hop; credentials only to HTTPS search origin; refuse cross-host redirect and HTTPS-to-HTTP downgrade; plain HTTP requires **both** a credential-free adapter declaration **and** explicit typed opt-in (default disabled) and is refused when credentials/login are used or configured; OpenSubtitles remains HTTPS-only (FR-005; Q4; H1/M1). Startup HTTP refusal remains scoped to unsafe URLs (Q2).
- [ ] T006 **Build lane**. After T004–T005: Add the shared abstract WireMock contract suite under `tests/LamuFlix.IntegrationTests/` exercising declared search kinds, in-cap SRT download, oversize/non-SRT rejection, 429, timeout, 5xx, cross-host redirect refusal, HTTPS downgrade refusal, credential-header confinement to HTTPS search origin, dual opt-in plain HTTP as **four independent typed-client gates** — (1) **permit** credential-free declaration **plus** explicit opt-in, (2) **refuse** missing opt-in, (3) **refuse** missing credential-free declaration despite opt-in, (4) **refuse** credential/login configuration despite both opt-ins; OpenSubtitles HTTPS-only — refusal of disallowed initial URL and redirect **before any contact**, and unsafe base URL startup refusal with host/local-track continuity and a **redacted** warning (provider name + `unsafe_base_url` only; no raw URL/credentials/query) (FR-005/FR-013; Test strategy; H1/M1/M2; CHK029). Suite runs through each adapter’s real typed HttpClient.
- [ ] T017 [US3] **Build lane**. After T006 and T002: Adapt `src/LamuFlix.Infrastructure/Adapters/OpenSubtitlesSubtitleProvider.cs` to the extended port (identity, capabilities, opaque file reference, outcomes) while preserving existing behaviour covered by `OpenSubtitlesSubtitleProviderTests` (FR-013). Foundation-path OpenSubtitles adapter work.
- [ ] T018 [US3] **Build lane**. After T017: **Adapter contract proof** — run shared contract suite against OpenSubtitles and ensure `tests/LamuFlix.IntegrationTests/OpenSubtitlesSubtitleProviderTests.cs` remain green (SC-004). Green typed-client contract proof required in foundation path before fan-out claims. Record the adapter-contract receipt T026 waits on.
- [ ] T019 **Gauge checkpoint**. After T018: Static gates on changed adapter `.cs`; record SC-004 adapter-contract receipt.
- [ ] T007 **Gauge checkpoint**. After T006 and T019: Run the three static gates on this phase’s changed `.cs` and the focused suite/composition tests proving Core lookup + outside-Core typed-client composition + policy + OpenSubtitles green contract-suite foundation. Record verdicts (keep SKIPPED/N/A/Could not run distinct from PASS).

**Checkpoint**: Foundation ready — ranked binder, then fan-out and download binding may proceed.

---

## Phase 4: Ranked batch binding before search issuance (US2 binder half)

**Goal**: Ranked batch binding boundary is wired before the search path issues Candidate IDs or claims parity.
**Independent Test**: Over-capacity highest-ranked-only issuance; every returned token resolves immediately; no self-evicted publish; A/B reference isolation at bind time; bounded/provider-bound property invariant foundation.
**Order**: After T007. Blocks T008+.

- [ ] T013 [US2] **Build lane**. After T007: Adapt `src/LamuFlix.Core/Features/Subtitles/SubtitleCandidateBinder.cs` for provider-bound opaque file references, A/B collision isolation, ranked batch binding (never publish self-evicted tokens), and over-capacity highest-ranked-only issuance with safe truncation log (FR-008; Q7; US2 scenario 4). **Over-capacity assertion**: every Candidate ID returned by that search resolves immediately (not already stale/evicted by the same search). Keep 1000-entry and five-minute defaults. Must complete before search issues Candidate IDs or claims parity.

**Checkpoint**: Binder ready for search issuance.

---

## Phase 5: User Story 1 - Aggregate multi-provider search (Priority: P1) 🎯 MVP

**Goal**: One aggregate search per language fans out concurrently, merges in Q6 order, returns additive outcomes with legacy compatibility; request-time 409 on search with zero paid calls.
**Independent Test**: SC-001/SC-002; multi-provider one usable + off/refused → 200; all unusable → search 409 with zero paid calls and ordered `providers` present; request-level existing errors with `providers` absent; simultaneous-fault first-match; zero-candidate multi-provider 200; one-provider legacy including any baseline non-200 settled outcome; missing key zero requests; discriminating Q2 usability-freeze fixture (multi-provider; only config-usable provider settles timeout / search 429 / failure / no formable input with off/refused sibling → 200, empty candidates, ordered named outcomes in each case); Q5 missing-earlier-ID-with-later-available / no-formable-input-at-all / stop-on-timeout-quota-failure / search-429 vs download-exhaustion distinction (search half).
**Order**: After T013 (binder) and T007.

- [ ] T008 [US1] **Build lane**. After T013 and T007: Adapt `src/LamuFlix.Core/Features/Subtitles/SearchSubtitlesQueryHandler.cs` (and helpers as needed) for concurrent per-provider search under independent timeouts; Q5 capability walk including skip unavailable identifiers and try next available input, no formable input at all → `failed`/`no_search_input`, empty-only fallback, stop-on-timeout/quota/failure, spacing, cancellation≠timeout, and search-429 → provider quota without inventing download quota; Q6 stable merge (`ASSUMPTIONS.md`); per-provider outcomes including off rows; Constitution VII request-time acquisition validation with sole missing/invalid **search** 409 and zero paid calls (FR-001, FR-002, FR-006, FR-007; Q1/Q2/Q5/Q6). Reuse Core-side provider lookup and centralized error handling. Issue Candidate IDs only through the T013 ranked binder.
- [ ] T009 [US1] **Build lane**. After T008: Add additive search DTO fields and ProblemDetails extension key `providers` in `src/LamuFlix.Api/Endpoints/SubtitleSearchResponse.cs` and `SubtitleEndpoints.cs` per `contracts/search-response.md` (FR-003–FR-004; Q3; M4 search-only). Closed `state` and `reason` MUST be Enumerations (or equivalent) at the **Core subtitle outcome boundary** with JSON `Name` conversion in the approved API DTO scope (Patron S5 vocabulary; Ledger S2); preserve legacy reason types/mappings. Attach settled-search `providers` data **only** through the existing **single centralized** ProblemDetails handler (Principle V; Ledger S3); do not introduce a second error carrier. Preserve unchanged legacy request/response semantics and legacy quota/availability projection rules without restating unverified legacy field tables. Do **not** regenerate OpenAPI/types yet (brief: prove OpenSubtitles adapter contract and endpoint parity/isolation first).
- [ ] T010 [US1] **Build lane**. After T009: **Endpoint parity plus isolation (search)** — IntegrationTests under `tests/LamuFlix.IntegrationTests/` proving (1) OpenSubtitles-only **one-provider** legacy parity including **each** legacy quota and download-availability projection **and** any baseline **non-200** settled legacy outcome against baseline without guessing status (SC-001; unchanged legacy contract), (2) two-provider concurrent fan-out/isolation (SC-002), (3) multi-provider one usable plus off/refused → 200, (4) all unusable / sole-unusable **search** 409 with zero paid calls **and** ProblemDetails `providers` present with ordered classified off/unusable rows (quota null when unobserved) (SC-005 search; Constitution VII; brief.md:69), (5) multi-usable zero-candidate 200, (6) missing-key zero requests, (7) exclusive `state`/`reason` wire pairs under pre-I/O precedence (disabled → `off`/`off`; else unsafe URL → `unusable`/`unsafe_base_url`; else missing/invalid credentials/timeout/gap → `off`/`provider_not_configured`) including **simultaneous-fault first-match** cases plus request-time reasons, (8) **request-level existing error branch** with ProblemDetails `providers` key **absent**, (9) legacy search errors with settled outcomes carry `providers`, and (10) **discriminating Q2 usability-freeze fixture**: multi-provider configuration whose **only** config-usable provider settles with timeout, search 429, failure, or no formable input, with an off/refused sibling — for **each** of those four settlements assert **200**, **empty candidates**, and ordered named `providers` outcomes (not Q2 409) (FR-001–FR-004; `contracts/search-response.md`; brief.md:69). Download 409 / read-only precedence are **not** in T010 (assigned to T015). Record the endpoint-parity/isolation receipt with complete **search** wire assertions that T026 waits on (must include request-level branch, simultaneous-fault first-match, Q2-409 providers presence/order, request-level providers absence, and the discriminating usability-freeze fixture for all four settlements).
- [ ] T011 [US1] **Build lane**. After T010: UnitTests under `tests/LamuFlix.UnitTests/Features/Subtitles/` proving (a) missing earlier identifier with later available input → skip and try next (not terminal), (b) no formable input at all → safe provider-local `failed`/`no_search_input`, (c) capability priority, empty-only fallback, stop-on-timeout/quota/failure, spacing via TimeProvider, cancellation ≠ timeout, search 429 vs known download exhaustion distinction (search half), and Q6 ordering stability (FR-006–FR-007).
- [ ] T012 **Gauge checkpoint**. After T011: Static gates on this phase’s changed `.cs`; record US1 acceptance receipts (SC-001, SC-002, SC-005 search portions) and the T010 endpoint-parity/isolation wire-assertion receipt.

**Checkpoint**: Aggregate search behaviour proven; unlocks API generation only after T018 adapter-contract receipt and this T010 endpoint-parity/isolation receipt with complete wire assertions.

---

## Phase 6: User Story 2 - Provider-bound download (Priority: P1)

**Goal**: Download routes only to bound provider; bound-provider acquisition 409 after read-only precedence (even with a usable sibling) with zero paid calls to either provider; full Q7 ranked issuance proofs including US2 scenarios 4–6 independently of expiry.
**Independent Test**: SC-003; bound-provider download 409 with usable sibling + zero calls to either provider; legacy status/ProblemDetails-without-`providers` on 409, read-only, and stale/invalid paths; every returned over-capacity token resolves immediately; earlier token evicted by a later search → existing stale response with zero paid calls; ranked issuance across providers and preferred languages at full capacity; expiry with injected time and deterministic randomness; explicit bounded/provider-bound property invariant.
**Order**: After T013 and T008 (bound identity path exists).

- [ ] T014 [US2] **Build lane**. After T013 and T008: Adapt `DownloadSubtitleCommandHandler.cs` and `SubtitleLookup.cs` to resolve via Core-side lookup by bound provider identity only; block cross-provider/cross-movie misuse before paid call; apply Constitution VII **download 409 predicate** — after ADR 0041 read-only precedence, validate **only the bound provider’s** acquisition configuration and return sole missing/invalid **download** 409 when that bound provider is missing/invalid/unusable **even if a sibling remains usable**, with zero paid calls to either provider (distinct from the search all-enabled-providers predicate); keep download exception/status mapping on the **single centralized** ProblemDetails handler (Principle V; Ledger S3; no `providers` extension on download); preserve publication safeguards/library policy; enforce known download-quota exhaustion only on otherwise valid candidates (FR-002, FR-004, FR-008; Q2/Q4/Q5/Q7; Ledger S1; ADR 0052; M4).
- [ ] T015 [US2] **Build lane**. After T014: Unit/Integration/property tests proving B-only download, A/B reference isolation, cross-provider/cross-movie zero paid calls, **download endpoint integration proof** of the **bound-provider** acquisition **409** case (bound provider missing/invalid/unusable **with a usable sibling**) with **zero paid calls to either provider**, ADR 0041 read-only precedence **before** acquisition validation, and **legacy HTTP status plus ProblemDetails field assertions without `providers`** on acquisition 409, read-only, and stale/invalid paths (wire assertions for absence of the search-only extension and for unchanged legacy fields) (SC-005 download; Constitution VII; plan Verification; Ledger S1; ADR 0052; M4 — not T010), known download-quota exhaustion on otherwise valid candidates, **and** the following Q7 proofs covering US2 scenarios 4–6 **independently of expiry**: (a) over-capacity — every returned token resolves immediately and no self-evicted publish; (b) an earlier token evicted by a later search takes the existing stale response with zero paid calls; (c) ranked issuance across providers and preferred languages at full capacity; (d) expiry with injected time and deterministic randomness as its own case; (e) explicit property coverage for the bounded/provider-bound Candidate ID invariant (SC-003; FR-008; brief.md Test strategy).
- [ ] T016 **Gauge checkpoint**. After T015: Static gates on this phase’s changed `.cs`; record SC-003, SC-005 bound-provider download 409, US2 scenarios 8–9, and US2 scenario 4–6 receipts.

**Checkpoint**: Provider-bound download and Q7 proofs complete.

---

## Phase 7: User Story 4 - Registration, spacing, configuration honesty (Priority: P2)

**Goal**: ProviderOrder, enablement, spacing, and missing-key off behaviour are composition-proven and documented in tests.
**Independent Test**: Composition tests for order/warnings/spacing; SC-005 key half.
**Order**: After T003–T004; coordinates with US1 proofs.

- [ ] T020 [US4] **Build lane**. After T007: Extend composition tests (e.g. `SubtitleAcquisitionOptionsCompositionTests`) for ordered Core-side lookup enumeration, ProviderOrder unknown/duplicate warnings, Enabled/off classification, and MinimumRequestGap spacing across concurrent requests with cancellation honour on **host-lifetime instance-owned** provider state (FR-009; Q8; Ledger S4). Cover **invalid timing** classification: host and existing-track continuity, off row for the invalid provider, zero calls to it, and usable-sibling search 200 (ValidateOnStart exception; M3/S1).
- [ ] T021 [US4] **Build lane**. After T020: Prove missing required key → provider off + zero requests when another provider is usable; unsafe URL unusable without host abort already covered by suite; reinforce invalid-timing continuity/usable-sibling proof from T020 — record SC-005 (FR-002; M3).
- [ ] T022 **Gauge checkpoint**. After T021: Static gates on any new/changed `.cs`; record US4 receipts.

**Checkpoint**: Config honesty proven.

---

## Phase 8: User Story 5 - Per-provider health (Priority: P2)

**Goal**: Health reports per-provider enabled/configured/last outcome without network calls or readiness Unhealthy on provider faults.
**Independent Test**: SC-006.
**Order**: After search path bookkeeping exists (T008).

- [ ] T023 [US5] **Build lane**. After T008 and T004: Adapt `src/LamuFlix.Infrastructure/Adapters/SubtitleProviderHealthCheck.cs` for per registered provider rows in configured order; **read** the Core-side process-local observation state owned by the lookup (one instance per provider for the host lifetime, shared with search); last completed wins; omit cancellations; TimeProvider timestamps; no network calls; no secrets/raw errors; readiness never Unhealthy for off/failed; at most Degraded on non-readiness; reset unobserved on restart (FR-010; Q9; Ledger S4). Typed-client composition remains outside Core.
- [ ] T024 [US5] **Build lane**. After T023: Health tests covering unobserved/reset, safe last settlement, cancellation exclusion, zero health-network calls, readiness continuity, and **search/health sharing** of the same host-lifetime instance-owned observation state (SC-006; S4).
- [ ] T025 **Gauge checkpoint**. After T024: Static gates on changed health `.cs`; record SC-006.

**Checkpoint**: Health observations complete.

---

## Phase 9: User Story 6 - Glossary, setup docs, generated contracts (Priority: P3)

**Goal**: CONTEXT.md Subtitle Provider; setup docs/tests; OpenAPI/types regeneration after T018 adapter-contract and T010 endpoint-parity/isolation receipts with complete wire assertions; web tests green.
**Independent Test**: SC-007 including explicit `SubtitleSetupDocumentationTests`.
**Order**: After T018 (adapter contract proof) and T010 (endpoint parity plus isolation with complete wire assertions).

- [ ] T026 [US6] **Build lane**. After T018 and T010: Regenerate `web/src/api/openapi.json` and `web/src/api/types.ts` from C# contracts; run drift checks plus wire-level and generated-schema coverage per `contracts/search-response.md` (FR-012; Q3/Q10). **Search-only** generation/proof: Explicit dependency on completed **T018 adapter-contract receipt** AND completed **T010 search endpoint-parity/isolation receipt with the complete search wire assertions** T026 claims (legacy projections including baseline non-200 settled outcome, **request-level existing error branch** with `providers` absent, simultaneous-fault first-match cases, one usable + off/refused, all unusable search 409 with ordered `providers` present, multi-usable, exclusive state/reason precedence, ProblemDetails `providers` on settled **search** errors, and **discriminating Q2 usability-freeze fixture** — multi-provider, only config-usable provider settles timeout / search 429 / failure / no formable input with off/refused sibling, asserting 200 + empty candidates + ordered named outcomes in each case). Download 409 / unchanged download ProblemDetails proof remains on T015 and is **not** a T026 dependency (M4).
- [ ] T027 [P] [US6] **Build lane**. After T001: Add Subtitle Provider to `CONTEXT.md` per brief glossary definition without rewriting Movie/Library/sidecar vocabulary (FR-011; Q10).
- [ ] T028 [US6] **Build lane**. After T003: Update existing subtitle setup documentation (README subtitle sections) and **explicitly run** `tests/LamuFlix.UnitTests/SubtitleSetupDocumentationTests.cs` for named settings (Patron S5 `Enabled` / `MinimumRequestGap`, dual HTTP opt-in default-disabled, ValidateOnStart acquisition exception behaviour), valid ranges, provisioning order, key/enablement behaviour; credential examples by configuration key or placeholder only (FR-011; SC-007; H1/S5).
- [ ] T029 [US6] **Build lane**. After T026: Run all existing web tests and require green (SC-007; FR-012).

**Checkpoint**: Docs and generated contracts complete.

---

## Phase 10: Exact-head / ship-review gates, Size L review

**Purpose**: Exact-head gates, ship-review split, rebased-head verification, Size L review under Q11–Q12 caps. ADR already Accepted in Phase 1; before-deliverable-merge bar remains in force.
**Order**: After **all** in-scope story and proof receipts are complete (including T015 download integration, T024 health, T028 setup documentation, T018/T010/T026 contract chain, and T029 web green); re-prove after any refactor/fix commits.

- [ ] T031 **Gauge**. After **all in-scope receipts** — T015 (download 409 / Q7), T024 (health), T028 (`SubtitleSetupDocumentationTests`), T018+T010+T026 (adapter + search wire + generate:api), T029 (web green), T030 (ADR Accepted), and any other frozen US receipts — **and** any refactor: Exact-head Roslyn, cyclomatic complexity (incl. refactor threshold), InspectCode, property tests, vulnerable packages, format, full `dotnet test` (incl. ArchitectureTests), mutation when applicable, web lint/test/build + drift. Perform Size L **ship-review split** and **rebased-head gate verification** at the exact reviewed PR head (`brief.md` Gate expectations). Keep SKIPPED/N/A/Could not run distinct from PASS. Confirm ADR 0052 remains Accepted (T030 already done; before-deliverable-merge bar). T028 setup-documentation proof is **inside** this dependency chain (not optional).
- [ ] T032 **Size L review**. After T031: Risk (Sentry), Standards (Ledger), Spec (Compass) with explicit dispositions under Q11 closing bar; max two implementation review rounds and two fix commits per round (Q12 — existing round caps preserved). Unresolved blockers carry to user on the PR. Owner merges. Plan-challenge closure and Gate 1 remain separate already-closed prerequisites. Ship-review split evidence from T031 is required input.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0**: Blocks all delivery work (Gate 1; separate from ADR Accept).
- **Phase 1**: ADR Accept first in delivery task sequence after Gate 1; before-deliverable-merge bar retained.
- **Phase 2**: Blocks production edits; DEV-525 Done verify before Build.
- **Phase 3**: Blocks fan-out/download/health wiring; includes OpenSubtitles adapt + green suite (T017–T019); T018 = adapter contract proof.
- **Phase 4**: Ranked binder before search issuance.
- **Phase 5 (US1)**: After binder + foundation; T010 = endpoint parity plus isolation with complete wire assertions.
- **Phase 6 (US2 download)**: After binder + search path; bound-provider download 409 (even with usable sibling; zero calls to either; legacy ProblemDetails without `providers` on 409/read-only/stale-invalid) + Q7 proofs (US2 scenarios 4–6 independently of expiry; scenarios 8–9).
- **Phases 7–8 (P2)**: After catalog/options and search bookkeeping.
- **Phase 9 (P3)**: OpenAPI/types after T018 adapter-contract receipt + T010 endpoint-parity/isolation receipt with complete wire assertions (incl. request-level branch, simultaneous-fault first-match, Q2-409 providers presence); glossary may proceed after T001; setup docs after options exist; explicitly run `SubtitleSetupDocumentationTests`.
- **Phase 10**: Exact-head / ship-review split / rebased-head gates + Size L review only after **all** in-scope receipts including T015, T024, and T028 (ADR already Accepted).

### User Story Dependencies

- **US1**: After foundational Phase 3 and ranked binder Phase 4.
- **US2 binder**: Before US1 issuance/parity claims.
- **US2 download**: After binder + search binding path.
- **US3**: Inside foundation (OpenSubtitles adapt + green suite / T018 adapter contract proof).
- **US4**: After options/catalog; complements US1 missing-key proofs.
- **US5**: After search bookkeeping shared with health.
- **US6**: OpenAPI/types after T018 adapter-contract and T010 endpoint-parity/isolation receipts; web green after generate; `SubtitleSetupDocumentationTests` explicit.

### Parallel Opportunities

- T003 options can proceed in parallel with T002 port after T001.
- T027 CONTEXT.md after T001 is `[P]` with unrelated Build lanes.
- US4 composition tests and US5 health can proceed on separate files once prerequisites exist.
- Do **not** mark same-file handler/binder/registration edits `[P]`.

---

## Parallel Example: Foundational options + port

```text
Task: "Extend ISubtitleProvider / SubtitleSearch / SubtitleCandidate in src/LamuFlix.Core/Ports/"
Task: "Add Enabled / MinimumRequestGap / ProviderOrder options under src/LamuFlix.Core/Options/"
```

---

## Implementation Strategy

### MVP First (foundation + binder + User Story 1)

1. Gate 1 + ADR Accept (first in delivery sequence) + pickup + DEV-525 Done
2. Core port/lookup + outside-Core typed-client composition + policy + OpenSubtitles adapt + green shared suite (T018 adapter contract proof)
3. Ranked batch binder (every returned token resolves immediately)
4. Aggregate search + additive DTO (without generate:api yet)
5. STOP and validate SC-001/SC-002 via T010 endpoint parity plus isolation

### Incremental Delivery

1. US2 download isolation + Q7 proofs (scenarios 4–6 independently of expiry; bounded/provider-bound property invariant)
2. US4/US5 config + health
3. US6 generate:api after T018+T010 receipts (complete wire assertions incl. request-level / simultaneous-fault / Q2-409 providers) + docs + explicit SubtitleSetupDocumentationTests + web green
4. Exact-head / ship-review split / rebased-head gates + Size L review only after all in-scope receipts including T015/T024/T028 (ADR already Accepted; before-deliverable-merge bar)

### Notes

- Confirm DEV-525 Done at T001 before Build edits.
- ADR Accept (T030) is first in delivery task sequence after Gate 1 — not deferred to final polish.
- OpenSubtitles adapter + green typed-client suite sit in foundation (T017–T018 adapter contract proof).
- Ranked batch binding (T013) before search issues Candidate IDs or claims parity.
- T026 depends explicitly on completed T018 adapter-contract receipt and T010 endpoint-parity/isolation receipt with complete wire assertions (request-level branch, simultaneous-fault first-match, Q2-409 providers presence/order, request-level providers absence).
- T031/T032 wait for all frozen story and proof receipts including T015 download integration, T024 health, and T028 setup documentation.
- Preserve existing OpenSubtitles assertions and all web tests; reference unchanged legacy contract (no unverified legacy field tables).
- Closing bar / caps: `brief.md` Q11–Q12 only (existing round caps preserved).
- Q6 assumed ordering remains in `ASSUMPTIONS.md`.
- Quill invents no decisions; gaps → `needs decision:` in `DRAFTING_RECEIPT.md`.
