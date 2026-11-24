---
description: "Task list for DEV-567 API failure toasts"
---

# Tasks: API failure toasts

**Input**: Design documents from `specs/DEV-567/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q8), `CONCLUSIONS.md`, `ASSUMPTIONS.md`; Maestri notes `DEV-567`, `recon-DEV-567`; `specs/PRODUCT.md`.

**Status**: **PLAN FROZEN** (2026-11-23). See [PLAN_ADJUDICATION.md](PLAN_ADJUDICATION.md). Gate 1 / T000 is complete. The verified spec PR #485 merge receipt is recorded in [CONCLUSIONS.md](CONCLUSIONS.md). T001 remains the read-only prerequisite for T002.

**Tests**: Required (`brief.md` Test strategy; FR-009; SC-001–SC-007). TDD optional. Use existing Vitest/RTL/MSW (`web/src/test/server.ts`, `subtitlesTab.test.tsx` patterns). Named QueryClient suite: `web/src/api/queryClient.test.tsx`. One Playwright check under `web/e2e/`. No new test framework or npm dependency.

**Format**: `- [ ] [ID] [Story?] Description with file path`. Tasks run after their explicit predecessors. Same-file edits are never parallel. Shared gate tasks are never parallel. Do not start a later ID before its named `After` predecessor completes.

**Conventions**: Edit only the authorized boundary in `plan.md` / brief approach; STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` only when a required OpenAPI export fact or a needed React Query discarding boundary remains unidentified after pickup; no new npm/NuGet dependency; never hand-edit `web/src/api/types.ts`; never invent a checked-in OpenAPI export script recon did not find; no explanatory comments beyond AGENTS exemptions; never lower harness thresholds. Otherwise-unassigned implementation tasks are owned by the generic Build lane (do not invent named seats).

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: After clean analyze → plan challenge → PLAN FROZEN, Bernstein coordinates; Rigger prepares/opens the spec PR; only the user merges; Keel T000 records the verified merge receipt. No Build work (T001+) before Gate 1.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (after PLAN FROZEN: Bernstein/Rigger produce/open; user merges; Keel Gate 1 merge record) — blocks T001+
Phase 1   T001          (Wisp pickup recon + Keel reanalysis + Gauge baseline; read-only) — blocks T002; drift amendment is separate if needed
Phase 2   T002 → T004   (Foundational: OpenAPI ProblemDetails schema docs + regenerate web contract)
Phase 3   T005 → T007   (US3 toast host lifecycle / a11y / styling)
Phase 4   T008 → T010   (US1/US2 shared normalize+report helper + QueryClient cache wiring)
Phase 5   T011 → T012   (US1/US2 raw subtitle acquisition wiring + MSW coverage)
Phase 6   T013 → T015   (Playwright SMOKE + full evidence matrix + gates/review)
```

Ordering per brief approach steps 1–5. No dependency cycle. No parallel starts that skip explicit predecessors.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1)

**Purpose**: After PLAN FROZEN, produce/open the spec PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [X] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-567 spec PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-567 in `specs/DEV-567/CONCLUSIONS.md`. Receipt only. T001–T015 MUST NOT proceed without verified merge (`brief.md` Closure; no structural owner checkbox on this ticket).

---

## Phase 1: Pickup recon (read-only; blocks T002)

**Purpose**: Confirm Gate-1-merged artifacts still match landed main; verify OpenAPI export invocation facts; inventory React Query error paths. No production edits inside this read-only step.
**Checkpoint**: Fresh recon recorded; Keel read-only reanalysis clean. Any plan/tasks amendment for authorized demonstrated drift is a **separate subsequent authorized step**, not part of T001. Adapter STOP only if a needed discarding boundary remains unidentified.

- [ ] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main inventorying: `Program.cs` v1 OpenAPI registration, existing schema transformers, `GlobalExceptionHandler` / `ProblemDetailsTraceId` non-behaviour-change confirmation, candidate app-generated OpenAPI v1 invocation, environment/configuration and expected v1 output (T002 verifies them), `web/scripts/generate-api.mjs` / `npm run generate:api`, `web/src/main.tsx` QueryClient, `useSubtitleAcquisition.ts` three raw call sites, MSW/`subtitlesTab.test.tsx` / DEV-525 e2e neighbors, the installed openapi-fetch result's raw `Response` shape, cancellation/current-request guards at all three raw sites, existing error subscribers in the touched query/mutation paths, and any named throwing boundary that drops diagnostics before cache `onError`. For a candidate adapter record its exact file/lines and compare available response diagnostics against what reaches the cache for the same execution. Identify the existing API test project and mutation applicability under `harness.yml`. **Keel** reanalyzes brief/spec/plan/tasks against landed main in a **strictly read-only** pass. Record the read-only reanalysis result. If authorized demonstrated drift requires plan/tasks changes, that amendment is a **separate subsequent authorized step** after this read-only recon/analyze — never conflated with T001. **Gauge** records pre-edit gate baseline. **STOP** `needs recon` only when the export invocation remains unidentified, or when a needed discarding-adapter boundary remains unidentified after pickup (do not imply an adapter discovery is always required). **STOP** for cited Patron ruling rather than expanding file/protected surface. **Strictly read-only**.

---

## Phase 2: Foundational — ProblemDetails contract documentation (blocks user stories)

**Purpose**: Document optional `reason`/`traceId` on C# OpenAPI `ProblemDetails`, export v1 JSON, regenerate TypeScript; zero runtime HTTP change.
**Checkpoint**: `openapi.json` and `types.ts` show optional string `reason` and `traceId`; re-export/regenerate is drift-free on both outputs; runtime payloads/routes/status unchanged.

- [X] T002 **Build lane**. After T001: Verify the T001 candidate against the app-generated v1 document. Record the exact invocation, environment/configuration and v1 output location. If the export cannot be verified, stop and route `needs recon: app-generated v1 export invocation` to Wisp through Bernstein (`brief.md` step 1; ruling 3; FR-008).
- [X] T003 **Build lane**. After T002: Add `src/LamuFlix.Api/ProblemDetailsSchemaTransformer.cs` declaring optional string `reason` and `traceId` on the exact `ProblemDetails` schema and leave `HttpValidationProblemDetails` and unrelated schemas untouched. Follow the existing transformer pattern and add focused coverage in the existing API test project if the transformer is mutation-eligible, proving both the target and an unrelated schema path. Register it in `src/LamuFlix.Api/Program.cs` alongside existing v1 transformers (~lines 60–67). Do not change `GlobalExceptionHandler.cs` or `ProblemDetailsTraceId.cs` runtime behaviour (`brief.md` step 1; rulings 2–3; FR-008).
- [X] T004 **Build lane**. After T003: Export the app-generated v1 document to `web/src/api/openapi.json`. Confirm both properties are optional strings on `ProblemDetails` and review the schema diff for unrelated changes before running `npm run generate:api` from `web/` to regenerate `web/src/api/types.ts`, review generated output for scope, enforce **zero drift** across both regenerated `openapi.json` and `types.ts` on a second export/generate cycle, and **separately** verify that `reason` and `traceId` are optional strings on `ProblemDetails` in both outputs (`brief.md` step 1; FR-008; SC-006). Never hand-edit `types.ts`.

---

## Phase 3: User Story 3 - Toast host lifecycle and accessibility (Priority: P2)

**Goal**: Single dependency-free host with stack, dismiss, live region, and house styling.
**Independent Test**: Host Vitest/RTL tests with fake timers/keyboard prove FIFO cap, 5s pause/resume, Dismiss, polite announcement without focus theft, and reduced-motion; narrow viewport-fit is asserted in the single T013 Playwright scenario.
**Order**: T005 → T007 (after T004).

### Tests

- [ ] T005 [US3] **Build lane**. After T004: Add failing Vitest/RTL coverage in `web/src/ui/ApiFailureToastHost.test.tsx` for three-toast FIFO eviction under a simulated failure burst, timer and pause cleanup for evicted items, stable focus on focused-item removal, five-second unpaused auto-dismiss, hover/focus pause and resume, keyboard-accessible Dismiss, one polite live-region announcement per addition, and reduced-motion expectations. Vitest/RTL may check styling intent (including narrow-safe class/token wiring) but does **not** assert browser layout fit — that viewport-fit assertion belongs to the single T013 Playwright scenario (`brief.md` Toast lifecycle; FR-005/FR-006; SC-003/SC-004).

### Implementation

- [ ] T006 [US3] **Build lane**. After T005: Create the in-memory queue for this host and its reporting callers. Implement `web/src/ui/ApiFailureToastHost.tsx` (single host, bottom-end / narrow-safe placement, cap 3, 5s unpaused dismiss, hover/focus pause, Dismiss control, persistent `aria-live="polite"` region without focus move) and use existing `web/src/index.css` house surface, ink, accent and square-corner tokens with the existing reduced-motion treatment. Wrap long diagnostics to keep the toast within the viewport (`brief.md` step 2; ruling 6; ASSUMPTIONS Q6; FR-005/FR-006).
- [ ] T007 [US3] **Build lane**. After T006: Mount the single subscriber host in `web/src/main.tsx` above route content and share the app-scoped in-memory queue with the reporter. UI tests create fresh queues. Reporting is owned by the call sites and independent of React render/effect replay (`brief.md` step 2; FR-005).

**Checkpoint**: Host lifecycle tests green in isolation with a test double reporter if needed; no API wiring required yet.

---

## Phase 4: User Stories 1 & 2 - Shared normalization + React Query reporting (Priority: P1) 🎯 MVP slice

**Goal**: Shared normalize/report helper; global QueryCache/MutationCache toasts for HTTP and network failures.
**Independent Test**: Forced query/mutation 4xx/5xx and rejected-fetch paths each show exactly one toast with correct diagnostics; programming exceptions excluded; no global fetch interceptor; retry stays false.
**Order**: T008 → T010 (after T007).

### Tests

- [ ] T008 [US1] **Build lane**. After T007: Add failing Vitest/RTL+MSW coverage in `web/src/api/reportApiFailure.test.ts` and `web/src/api/queryClient.test.tsx` that exercises the **configured QueryClient through both QueryCache and MutationCache** for representative forced HTTP 4xx/5xx failures and rejected fetch, asserting exactly one toast with correct diagnostics per terminal failure, unavailable labels on transport failure, HTTP status precedence and structured-status fallback, optional reason/traceId handling, `Request failed` / `Network error` titles, suppression helpers for abort/stale where applicable, rejection of unknown throws (including ordinary TypeError and non-Error values), intentional cache cancellation, empty/HTML/malformed error bodies that keep known HTTP status without a second toast, and no raw exception/body/`detail` text. Exercise concurrent query/mutation failures through the configured caches and assert exactly one report per execution and the three-visible FIFO cap, including a case with an inline/local error subscriber (`brief.md` Vitest matrix / Q5/Q7; spec US1/US2; FR-003/FR-004; SC-001/SC-002).

### Implementation

- [ ] T009 [US1] **Build lane**. After T008: Implement pure normalization and publication to the app-scoped queue in `web/src/api/reportApiFailure.ts`, consuming generated `ProblemDetails` optional fields. Recognize HTTP failure from an unsuccessful response or a recognized ProblemDetails value carrying a valid failure status. Only report `Network error` for an explicit transport marker assigned at the scoped fetch-rejection boundary before response parsing or application logic. Ignore unknown or cancelled inputs, including an unmarked `TypeError`. An empty, HTML or malformed HTTP body retains the known response status and uses fallback diagnostics. Reporting must tolerate these inputs without throwing into callers. Keep the subtitle decoder for inline state and avoid a global interceptor (`brief.md` Q4/Q5 and step 6 amendments; FR-003/FR-004/FR-007).
- [ ] T010 [US1] **Build lane**. After T009: Create `web/src/api/queryClient.ts` with the configured `QueryCache` and `MutationCache` callbacks and `queries: { retry: false }`, and use it in `web/src/main.tsx`. Ignore React Query cancellation and scoped intentional aborts before reporting. Verify both caches through the same factory in `web/src/api/queryClient.test.tsx`. A Q5 adapter edit is limited to carrying diagnostics or transport provenance at the exact throwing boundary identified by recon, when the same execution demonstrably loses metadata before reaching the cache. Preserve routes, retry and inline behavior. Stop for targeted recon if a required boundary remains unidentified (`brief.md` Q4/Q5/Q7; FR-001/FR-002; T001).

**Checkpoint**: React Query HTTP and network failures each produce one correct toast; US3 host still obeys lifecycle rules.

---

## Phase 5: User Stories 1 & 2 - Raw subtitle acquisition reporting (Priority: P1)

**Goal**: Explicit helper at languages catalog, search, and download (including 429 quota and transport errors) without disturbing inline UI.
**Independent Test**: Extend `subtitlesTab.test.tsx` MSW coverage for the three calls across 4xx/5xx/rejected fetch/429/missing optional fields/abort/stale/inline preservation.
**Order**: T011 → T012 (after T010).

### Tests

- [ ] T011 [US2] **Build lane**. After T010: Extend `web/src/features/playback/subtitlesTab.test.tsx` using existing MSW handlers to cover catalog/search/download reporting for forced 4xx/5xx, rejected fetch with unavailable diagnostics, traceId present and absent, response status precedence with mismatched body status, HTML/empty/truncated JSON preserving HTTP classification, repeated 429s with one report per execution and preserved inline quota, abort during flight, and an older failure settling after a newer success. Keep unrelated programming throws out of network reporting and assert unchanged inline subtitle behavior (`brief.md` step 4; FR-001–FR-004; SC-001/SC-002/SC-005).

### Implementation

- [ ] T012 [US2] **Build lane**. After T011: Wire `reportApiFailure` explicitly at the languages catalog, search, and download boundaries in `web/src/features/playback/useSubtitleAcquisition.ts` (brief-cited ranges ~79–96, 107–142, 177–205) for both `{ error }` HTTP failures and caught transport errors. Check the current execution first at each asynchronous boundary using its existing request/effect guard where present and an equivalent scoped guard where needed. Use the owned signal and the request's intentional abort outcome where applicable. Only then publish a classified failure, before existing inline state handling, while preserving the decoder and quota behavior (`brief.md` step 4; ruling 4; FR-001/FR-002).

**Checkpoint**: All three raw paths report correctly; inline tests remain meaningful.

---

## Phase 6: Polish — Playwright evidence, gates, review

**Purpose**: SMOKE e2e, full evidence matrix, honest gate labels, Size M review loop.
**Checkpoint**: Playwright green; web and applicable .NET gates honest; review cleared or blockers routed per closing bar.

- [ ] T013 [US1] **Build lane**. After T012: Add a **single** Playwright scenario in `web/e2e/dev567-api-failure-toast.e2e.ts` following `web/e2e/dev525-subtitle-search.e2e.ts` patterns: at a **narrow viewport**, intercept a deterministic raw subtitle HTTP failure with known status, reason and traceId, including a long diagnostic value. Assert one app-wide toast with those diagnostics alongside the existing inline error (SMOKE-001). Verify viewport fit. Dismiss the toast and verify removal and focus behavior (SMOKE-002) (`brief.md` step 5 / Toast lifecycle; FR-005/FR-009; SC-001/SC-004/SC-005).
- [ ] T014 **Build lane**. After T013: Run the complete evidence matrix — web lint, typecheck, Vitest suite (including `reportApiFailure.test.ts`, `queryClient.test.tsx`, host and subtitle suites), targeted Playwright check; re-export/regenerate and enforce **zero drift** on both `web/src/api/openapi.json` and `web/src/api/types.ts`, and **separately** re-verify that `reason` and `traceId` are optional strings on `ProblemDetails`. For the C# schema transformer diff, name and run the **mandatory scoped static gates** `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (use the configured refactor threshold when that gate applies), and `./scripts/run-jetbrains-inspectcode.ps1`; require `dotnet format --verify-no-changes`; require `LamuFlix.ArchitectureTests` to pass inside `dotnet test` (may be via the full suite). Record the schema-only diff and runtime payload/routes/status invariance using the registration and affected-file review, backed by focused .NET regression evidence where applicable. Record routes, retry and inline behavior evidence for any T010 adapter edit. Include `./scripts/run-mutation.ps1` under `harness.yml` applicability and record exclusions with their reasons. Applicability of other harness gates is determined under the harness (honest `PASS` / `SKIPPED` / `SKIP` / `N/A` / `Could not run` — never fold non-pass into PASS; never lower thresholds) (`brief.md` ruling 7; SC-006/SC-007).
- [ ] T015 **Build lane**. After T014: Size M three-axis review (Risk/Standards/Spec) pinned to the reviewed revision; dispose every finding explicitly; Critical/High/Medium and failed mandatory ACs block; accepted in-scope defects close; cap two rounds; route remaining blockers at the cap to Bernstein (`brief.md` Frozen scope / Closure).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (T000)**: After PLAN FROZEN — blocks all Build work
- **Phase 1 (T001)**: Depends on T000 — read-only; blocks T002+; drift amendment is a separate authorized step when needed
- **Phase 2 (T002–T004)**: Foundational contract — blocks host/helper/wiring
- **Phase 3 (T005–T007)**: Toast host — after Phase 2; T005 after T004
- **Phase 4 (T008–T010)**: Shared helper + React Query — after Phase 3; T008 after T007
- **Phase 5 (T011–T012)**: Raw subtitle wiring — after Phase 4; T011 after T010
- **Phase 6 (T013–T015)**: Evidence and review — after Phases 3–5 complete

