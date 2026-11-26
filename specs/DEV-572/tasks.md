---
description: "Task list for DEV-572 browser-unsupported audio stream conversion"
---

# Tasks: Browser-unsupported audio stream conversion

**Input**: Design documents from `specs/DEV-572/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q7), `CONCLUSIONS.md`, `ASSUMPTIONS.md`; Maestri notes `DEV-572`, `recon-DEV-572`; `specs/PRODUCT.md`.

**Status**: Draft — Gate 1 / T000 remains **pending**. Proof and Build begin only after the verified Gate 1 merge receipt. Production code begins only after proof pass (Q6).

**Tests**: Required (`brief.md` Task order; FR-016; AC8; Q6). Follow implement test-first (failing cases before implementation). Use existing unit and `StreamEndpointTests` projects; small AC3 fixture. Chromium proof/acceptance use real library samples. No new test framework/dependency.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence/review work is never `[P]` and is **not** Anvil-dispatched.

**Conventions**: Edit only the authorized envelope in `plan.md` / brief Q5; STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` / `needs decision` if a required fact/decision is missing; no new dependencies; no LocalPlay read/change; no production `web/` edit without return ruling; never lower harness thresholds. Otherwise-unassigned **local Build** implementation tasks are owned by the generic Build lane (Anvil). Named seats own Spec PR, pickup, proof coordination, Cog refactor, Gauge gates, review, and ship per `task-pipeline`.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` Build dispatch only. They are **not** canonical `task-pipeline` phases. Under task-pipeline Phase 3 step 2, Anvil receives **one local phase per ask, in order**. Do **not** bundle Gauge or review into an Anvil phase.

**Spec-PR provenance**: ADR is `docs/adr/0050-browser-stream-audio-conversion-port.md` (Proposed now; Accepted in the Build PR that also delivers `IStreamConversionService`, Principle I 2.0.0 → 2.1.0, and the matching `PortWhitelistPattern`). After Keel records the plan-challenge freeze, Patron sets `gate1: provisional`; Bernstein coordinates; Rigger prepares/opens the Spec Kit PR (`specs/**` only — ordinary owner Gate 1, no structural checkbox); only the user merges (completes Gate 1); Keel records Gate 1 merge receipt. No Build/proof production tasks (T001+) before Gate 1.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger open; user merges; Keel Gate 1) — blocks T001+
Phase 1   T001          (Wisp pickup + Keel reanalysis) — blocks production edits
Phase 2   T002 → T003   (US proof / FR-013–FR-014; Q6) — blocks production conversion code
Phase 3   T004 → T008   (US2 decisions + US1/US5 service foundation)
Phase 4   T009 → T011   (US1/US3 wire StreamEndpoints + integration)
Phase 5   T012          (automated integration checkpoint; local Build ends)
—— local Build ends ——
Downstream (not Anvil): Cog /refactor → Gauge Phase 3 step 5 (incl. ArchitectureTests) → Size L axes → Keel findings → Phase 5 rebase
  → post-build Chromium acceptance Compose + cluster (FR-015 enumerated integrated route checks; SMOKE-001–003)
```

Ordering per brief Task order, Q6, and Step 6. No dependency cycle. Do not start Phase 3 production conversion until Phase 2 proof **passes**, including the forced-hang cancel proof (else return to Patron). Do not assert hosted merge readiness from Build.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1+)

**Purpose**: Produce/open the Spec Kit PR, then confirm ordinary owner Gate 1 merge before pickup/proof/Build.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop.

- [ ] T000 After Keel freeze receipt (PLAN FROZEN) and Patron `gate1: provisional`: **Bernstein** coordinates and **Rigger** prepares/opens the DEV-572 Spec Kit PR (`specs/**` only) for ordinary owner Gate 1; surface the 300 ms cold raw probe interpretation as a Spec PR **note** (not a checkbox); do not include ADR Accepted, constitution, or ArchitectureTests whitelist edits in the Spec Kit PR. Only the user merges (completes Gate 1). After merge, **Keel** records the verified Gate 1 Spec Kit PR merge receipt for DEV-572 in `specs/DEV-572/CONCLUSIONS.md`. Receipt only. T001+ MUST NOT proceed without verified merge (`brief.md` Q7 / Step 6 F1; no structural owner checkbox).

---

## Phase 1: Pickup recon (blocks production edits)

**Purpose**: Confirm authorized seams against pickup HEAD.
**Checkpoint**: Written pickup receipt cites file/test seams; Keel read-only reanalysis clear. T001 itself does not edit production files.

- [ ] T001 After T000: **Wisp** obtains Phase B pickup recon in the Build worktree confirming the authorized envelope (`src/LamuFlix.Api/Endpoints/StreamEndpoints.cs`, Core options/`IStreamConversionService` locations, `src/LamuFlix.Infrastructure/Streaming/` target, `SystemProcessRunner.cs` / `ThumbnailOptions` reuse, `tests/LamuFlix.IntegrationTests/StreamEndpointTests.cs`, unit-test project paths, fixture location, Build-only constitution + `ArchitectureTests` whitelist files, `docker/Dockerfile.api` only if AC7 proof requires it) and recording demonstrated drift as a **separate follow-up** before production edits. Receipt MUST also record: (1) deployment replica count and scratch-ownership topology, deriving aggregate limits from evidence and requiring exclusive owned cleanup domains if storage is shared before AC7 cluster acceptance; (2) the exact DI composition site that will call the Streaming registration (Q5 return ruling if outside the authorized envelope); (3) ffmpeg/ffprobe tool availability for CI/dev and a reproducible AC3 fixture source — missing prerequisite → explicit blocker, not a silent green; (4) cited evidence of `SystemProcessRunner` cancel/kill and output-capture contracts (or the AC6-cited change path). Confirm LocalPlay `Infrastructure/Playback` remains untouched. Confirm Q3 sibling ffprobe-beside-ffmpeg resolution plan. **Keel** reanalyzes brief/spec/plan/tasks against pickup HEAD **read-only**. **Strictly read-only** aside from collecting the receipt artifacts named by the Build lane.

**Checkpoint**: T002+ unlocks only when the Wisp receipt is recorded and Keel pickup analysis is clear.

---

## Phase 2: Feasibility proof (Priority: P1) — blocks production conversion code

**Goal**: Pass Q6 bars (a)–(f) and Q2 subtitle/delay/control parity on Compose; record cluster tool/scratch/one cold timing; fill required `plan.md` measurement tables (FR-013–FR-014).
**Independent Test**: Written proof receipt with numbers and Chromium observations for movie 959 + real AC3 + real DTS, with plan tables populated (linked evidence may supplement only).
**Order**: After T001. No production `src/` conversion implementation in this phase.

- [X] T002 **Build/proof lane**. After T001: Run the isolated read-only feasibility proof per `plan.md` / Q2 / Q6 / Step 6 R2 — API-owned scratch only; byte-range-capable origin; supported Chromium; movie 959 plus one real AC3 and one real DTS. Record **selected input codecs** in the three existing sample rows, stream-copy metadata, AAC audio, full duration, both seek modes (click and arrow-key ±10 s) ≤3 s, range-abort continuity, Q2 subtitle / subtitle-delay / existing-control parity for those samples, cold prep times with the **300 s cap from cold preparation to audible start on the largest Compose sample**, and raw cold/warm timings (≤300 ms cold probe; no repeatable warm delta). Additionally demonstrate last-waiter cancellation of a **deliberately long-running / forced-hang** child through the proposed cancellation path and show no leftover process or partials after the 1 s grace; if that proof cannot meet AC6, **STOP and return to Patron** with evidence (do not proceed to production conversion; no stealth wall-clock timeout). Confirm ffmpeg/ffprobe versions and paths plus writable scratch inside the Compose API container. If ffprobe is missing on Compose, **STOP** and return for provisioning ruling (Q3). If container/stream-copy fails, **STOP** and reopen Q2. If the largest sample takes >300 s from cold preparation to audible start, **STOP** and return the measurements to Patron (reopened Q6 bar e). Any reopening of progressive delivery requires a separate spec cycle. If raw timing misses, **STOP** and reopen Q3. **Require** results in `specs/DEV-572/plan.md` measurement tables as ruled; a linked evidence note may supplement but does not replace those tables.
- [X] T003 **Build/proof lane**. After T002 Compose bars: On the running home-cluster API image, record ffmpeg/ffprobe resolved paths/versions, writable scratch, and one cold-preparation timing (AC7). Write those cluster checks and the one cold timing into the `specs/DEV-572/plan.md` measurement tables (linked evidence may supplement). If ffprobe is missing on the cluster, **STOP** and return for Q3 provisioning ruling. Do not invent an additional cluster timing threshold. Do not claim full cluster playback bars here — those move to post-build acceptance (FR-015). Record pass or explicit blocker. **Do not proceed to Phase 3 production conversion without T002+T003 pass.**

**Checkpoint**: Proof pass recorded. Authorized Q5 source/test edits remain allowed. Deploy-only edits (`Dockerfile.api` / Compose) remain forbidden unless this proof demonstrated AC7 need (then cite and limit to that need); any other tracked file outside the authorized Q5 envelope needs the existing return ruling.

---

## Phase 3: User Story 2 + foundation for US1/US5 — probe, cache, conversion, scratch (Priority: P1)

**Goal**: Tested codec/track/cache decisions and conversion/scratch lifetime service inside the envelope (FR-003–FR-012; US2; US5 mechanics).
**Independent Test**: Unit tests for decisions and lifetime without requiring the HTTP route wiring yet.
**Order**: T004 → T005 → T006 → T007 → T008 after Phase 2 pass.

### Tests

- [ ] T004 [P] [US2] **Build lane**. After T003: Using implement test-first, author failing unit tests for actual selected-track mapping (default/first), AC3/E-AC3/DTS → convert, AAC/MP3/Opus/Vorbis/other/no-audio → raw, probe fault → structured warn+raw (movie id + probe-failed marker; cancellation ≠ failure), bounded/malformed/oversized/missing-field probe JSON → warn+raw, valid empty-audio → raw, in-memory cache keyed by path+size+mtime (hit/miss/invalidate), and mid-flight source-version mismatch discard at conversion-start / pre-publish recheck, under `tests/LamuFlix.UnitTests/` (FR-003–FR-006; FR-016; AC8; Step 6 R4 / R7 / R8).
- [ ] T005 [P] [US5] **Build lane**. After T003: Using implement test-first, author failing unit tests for one in-flight per source version, overlapping waiters stay alive, **deterministic reattach-within-grace** reuses the child for the same source version, **grace-expiry at zero waiters kills child and deletes partials**, immediate failure cleanup (no grace delay), atomic publish staged and renamed inside the same scratch directory, Range-abort does not delete completed asset, LRU/idle/version/orphan cleanup never evicts while a reader holds (including open/disposal contention), under `tests/LamuFlix.UnitTests/` (FR-007–FR-009; FR-016; AC6; Step 6 R1 / R5 / R6).

### Implementation

- [ ] T006 [US2] **Build lane**. After T004: **Atomically** introduce Core port `IStreamConversionService` under `src/LamuFlix.Core/Ports/`, mark `docs/adr/0050-browser-stream-audio-conversion-port.md` Accepted, amend `.specify/memory/constitution.md` Principle I enumeration (MINOR 2.0.0 → 2.1.0) with Sync Impact Report and Last Amended, and update `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` `PortWhitelistPattern` to include `IStreamConversionService` — no unrelated Principle or whitelist edits. Implement probe JSON parsing (bounded required fields), selected-track/codec policy, memory cache, and version recheck behind that port + Infrastructure adapter under `src/LamuFlix.Infrastructure/Streaming/`, reusing `IProcessRunner` / `ThumbnailOptions.FfmpegPath` with Q3 sibling ffprobe lookup. Carry structured processing/error logging with movie id and probe-failed bypass marker. Make T004 pass. Keep `LamuFlix.ArchitectureTests` green. No LocalPlay folder edits; no secrets; authorize/resolve before any child; args are server-resolved paths plus generated scratch filenames only (Step 6 F1).
- [ ] T007 [US1] [US5] **Build lane**. After T005 and T006: Implement conversion (video copy + selected audio transcoded to AAC stereo → completed MP4), shared in-flight work with the 1 s internal reattachment grace, stage+atomic publish inside the same resolved scratch directory (including absolute configured path), reader tracking, instance-scoped lifecycle, owned-orphan startup cleanup, and Core typed options/validator with startup validation, content-root resolution for relative scratch paths in Infrastructure, and the four `[assumed]` scratch defaults from `plan.md` in `src/LamuFlix.Infrastructure/Streaming/` / `src/LamuFlix.Core/Options/`. Kill-on-cancel via existing runner; change `src/LamuFlix.Infrastructure/Thumbnails/SystemProcessRunner.cs` **only** if AC6 requires it and cite AC6 in the commit/receipt. Make T005 pass. No library writes; no wall-clock conversion timeout; no static mutable lifecycle state (Step 6 R1 / R5 / F4 / F5 / F7).
- [ ] T008 **Build lane**. After T007: Register the service/options via `src/LamuFlix.Infrastructure/Streaming/StreamingServiceCollectionExtensions` at the exact DI composition site confirmed in T001, with instance-scoped lifetime, owned-orphan startup cleanup hook, and startup validation for the new typed options. Ensure `Features:LocalPlay` is neither read nor written. If the confirmed DI site is outside the authorized envelope, **STOP** for Q5 return ruling (Step 6 F4).

**Checkpoint**: Unit suites for US2 decisions and US5 lifetime pass before endpoint wiring.

---

## Phase 4: User Story 1 + US3 — wire stream endpoint (Priority: P1)

**Goal**: Thin dispatch in `StreamEndpoints` serves raw or completed converted MP4 with 200/206/416 and `video/mp4` for converted (FR-001–FR-002, FR-010–FR-011).
**Independent Test**: Integration tests on the route with fixture and status/range behaviour.
**Order**: After Phase 3 checkpoint.

- [ ] T009 [US1] [US3] **Build lane**. After T008: Using implement test-first, author failing extensions in `tests/LamuFlix.IntegrationTests/StreamEndpointTests.cs` for named small AC3 fixture → copied-video / AAC stereo metadata evidence (missing tool/fixture prerequisite → explicit blocker per T001), converted `video/mp4` 200/206/416 seek behaviour, absolute scratch-path stage/publish coverage, explicit raw response/Range parity, known conversion failure on the existing API error path with no raw fallback or redirect, denied/missing/invalid current responses with no child invocation, and LocalPlay-off raw parity retention (FR-001, FR-002, FR-009, FR-010, FR-016; route seams `recon-DEV-572` lines 13–15; LocalPlay-off case `StreamEndpointTests.cs:466-488`; Step 6 R5 / F6).
- [ ] T010 [US1] [US3] **Build lane**. After T009: Wire raw-versus-converted dispatch in `src/LamuFlix.Api/Endpoints/StreamEndpoints.cs` only after authorize/resolve; keep auth/resolution/dispatch thin; serve completed scratch with range processing and Content-Type `video/mp4` when converted; preserve existing problem responses; structured processing/error logs carry movie id; no secrets and no library writes. Make T009 pass.
- [ ] T011 [US5] **Build lane**. After T010: Add/adjust **required** integration coverage for concurrent shared conversion, disconnect/reattach across the 1 s grace window, grace-expiry cleanup when still at zero waiters, and cleanup-without-racing-readers in `StreamEndpointTests.cs` / adjacent tests — not optional or “as feasible” (FR-007–FR-008; FR-016; Step 6 R1). Unit layer retains T004/T005 intent for decision and lifetime mechanics.

**Checkpoint**: Route integration cases for convert/raw/range/auth/cleanup authored and green.

---

## Phase 5: Automated verification checkpoint (closes local Build)

**Goal**: Record automated test pass evidence for FR-016 before downstream gates/acceptance.
**Independent Test**: `dotnet test` filter evidence for new/changed tests.

- [ ] T012 **Build lane**. After T011: Run the unit, `StreamEndpointTests`, and `LamuFlix.ArchitectureTests` suites covering T004–T011 scope; ArchitectureTests MUST be green for `IStreamConversionService` after the atomic Principle I / `PortWhitelistPattern` / ADR 0050 Accepted delivery; record pass evidence or an explicit blocker distinct from a passed checkpoint. Do **not** assert hosted merge readiness or Chromium acceptance here.

**Checkpoint**: Local Build ends. Downstream Cog/Gauge/review/acceptance follow — not Anvil.

---

## Downstream pipeline (Conductor-owned; not Anvil local phases)

After Phase 5 Build checkpoint **pass**, Conductor sequences:

1. **Cog** `/refactor` over the whole build.
2. **Gauge** Phase 3 step 5: Roslyn, cyclomatic complexity, InspectCode, scoped mutation for production C#, property tests under standing task-note rules, vulnerable packages, format, `dotnet test` including `LamuFlix.ArchitectureTests`. Apply `plan.md` Verification Approach labels. Confirm ADR 0050 is Accepted and the Principle I / `PortWhitelistPattern` atomic delivery is present.
3. **Gauge** pre-pass, then **Sentry / Ledger / Compass** Size L three-axis review. **Keel** adjudicates and owns the findings artifact. Unresolved Critical/High or AC/gate/trust-boundary failures block; two rounds max then Bernstein (FR-017; Q7).
4. **Phase 5** rebase and Gauge gate rerun on the rebased head.
5. **Post-build acceptance** on Compose and home cluster against the integrated route (FR-015): on the three named converted samples (959, real AC3, real DTS) — audible AAC and video-copy output evidence; full duration; both seek modes within 3 s; old-range abort and new-range continuity; subtitle / subtitle-delay / existing-control parity; three fault/cleanup cases (forced failure, mid-conversion disconnect observed after the 1 s grace, cancelled seek) leave no process/partials; and separately, supported-codec raw cold/warm baseline comparison; SMOKE-001–003. Preserve pre-build cluster (tool/scratch + one cold timing in plan measurement tables) vs these post-build full playback bars; do not invent a new cluster timing threshold.

---

## Parallel opportunities

- T004 and T005 may run in parallel after T003 (different test files).
- T006 depends on T004; T007 depends on T005 and T006; do not parallelise same-service implementation blindly.
- No parallel production edits to `StreamEndpoints.cs` across tasks.

## Implementation strategy

1. Gate 1 merge receipt.
2. Pickup recon.
3. Proof pass with measurements recorded (MVP gate for the delivery model).
4. Service + units (raw decisions and lifetime).
5. Endpoint wire + integration (audible converted path).
6. Downstream gates, Size L review, then Chromium acceptance on both deployments.

**Suggested MVP**: Proof + US1 converted audible path with US2 raw parity and US3 seek on Compose.
