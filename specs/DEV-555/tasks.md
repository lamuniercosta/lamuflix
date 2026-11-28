---
description: "Task list for DEV-555 Subtitles BSPlayer provider"
---

# Tasks: Subtitles BSPlayer provider

**Input**: Design documents from `specs/DEV-555/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q5 and D1–D15), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `contracts/bsplayer-soap-adapter.md`; Maestri notes `DEV-555`, `recon-DEV-555`; `specs/PRODUCT.md`.

**Status**: Draft

**Tests**: Required (`brief.md` Task order; FR-001–FR-012; SC-001–SC-006). TDD optional. Use existing UnitTests / IntegrationTests homes. No new test framework/dependency.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence tasks are never `[P]`.

**Conventions**: Edit only the authorized envelope in `plan.md` / brief Approach (Q4); STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` / `needs decision` if a required fact/decision is missing; no new NuGet/npm; never lower harness thresholds; no explanatory comments on changed C# beyond AGENTS exemptions; copy no AGPL HolyArrs source; new or changed production code has no `static readonly` and no mutable static state (D14) — that covers SOAP templates, `XmlReaderSettings` and any session cache (use `const`, locals or instance members instead). Named seats own Spec PR, pickup, refactor, gates, review, ship, and live receipt per `task-pipeline`. Cite constitution principles, not line numbers.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: After plan challenge clearance, Bernstein coordinates; Rigger prepares/opens the Spec Kit PR; only the user merges; Keel records Gate 1 merge receipt. No Build tasks (T001+) before Gate 1 (T000). No structural owner checkbox on this ticket (Q1, Q4).

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR + T000          (Bernstein/Rigger open; user merges; Keel Gate 1) — blocks T001+
Phase 1   T001                    (Wisp pickup: DEV-551 + DEV-549 + FakeTimeProvider + authorized paths; Keel reanalysis) — blocks production edits
Phase 2   T002–T003               (Recorded SOAP evidence + scrub before reliance)
          Keel Constitution Check recheck against T003 evidence note — blocks T004