### User Story Dependencies

- **US3 (host)**: Completes after foundational contract; independently testable with local report doubles
- **US1 (HTTP toasts)**: Depends on host + helper; React Query slice is MVP with US2 network classification in the same helper
- **US2 (network + raw paths)**: Shares helper with US1; raw wiring after helper exists; independently testable via MSW subtitle suite

---

## Implementation Strategy

### MVP First (US1 React Query HTTP path + host)

1. Complete Phase 0–2 (Gate 1 + contract)
2. Complete Phase 3 host
3. Complete Phase 4 helper + QueryClient wiring
4. **STOP and VALIDATE**: Forced query/mutation 4xx/5xx and rejected fetch each show one toast; programming exceptions excluded
5. Continue Phase 5–6 for raw paths, network matrix, Playwright, gates, review

### Incremental Delivery

1. Contract docs → typed optional fields available
2. Host alone → lifecycle proven in Vitest; narrow viewport-fit proven later in T013 Playwright
3. React Query reporting → global API failures visible through both caches
4. Raw subtitle reporting → ticket Wanted raw calls covered
5. Playwright + gates → Done when evidence complete

---

## Notes

- `[Story]` maps to US1/US2/US3 in `spec.md`
- Approved file names are fixed in `plan.md` Source Code; Build must not relocate outside authorized areas without a Patron ruling
- Contingent React Query adapter edit is not pre-filed; if recon already names a discarding boundary, T010 applies the minimum approved fix; STOP/`needs recon` only when a required boundary remains unidentified (Q5; T001)
- Keel completed Phase A analysis and plan adjudication. T001 owns the read-only Phase B reanalysis after T000.
