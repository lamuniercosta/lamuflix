---
description: "Task list for DEV-557 OMDb and TMDb metadata-provider contract suite"
---

# Tasks: OMDb and TMDb metadata-provider contract suite

**Input**: Design documents from `specs/DEV-557/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q7), `CONCLUSIONS.md`; Maestri notes `DEV-557`, `recon-DEV-557`; `specs/PRODUCT.md`.

**Status**: Draft — Quill drafting only. Gate 1 / T000, Phase B, Build, and runtime receipts remain pending. Quill does not declare PLAN FROZEN. **Phase B / T001+ Build edits are blocked until DEV-551 implementation merges to main** (Q1).

**Tests**: Required (`brief.md` Test strategy; FR-001–FR-009; SC-001–SC-005). IntegrationTests only (WireMock + real DI). TDD optional. No new test framework/dependency. Fake WireMock keys only.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence tasks are never `[P]`. **Story labels (Ledger L4 / plan challenge item 12):** T000–T004 and T007 are cross-story foundational or gate tasks and carry no story label; T005 and T006 are US1 work placed in local Phase 2 for ordering. No renumbering.

**Conventions**: Edit only the authorized envelope in `plan.md` / brief Plan decisions; STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` / `needs decision` if a required fact/decision is missing; never invent harness type names before Phase B pin; never lower harness thresholds; no explanatory comments on changed C# beyond AGENTS exemptions. Every commit message starts `DEV-557 - {subject}` (P11). **Static-analysis completion**: mark each new or materially changed C# task complete only after scoped evidence that `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, and `./scripts/run-jetbrains-inspectcode.ps1` pass on that task's changed `.cs` (no `-All`). Otherwise-unassigned implementation tasks are owned by the generic Build lane. Named seats own Spec PR, pickup, refactor, gates, review, and ship per `task-pipeline`. Closing bar and caps: `brief.md` Closing bar (single source). Two review/remediation rounds max.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: After plan challenge clearance, Bernstein coordinates; Rigger prepares/opens the Spec Kit PR; only the user merges; Keel records Gate 1 merge receipt. No Build tasks (T001+) before Gate 1. Phase B additionally waits for DEV-551 implementation on main.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger open; user merges; Keel Gate 1) — blocks T001+
Phase 1   T001–T002     (Phase B: wait DEV-551 merge; drift check + harness name pin; Stryker/line before) — blocks production edits
Phase 2   T003–T007     (Foundational: harness consume/extract; contract base covering Wanted 1 + draft-inventory extras; OMDb+TMDb fixtures green) — before twin removal
Phase 3   T008–T011     (US2: final twin inventory; unmatched twin → add case + re-green; remove twins family by family; Q7 if needed)
Phase 4   T012–T014     (US3: Stryker/line after; exact-head gates; Size M review)
```

Ordering per `brief.md` Task ordering constraints / Q1 / Q5 / Q7.

---

## Twin inventory seed (draft at T001 as test-name lists; complete at T008 → `specs/DEV-557/twin-inventory.md`; `pr-body-DEV-557` copies that table)

Minimum known resilience twins from ticket *Problem* (TMDb cases with OMDb twins): always rate limited; always a server error; **transient server error then success** (known contract extra beyond *Wanted* 1 — recovers, not `ProviderUnavailable`); `Retry-After` header; `Retry-After` past total budget; response slower than attempt timeout; unreachable server; **cancel during backoff wait** (twin — removable); breaker opening. Stay (not twins / not removable as 401 twins): **TMDb cancel-during-request** (stay — distinct from cancel-during-backoff-wait); **OMDb request-limit-body 401 (quota)** (OMDb-only; outside shared 401 assertion). T001 draft covers lookup, resilience, and telemetry as **test-name lists without assertions**; lookup and telemetry method names are completed at T008 from both families — do not invent assertions here beyond ticket text and recon 401 lookup assertions (`MetadataProviderLookupTests.cs:459-474`, `SecondaryMetadataProviderLookupTests.cs:215-229`) (P6).

| Removed test (fill at T008) | Family | Contract case |
|---|---|---|
| _(Build completes full table at `specs/DEV-557/twin-inventory.md`)_ | lookup / resilience / telemetry | matching Wanted or inventory-extra case |

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1+)

**Purpose**: Produce/open the Spec Kit PR, then confirm ordinary owner Gate 1 merge before Phase B / Build.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-557 Spec Kit PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 Spec Kit PR merge receipt for DEV-557 in `specs/DEV-557/CONCLUSIONS.md`. Receipt only. T001–T014 MUST NOT proceed without verified merge (`brief.md` Approach; no structural owner checkbox on this ticket). Phase B still waits separately for DEV-551 implementation on main (Q1).

---

## Phase 1: Phase B pickup — DEV-551 merge, drift check, baselines (blocks production edits)

**Purpose**: Unlock Build only after DEV-551 implementation is on main; capture Stryker/line baselines before edits. Everything in T001 is read-only except Keel's one authorized harness-name repin commit (P9).
**Checkpoint**: Written pickup receipt cites DEV-551 merge on main, harness type names from main, Keel's repin commit `DEV-557 - Repin harness names to DEV-551 main` recorded in `CONCLUSIONS.md`; Q5 Stryker before receipts + T002 line-count baseline recorded; Keel read-only reanalysis clear.

- [ ] T001 After T000: **Wisp** obtains Phase B pickup recon in the Build worktree **only after DEV-551 implementation has merged to main**. Inventory: (1) DEV-551 harness file path(s) and concrete type names on main — list **every** DEV-551 file the Q1 extraction would touch, each with its HEAD blob hash and line count; extraction is structural only (no assertion edits); touching any file beyond those harness base file(s), including a derived subtitle suite's base class or constructor, needs a cited Patron ruling (STOP) (P3), (2) whether the harness is subtitle-bound (Q1 extraction needed), (3) `MetadataProviderServiceCollectionExtensions.cs:43-59` composition still matches shipped DI assumption, (4) current paths for the 12 recon files and any new DEV-551 harness lines to exclude from Q5 line count, (5) confirm recon 401 file:line citations still hold on pickup HEAD for `OmdbMetadataProvider.cs` and `TmdbMetadataProvider.cs` and the two lookup 401 tests, (6) **read-only draft twin inventory** as test-name lists (no assertions) across lookup, resilience, and telemetry so T004 can cover every twin (including known extra "transient server error then success") before the contract class is written (P6), (7) **harness capability receipt**: delay injection (yes or no, and how), the unreachable-server mechanism, a fresh host per case or a proven reset, and time control (or real clock with the existing budgets cited); a capability the harness lacks is built in the DEV-557 contract or fixture files, and adding it to a DEV-551 file needs a cited Patron ruling (STOP) (P4). **Keel** re-runs `/speckit-analyze` against main and reanalyzes brief/spec/plan/tasks **read-only**, then makes the harness-name repin as **one commit** on the Phase B Build branch, message `DEV-557 - Repin harness names to DEV-551 main`, changing only names and paths (no scope, FR, or SC edits), and records that commit in `CONCLUSIONS.md` (P9). Everything else in T001 remains read-only. Phase-B-pinned **contract and fixture class names MUST be functional** (`MetadataProvider*` / `SecondaryMetadataProvider*` style), **not** vendor-prefixed `Omdb*` / `Tmdb*` (constitution VIII; "OMDb fixture" / "TMDb fixture" are role labels only). **STOP** if DEV-551 implementation is not on main. **STOP** `needs recon:` on missing seams. **STOP** for cited Patron ruling before any outside-envelope edit.
- [ ] T002 After T001: **Gauge**. On the Phase B base **before any DEV-557 edit**: record **Q5 receipts** (labelled receipts, not a gate PASS) from a direct Stryker run with explicit `--mutate` on `src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.cs` and `src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.cs` under identical config; record the exact command and the Stryker `additional-timeout` value next to the identical-settings proof (P5; P8). Record Q5 line-count baseline — this T002 measurement on the Phase B base is the comparator for after (5,072 at `e1649d53` is historical context only); note exclusions for DEV-551-merged harness lines (P10). Receipts only — no production or test edits.

**Checkpoint**: T003+ unlocks only with DEV-551 on main, harness names pinned, before receipts recorded, and Keel pickup analysis clear.

---

## Phase 2: Foundational — harness consume, contract base, both fixtures green (blocks twin removal)

**Purpose**: Consume (or extract provider-agnostic base from) the DEV-551 harness; land abstract contract + OMDb + TMDb fixtures covering *Wanted* 1 minimum plus T001 draft-inventory extras; all those cases green before any twin removal (`brief.md` ordering step 3 / Contract case set).
**Independent Test**: Both fixtures pass every Wanted case and inventory extra through shipped DI against WireMock; ordinary 401 asserts Failed `InvalidResponse` + one request + `wasUnauthorized` (advice via fixture hook); telemetry redaction holds; fixture/contract type names are functional.
**⚠️ CRITICAL**: No twin removal until this phase is complete and green.

- [ ] T003 After T002: **Build lane**. Wire DEV-557 to the Phase-B-pinned DEV-551 harness under `tests/LamuFlix.IntegrationTests/` (and harness path from T001). If T001 shows the harness is subtitle-bound, extract provider-agnostic WireMock server lifetime, DI host build, and telemetry capture into a shared base both suites use — structural only (no assertion edits); touch only the DEV-551 harness base file(s) listed in T001 with blob hash/line count; **do not** change DEV-551 subtitle assertions; touching any file beyond those harness base file(s) (including a derived subtitle suite's base class or constructor) needs a cited Patron ruling (STOP) (FR-005; Q1; *Wanted* 4; P3). Skip extraction with recorded evidence when the harness is already provider-agnostic. Complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on this task's changed `.cs` (N/A with receipt if no `.cs` changed beyond renames already covered).
- [ ] T004 After T003: **Build lane**. Add the Phase-B-pinned abstract metadata-provider contract class under `tests/LamuFlix.IntegrationTests/` (functional type name, not `Omdb*`/`Tmdb*`) covering every *Wanted* 1 case as the **minimum** plus every twin from the T001 draft inventory through shipped DI (`MetadataProviderServiceCollectionExtensions` composition): found by IMDb id; found by title and year; not found; ordinary 401 key problem (Failed `InvalidResponse` + one request + `wasUnauthorized`; advice text via fixture hook); 429 `RateLimited`; 5xx `ProviderUnavailable`; timeout; unreachable; `Retry-After` inside and past budget; breaker opening; cancellation; **transient server error then success**; telemetry one span + one duration with no key/title/IMDb id tags (FR-001; FR-003; FR-008; FR-009 — not against the chain; `brief.md` Contract case set). Use abstract hooks for DI registration/options, WireMock stubs per provider wire format, and expected ordinary-401 outcome. **Isolation / timing (P5):** each contract case runs on a fresh DI host and fresh WireMock state, or on a reset that this task cites as proven; breaker and quota state must not carry over between cases; timing budgets come from the shipped options, with no invented SLA. **Negative guard (P7):** the OMDb ordinary-401 stub must not match the request-limit (quota) body; the OMDb quota case stays OMDb-only and outside the shared 401 assertion (FR-003). Complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on this task's changed `.cs`.
- [ ] T005 [P] [US1] After T004: **Build lane**. Add the Phase-B-pinned OMDb derived fixture under `tests/LamuFlix.IntegrationTests/` with a **functional** type name (not `Omdb*`; "OMDb fixture" is a role label only); register OMDb via shipped DI; implement OMDb WireMock stubs and fixture options with **fake** keys only (FR-008); supply OMDb ordinary-401 advice via the fixture hook; run the full contract suite green including inventory extras (FR-002; FR-008; SC-001). **Negative guard (P7):** the OMDb ordinary-401 stub must not match the request-limit (quota) body; keep the OMDb quota case OMDb-only and outside the shared 401 assertion (FR-003). Do not edit `OmdbMetadataProvider.cs` (read-only Q3 reference; expected Q3 outcome "none") (P1). Complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on this task's changed `.cs`.
- [ ] T006 [P] [US1] After T004: **Build lane**. Add the Phase-B-pinned TMDb derived fixture under `tests/LamuFlix.IntegrationTests/` with a **functional** type name (not `Tmdb*`; "TMDb fixture" is a role label only); register TMDb via shipped DI; implement TMDb WireMock stubs and fixture options with **fake** keys only (FR-008); supply TMDb ordinary-401 advice via the fixture hook; run the full contract suite green asserting the shared ordinary-401 outcome (Failed `InvalidResponse` + one request + `wasUnauthorized`) (FR-002; FR-003; FR-008; SC-001). If and only if the suite cannot assert OMDb ordinary-401 behaviour on TMDb without a production change, apply the minimal Q3 carve-out to `src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.cs` only (`OmdbMetadataProvider.cs` stays read-only) and record the difference for the PR body; otherwise leave production adapters untouched and record "none" (expected outcome per recon OMDb `:452-455` and TMDb `:537-540`) (P1). Complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on this task's changed `.cs`.
- [ ] T007 **Gauge checkpoint**. After T005–T006: Focused `dotnet test` for both fixtures + scoped three static gates on this phase’s changed `.cs`. Record US1 / SC-001 receipt. Twin removal MUST NOT start without this receipt.

**Checkpoint**: Foundation ready — twin inventory and removal may proceed.

---

## Phase 3: User Story 2 - Twin inventory and family-by-family removal (Priority: P1)

**Goal**: Every twin across lookup, resilience, and telemetry exists only in the suite; single-adapter cases and residuals stay; Q7 satisfied if telemetry file touched.
**Independent Test**: Twin inventory complete; after each family removal, suite + remaining adapter tests green; listed stay suites still present (SC-002).
**Order**: After T007.

- [ ] T008 [US2] **Build lane**. After T007: Write the full twin inventory (removed-test → contract-case) covering lookup, resilience, and telemetry twins across `tests/LamuFlix.IntegrationTests/MetadataProvider{Lookup,Resilience,Telemetry}Tests.cs` and `SecondaryMetadataProvider{Lookup,Resilience,Telemetry}Tests.cs`, seeding from the T001 draft test-name lists and the ticket resilience twin list (including "transient server error then success") and recon 401 lookup assertions; mark residuals that must stay. **Commit** the inventory at `specs/DEV-557/twin-inventory.md` on the Build branch **before any deletion** (T009 depends on it); `pr-body-DEV-557` copies that table (P11). Confirm stay surfaces are **read-only** (`MetadataProviderTmdbSignalTests.cs`, `SecondaryMetadataProviderTrailerTests.cs`, `SecondaryMetadataProviderMappingTests.cs`, composition and chain-composition tests, **TMDb cancel-during-request** stays, **OMDb request-limit-body 401 (quota)** — do not remove as a 401 twin); if a twin is found in a stay surface, Build **STOP**s for a cited Patron ruling before editing it (P2). Checklist: **"cancel during backoff wait" is a twin and removable**; **TMDb cancel-during-request stays** (P6). The unmatched-twin loop is the **safety net**, not the main path: **If** the final inventory finds another unmatched twin, add the contract case and re-run T005, T006, and T007 green **before** T009 — never keep an unmatched twin as a residual (`brief.md` Contract case set; P6).
- [ ] T009 [US2] **Build lane**. After T008: Remove lookup twins from `tests/LamuFlix.IntegrationTests/MetadataProviderLookupTests.cs` and `tests/LamuFlix.IntegrationTests/SecondaryMetadataProviderLookupTests.cs` per inventory; keep residuals; run focused tests green after this family (FR-004). Complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on this task's changed `.cs`.
- [ ] T010 [US2] **Build lane**. After T009: Remove resilience twins from `tests/LamuFlix.IntegrationTests/MetadataProviderResilienceTests.cs` and `tests/LamuFlix.IntegrationTests/SecondaryMetadataProviderResilienceTests.cs` per inventory; keep TMDb cancel-during-request and residuals; run focused tests green after this family (FR-004). Complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on this task's changed `.cs`.
- [ ] T011 [US2] **Build lane**. After T010: Remove telemetry twins from `tests/LamuFlix.IntegrationTests/MetadataProviderTelemetryTests.cs` and `tests/LamuFlix.IntegrationTests/SecondaryMetadataProviderTelemetryTests.cs` per inventory; keep residuals. **If** `SecondaryMetadataProviderTelemetryTests.cs` is in the diff at all, fix InspectCode `MethodHasAsyncOverloadWithCancellation` at `:98` and `:102` (Q7; FR-007); otherwise leave those baseline warnings `noted, no ticket`. Run focused tests green after this family. Complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on this task's changed `.cs`.

**Checkpoint**: SC-002 inventory + removal complete; unlocks measurement and exact-head gates.

---

## Phase 4: User Story 3 - Receipts, exact-head gates, Size M review (Priority: P2)

**Purpose**: Prove Stryker/line-count Done-when items; green exact-head gates; Size M three-axis review under closing bar.
**Order**: After T011 and all in-scope receipts.

- [ ] T012 [US3] **Gauge**. After T011: Record **Q5 receipts** (labelled receipts, not a gate PASS) from a direct Stryker run with explicit `--mutate` on `OmdbMetadataProvider.cs` and `TmdbMetadataProvider.cs` with **identical** settings to T002 (record the exact command and the same `additional-timeout` value next to the identical-settings proof); assert no per-file score drop (FR-006; SC-003; P5; P8). Record Q5 line-count after vs the **T002 Phase B base** measurement (5,072 at `e1649d53` is historical context only): 12 recon files + new DEV-557 contract/fixture files + any harness lines DEV-557 added; for each DEV-551 harness file touched under Q1, show a **diff stat** that separates DEV-551-merged lines from DEV-557-added lines; exclude DEV-551-merged harness lines from the measure; assert after < before (SC-004; P3; P10).
- [ ] T013 [US3] **Gauge**. After T012: Exact-head Roslyn, cyclomatic complexity (incl. refactor threshold 6), InspectCode, `dotnet test`, `dotnet format --verify-no-changes`; web scope-empty as `SKIPPED (scope-empty)` never PASS. Keep SKIPPED/N/A/Could not run distinct from PASS (FR-007; SC-005). Also run `scripts/run-mutation.ps1` and report its verdict as the script prints it (PASSED, FAILED, SKIPPED, or N/A), plus the kill-share and file-span lines it prints; note the risk that a consolidated suite has a few tests killing most mutants across both adapters (P8). Re-prove after any review fix commits.
- [ ] T014 **Size M review / PR body**. After T013: Risk (Sentry), Standards (Ledger), Spec (Compass) with explicit dispositions under `brief.md` Closing bar; missing axis blocks; max two review/remediation rounds. Unresolved Critical/High or Done-when/gate failure blocks; residual blockers to Bernstein at the cap. Out-of-scope findings: follow-up tickets only for Critical/High/broken behaviour; otherwise `noted, no ticket`. **`pr-body-DEV-557` MUST carry (1) the T008 table copied from `specs/DEV-557/twin-inventory.md` and (2) the Q3 401 difference statement, or "none"** (`brief.md` Test strategy / Frozen scope; P11). Checklist: **"cancel during backoff wait" is a twin and removable**; **TMDb cancel-during-request stays** (P6). Owner merges. Gate 1 remains a separate already-closed prerequisite.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0**: Blocks all Build work (Gate 1).
- **Phase 1**: Blocks production edits; additionally blocked on DEV-551 implementation merge; captures before receipts.
- **Phase 2**: Blocks twin removal; both fixtures must be green.
- **Phase 3**: Depends on Phase 2; family-by-family sequential removal (lookup → resilience → telemetry).
- **Phase 4**: Depends on Phase 3 complete; measurement then gates then review.

### User Story Dependencies

- **User Story 1 (P1)**: Contract + fixtures in Phase 2 (T004–T007).
- **User Story 2 (P1)**: Depends on US1 green (T007) then T008–T011.
- **User Story 3 (P2)**: Depends on US2 complete (T011) then T012–T014.

### Parallel Opportunities

- T005 and T006 may run in parallel after T004 (different fixture files).
- T001 analysis seats are read-only; no parallel production edits before T002 baselines.
- Family removals (T009–T011) are sequential by design (green after each).

### Parallel Example: Fixtures

```text
# After T004:
Task: "OMDb derived fixture — full contract suite green"
Task: "TMDb derived fixture — full contract suite green (Q3 carve-out only if required)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 0–1 (Gate 1 + DEV-551 wait + baselines)
2. Complete Phase 2 (harness + contract + both fixtures green)
3. **STOP and VALIDATE**: SC-001 before any twin deletion

### Incremental Delivery

1. US1 green → inventory (T008) → remove lookup → resilience → telemetry
2. Measurement + exact-head gates → Size M review

### Notes

- `[P]` tasks = different files, no incomplete deps
- `[Story]` label maps task to US1/US2/US3; T000–T004 and T007 carry no story label (cross-story foundational/gate); T005–T006 are US1 in local Phase 2 for ordering (L4)
- Never invent harness type names before T001 pin
- Fake keys only; no real secrets
- Every commit message starts `DEV-557 - {subject}` (P11)