Phase 3   T004–T010               (US1/US2 options + adapter + contract suite + hash + deadline proofs)
Phase 4   T011–T012               (US3 docs + config-state health)
Phase 5   T013–T015               (Cog refactor + Gauge delivery-diff gates + Size M review)
Phase 6   T016–T017               (Ship checkpoint then home-cluster live receipt / down-service note)
```

Ordering per `brief.md` Task order. Recorded evidence before adapter reliance. Keel Constitution Check recheck after T003 before T004 (Ledger L5). Live receipt remains open until Phase 6 — do not claim it from planning.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1+)

**Purpose**: Produce/open the Spec Kit PR, then confirm ordinary owner Gate 1 merge before delivery tasks.
**Checkpoint**: Spec PR opened; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-555 Spec Kit PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 Spec Kit PR merge receipt for DEV-555 in `specs/DEV-555/CONCLUSIONS.md`. Receipt only. T001–T017 MUST NOT proceed without verified merge (`brief.md` Approach; no structural owner checkbox — Q1/Q4).

---

## Phase 1: Pickup recon (blocks production edits)

**Purpose**: Confirm DEV-551 seam/suite/`AllowPlainHttp` contract, DEV-549 fake layout, FakeTimeProvider availability, and authorized paths. Read-only.
**Checkpoint**: Written pickup receipt cites prerequisites and authorized files; Keel read-only reanalysis clear.

- [ ] T001 After T000: **Wisp** obtains Phase B pickup recon in the Build worktree confirming (1) DEV-551 provider seam, shared contract suite entrypoint, `AllowPlainHttp` / provider-local validation contract, and the DEV-551 provider-identifier mechanism for functional health/telemetry names (hard dependency; Q1 — cite as contract, was spec-only at grill HEAD), (2) DEV-551 per-provider options shape, the binding site for a named `SubtitleProviderOptions` instance, and the OpenSubtitles download-cap precedence against global `Subtitles:MaxSubtitleDownloadBytes` (D11), (3) DEV-551 cancellation semantics for caller-token propagation (D4), (4) DEV-549 fake layout path for recorded SOAP fixtures (`tests/fakes/bsplayer/` is a preferred layout — T001-cited pickup fact; not verified in recon-DEV-555), (5) exact registration/health/options paths and lines for minimum BSPlayer wiring (`recon-DEV-555:12–20` patterns — `SubtitleProviderServiceCollectionExtensions`, `SubtitleProviderHealthCheck`, Core options validators); note that home settings are untracked under Principle VII and T004 edits no home file (D13), (6) whether test projects already reference FakeTimeProvider / `Microsoft.Extensions.TimeProvider.Testing` (Q3 — any new test package needs Patron ruling), (7) existing OpenSubtitles hash path, `MovieHash.cs` touch points, and whether an OpenSubtitles golden-value hash test already exists (D6), (8) subtitle setup doc surfaces covered by `SubtitleSetupDocumentationTests` (README / `docker/api.settings.example.json` / `tests/LamuFlix.UnitTests/SubtitleSetupDocumentationTests.cs` are T001-cited pickup facts — not verified in recon-DEV-555; confirm whether `docker/api.settings.example.json` is a setup-doc surface for the T011 example block), (9) mutation applicability for production C# scope. **Keel** reanalyzes brief/spec/plan/tasks against pickup HEAD **read-only**. **STOP** `needs recon:` on missing seams; **STOP** for cited Patron ruling before any edit outside the Q4 envelope. **Strictly read-only reconnaissance and analysis**.

**Checkpoint**: T002+ unlocks only with a passing Wisp receipt and Keel pickup analysis clear.

---

## Phase 2: Recorded SOAP evidence (blocks adapter reliance)

**Purpose**: Capture and scrub a BSPlayer exchange establishing session sequencing, wait necessity, known-file hash, and download-host shape before implementation relies on them (Q2; brief Task order step 1; D7).
**Checkpoint**: Scrubbed fixtures exist at the DEV-549 location; evidence notes recorded for the four prerequisites; D7 second-capture applied if needed; Keel Constitution Check recheck complete before T004; no AGPL copy.

- [ ] T002 **Build lane / operator evidence**. After T001: Record a BSPlayer SOAP exchange against a **deterministic test-generated known file** — generated in the test from deterministic bytes; no media or binary file committed (D10) — covering session open, search, download link shape, and close as observed (SC-002 / T008 reuse this regenerable input). Store scrubbed mappings/bodies under the T001-cited DEV-549 layout (preferred `tests/fakes/bsplayer/` — T001-cited pickup fact). Redact anonymous session tokens before storing (FR-003, FR-005; Q2). Use HolyArrs notes for orientation only; copy no AGPL source. **If capture fails** (no BSPlayer server answers during capture): **STOP** and report blocked with the attempt evidence; never synthesize fixtures or wire behaviour from HolyArrs notes. **If the capture for the generated file returns no candidates or no download link** (D7): the operator makes a second capture — a search for a home-library movie that BSPlayer knows, using the hash and size LamuFlix computes on the home cluster; commit only the scrubbed SOAP bodies and that hash and size, never media bytes; download-host shape and hash acceptance (a non-empty match for the LamuFlix-computed hash) come from that capture; **if the second capture also yields no download link, STOP blocked for a ruling**; no allowlist is assumed (Q2).
- [ ] T003 **Build lane**. After T002: Write an evidence note at `C:\Users\lamun\AppData\Local\LamuFlix\artifacts\LamuFlix\DEV-555\receipts\` (PRIVATE_TICKET_DIR/receipts/) stating for each prerequisite whether the recording establishes (a) session open/close sequence, (b) whether an 8 s wait is required, (c) known-file hash match expectation for the regenerable test file (or D7 second-capture hash-acceptance proof — a non-empty match for the LamuFlix-computed hash), (d) download-host shape (allowed hosts are always the union of `Servers` and `DownloadAllowedHosts`; empty `DownloadAllowedHosts` is valid — D3/D15). If D7 second capture was used, record that branch and the committed hash/size. If the recording requires a new destination or a redirect, **STOP** for an explicit Patron ruling before T004+ relies on it (FR-005; Q2). After the evidence note: **Keel** reruns the Constitution Check against that note (wait, host shape, or hash may change the plan) before T004 (Ledger L5; brief Recheck).

**Checkpoint**: Evidence cleared (or ruled) and Keel Constitution Check recheck complete before options/adapter implementation relies on wire behaviour.

---

## Phase 3: User Stories 1–2 - Options, adapter, contract suite, hash and deadline proofs (Priority: P1) 🎯 MVP

**Goal**: Named-instance `Subtitles:BsPlayer` settings, bounded SOAP adapter, DEV-551 suite green, hash proof, FakeTimeProvider failover/all-down, safety refusals.
**Independent Test**: SC-001–SC-004.

### Options and adapter

- [ ] T004 [US1] **Build lane**. After T001, T003, and Keel Constitution Check recheck: Bind a named instance of the existing `SubtitleProviderOptions` from section `Subtitles:BsPlayer` at the T001-cited registration site without `ValidateOnStart` (D11) — reuse `RequestTimeout`, `DownloadAllowedHosts`, `MaxDownloadBytes`, DEV-551 `Enabled` and `AllowPlainHttp`; add only `Servers` (`string[]` of absolute URLs) and `MaxResponseBytes` (`long`); both stay inert for OpenSubtitles; BSPlayer ignores `ApiKey`, `BaseUrl`, `UserAgent` and `DownloadLinkHost`; keep OpenSubtitles validation byte-identical and existing validator tests unchanged; follow the OpenSubtitles download-cap precedence against global `Subtitles:MaxSubtitleDownloadBytes` (cited at T001); if DEV-551's landed shape makes a named instance impossible, **STOP** for a ruling. Apply D15 validator rules (when `Enabled=false`: nothing validated, no calls, health disabled; when `Enabled=true`: unusable with zero calls if `Servers` empty / non-absolute / bad scheme / `http` while `AllowPlainHttp=false` / IP-literal / userinfo, or `RequestTimeout ≤ 0` / `MaxResponseBytes ≤ 0` / `MaxDownloadBytes ≤ 0`, or any `DownloadAllowedHosts` entry blank or containing `*`; empty `DownloadAllowedHosts` valid; `https` entries valid when `AllowPlainHttp=true`). Repository `appsettings` default `Enabled=false`. **T004 edits no home file** (D13). No guessed numeric or allowlist defaults. No `static readonly` / no mutable static state in new or changed production code (D14). Honour Principle VII acquisition ValidateOnStart exception (provider-local off/unusable, host continues) (FR-002; Q1–Q2; D11; D13–D15). Cover D15 with Theory + MemberData tests (Principle IX).
- [ ] T005 [US1] **Build lane**. After T003–T004: Implement `src/LamuFlix.Infrastructure/Adapters/BsPlayerSubtitleProvider.cs` on the DEV-551 `ISubtitleProvider` seam — movies-only file-hash(+size) search with IMDb id when present; one provider deadline with D1 attempt budget (`share = (remaining − W) / untried`; attempt = session open only; no failover after open; close gets no reserved budget and never fails returned results); configured list order; session token local to each search call; adapter stateless apart from injected services (D2); close awaited within remaining budget with exceptions logged scrubbed and swallowed; D3 destination predicate via one shared refusal gate extending the existing host-refusal service; redirects disabled (any 3xx failed attempt); D5 XML (`XElement`/`XmlWriter` build; counting-stream abort before parsing at `MaxResponseBytes`; `XmlReader` with `DtdProcessing.Prohibit`, `XmlResolver = null`, `MaxCharactersFromEntities = 0`, `MaxCharactersInDocument` from bound); download size from configured `MaxDownloadBytes` with global-cap precedence; existing SRT validation; session scrub; injected `TimeProvider` (`CancellationTokenSource(timeout, timeProvider)` / `Task.Delay(…, timeProvider, …)`; infinite `HttpClient.Timeout`); D4 linked-token cancellation classification (caller → propagate as DEV-551 specifies, never `failed`; provider deadline → `failed` / “BSPlayer is not answering”; attempt-only → next server); all-down → `failed` / “BSPlayer is not answering” (FR-001, FR-003, FR-004; Q2–Q3; D1–D5; `contracts/bsplayer-soap-adapter.md`). Apply T003 evidence for wait/hash/download-host. If T003 proves hash differs: add a separately named functionally named routine in `MovieHash.cs`; BSPlayer calls it; existing routine stays byte-identical; existing OpenSubtitles hash tests unchanged (D6); otherwise leave `MovieHash.cs` unchanged (FR-009). No `static readonly` / no mutable static state (D14).
- [ ] T006 [US1] **Build lane**. After T005: Minimum registration/composition touch to register BSPlayer typed client (`AddHttpClient<BsPlayerSubtitleProvider>`) and adapter on the T001-cited Infrastructure/Api registration paths; serialize with other subtitle-provider tickets if they touch the same file (FR-009; Q4; D12).

### Tests

- [ ] T007 [P] [US2] **Build lane**. After T002 fixture placement and T006: Wire recorded SOAP fake into IntegrationTests and run the DEV-551 shared contract suite against BSPlayer until green (FR-006; SC-001; Wanted 5). WireMock covers the wire shape only (D8).
- [ ] T008 [US2] **Build lane**. After T007: Add known-file hash assertion that recomputes the hash from the same regenerable deterministic test-generated file recorded in T002 (generated in the test only — no media or binary committed — D10) and compares the outgoing value to the recorded request; or, when D7 second capture applies, prove hash acceptance as a non-empty match for the LamuFlix-computed hash (SC-002). If T003 proves hash differs, verify the separately named routine is used and that existing OpenSubtitles hash tests stay unchanged and green; add an OpenSubtitles golden-value test only if T001 cites none exists (D6). Do not commit copyrighted media (FR-006; SC-002; Done when).
- [ ] T009 [US1] **Build lane**. After T006–T007: Timing tests run the real adapter over a stub `HttpMessageHandler`; the test owns `FakeTimeProvider.Advance`; no wall-clock assertions; WireMock is not used for timing (D8). Cover: (D1-i) ≥3 servers with server 1 hanging, server 2 refusing and server 3 answering — assert server 3 is reached and results return inside the deadline; (D1-ii) close with zero remaining budget still succeeds (close skipped, results return as success); (D2) two concurrent searches on the recorded fake returning a distinct token per open — each search and close carries its own token; (D4) caller cancellation during a probe yields cancellation, not `failed` and not the timeout text; all-servers-down within timeout with sibling provider results present; disabled/invalid settings → zero outbound calls while existing-track and languages routes still serve; D3 destination-table tests asserting zero contact for: relative URL, protocol-relative URL, IP literal, trailing-dot or case variant (allowed when it matches, refused when it does not), disallowed port, userinfo, and `http` with `AllowPlainHttp=false`; D5 over-bound body aborted before parsing and DOCTYPE with internal-entity expansion payload refused; oversize/non-SRT rejection; session-token scrub in fixtures/logs; log scrub: captured logs contain no token and do contain the movie id (D9); D15 Theory + MemberData validator coverage if not already under T004 (FR-003, FR-004, FR-006; SC-003, SC-004; Q3; D1–D5; D8–D9; D15).
- [ ] T010 **Gauge checkpoint**. After T007–T009: Run ArchitectureTests and the three static gates on this phase’s changed `.cs` (`scripts/run-roslyn-analyzers.ps1`, `scripts/run-cyclomatic-complexity.ps1`, `scripts/run-jetbrains-inspectcode.ps1`, no `-All`). Record verdicts. Preserve existing OpenSubtitles assertions and read-path regressions.

**Checkpoint**: Green T004–T010 unlock docs/health.

---

## Phase 4: User Story 3 - Setup docs and config-state health (Priority: P2)

**Goal**: Document BSPlayer plain HTTP + settings; register config-state-only health.
**Independent Test**: SC-005.

- [ ] T011 [P] [US3] **Build lane**. After T004: Update subtitle setup docs on the T001-cited surfaces (`SubtitleSetupDocumentationTests`, README, and `docker/api.settings.example.json` — pickup facts not verified in recon-DEV-555) and extend `tests/LamuFlix.UnitTests/SubtitleSetupDocumentationTests.cs` for BSPlayer settings (`Enabled`, `Servers`, `RequestTimeout`, `MaxResponseBytes`, `MaxDownloadBytes`, `DownloadAllowedHosts`, `AllowPlainHttp`) and an explicit plain-HTTP statement; keep repository default off. When T001 confirms `docker/api.settings.example.json` as a setup-doc surface, add the full `Subtitles:BsPlayer` example block there (D13) (FR-008; SC-005; Wanted 6).
- [ ] T012 [US3] **Build lane**. After T006: Add config-state-only BSPlayer health registration on the T001-cited health seam with no live reachability probe; verify no session tokens in health data; reuse existing and DEV-551 provider spans, metrics and attribute keys with no new attribute keys; server host and attempt number in structured logs only (never as telemetry tags); logs carry movie id, attempt number and failure category and never the session token (FR-007; Q4; Principle VI; D9; D12). Identifiers per D12 (`BsPlayerSubtitleProvider`; section-key const; functional health/telemetry names via DEV-551 mechanism). If the DEV-551 provider identifier requires a new vendor-named `TelemetryConstants` entry, **STOP** and route as `blocked: structural` (PRODUCT §5 b); do not add the entry. **STOP** for cited Patron ruling before editing out-of-envelope telemetry paths. Add or confirm the log-scrub test (captured logs contain no token and do contain the movie id) here or under T009.

**Checkpoint**: Green T011–T012 unlock refactor/gates/review.

---

## Phase 5: Polish — refactor, delivery-diff gates, Size M review

**Purpose**: Meet Size M backend quality bar under Q5 on the delivery diff.
**Checkpoint**: Actual gate verdicts recorded; review dispositions under Q5; no undeserved PASS.

- [ ] T013 **Cog**. After T010–T012: Run refactor gate `./scripts/run-cyclomatic-complexity.ps1 -Threshold` from `harness.yml` refactor setting on changed production C#; extract helpers as needed; no threshold edits.
- [ ] T014 **Gauge**. After T013: Run Size M backend matrix on the **initial delivery diff** — Roslyn, cyclomatic complexity, InspectCode, property tests, vulnerable packages, format, `dotnet test`; mutation for production C# scope at harness threshold with **actual** verdict preserved; on mutation share/span failure, correct deficient tests while retaining configured thresholds; web gates `SKIPPED (scope-empty)` if no web files; note OpenAPI drift check via existing CI gate (`brief.md` Task order step 3; SC-006).
- [ ] T015 **Size M review**. After T014: Risk (Sentry), Standards (Ledger), Spec (Compass) against Q5 closing bar — Critical/High/Medium and accepted in-scope Low block until fixed and re-verified; disposition all findings; max two rounds; out-of-scope Critical/High or broken behaviour → YouTrack follow-up via Rigger; other out-of-scope noted (Q5).

**Checkpoint**: Review closed under bar; ready for ship checkpoint then live receipt.

---

## Phase 6: User Story 4 - Ship checkpoint then home-cluster receipt (Priority: P2)

**Goal**: Gauge finishes Size M matrix on rebased delivery head before opening the delivery PR; then attach scrubbed live search+download or explicit down-service note.
**Independent Test**: SC-006 live half; Done when bullet on real search.

- [ ] T016 **Ship checkpoint (Gauge) — before opening delivery PR**. After T015 closed and after Rigger rebases/pushes the delivery branch: **Gauge** re-runs the Size M matrix on the **rebased delivery head** with actual verdicts. **T016 MUST finish before the delivery PR is opened.**
- [ ] T017 [US4] **Rigger / operator evidence — after T016, on delivery PR**. After T016 and after the delivery PR is opened: Attach a real home-cluster BSPlayer movie search and one successful download (scrub credentials/session tokens). List the exact keys the operator sets in the untracked home settings (`docker/api.settings.json` or the `api-settings` Secret) before the live search: at minimum `Subtitles:BsPlayer:Enabled=true`, `Servers`, `RequestTimeout`, `MaxResponseBytes`, `MaxDownloadBytes`, `DownloadAllowedHosts`, and `AllowPlainHttp: true` (D13). If the service is down, record that explicitly on the PR and keep DEV-555 open until a real search has worked; recorded fixtures remain automated acceptance (FR-010; SC-006; Q5; Done when).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (T000)**: Blocks all Build work.
- **Phase 1 (T001)**: Blocks production edits; read-only prerequisite inventory.
- **Phase 2 (T002–T003)**: Blocks adapter reliance on unverified wire details; Keel Constitution Check recheck after T003 blocks T004.
- **Phase 3 (US1/US2)**: After evidence + recheck; contract suite + hash + deadline proofs.
- **Phase 4 (US3)**: After options/registration exist.
- **Phase 5**: After T010–T012.
- **Phase 6**: After T015; T016 before opening delivery PR; T017 after T016 on that PR.

### User Story Dependencies

- **US1**: Depends on DEV-551/549 pickup and recorded evidence for session/wait.
- **US2**: Shares adapter with US1; depends on recorded fixtures for suite/hash/safety.
- **US3**: Depends on options shape (T004) and registration (T006).
- **US4**: After gates/review; live evidence not a CI substitute.

### Parallel Opportunities

- T004 is sequential after T001, T003, and Keel Constitution Check recheck (D11 named-instance binding and D15 rules); it is not `[P]` and has no parallel peer at that point.
- T007 suite authoring can start once fixtures (T002) and registration (T006) exist.
- T011 docs after T004 may proceed while T012 health finishes (different files).
- Do **not** mark same-file adapter/registration edits `[P]`.

---

## Implementation Strategy

### MVP First (evidence + US1/US2)

1. Gate 1 + pickup + recorded SOAP evidence + Keel Constitution Check recheck
2. Named-instance options + adapter + registration
3. Contract suite + hash + stub-handler FakeTimeProvider all-down / D1–D5 proofs
4. STOP and validate SC-001–SC-004

### Incremental Delivery

1. Docs + health (US3)
2. Delivery-diff gates/review → rebased-head ship checkpoint → live receipt or down-service note (US4)

### Notes

- Confirm DEV-551 landed at T001 before Build relies on the seam/suite/`AllowPlainHttp` contract.
- Do not invent 8 s wait, hash algorithm changes, or download hosts without T003 evidence.
- Configured list order remains `[assumed]` in `ASSUMPTIONS.md`.
- Quill invents no decisions; gaps → `needs decision:` in `DRAFTING_RECEIPT.md`.
