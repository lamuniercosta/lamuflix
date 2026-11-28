---
description: "Task list for DEV-554 SubDL provider and shared archive reader"
---

# Tasks: SubDL provider and shared archive reader

**Input**: Design documents from `specs/DEV-554/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q7 + Step 3.3), `CONCLUSIONS.md`, `contracts/subdl-v2-adapter.md`; Maestri notes `DEV-554`, `recon-DEV-554`; `specs/PRODUCT.md`.

**Status**: PLAN FROZEN (2026-11-28). Gate 1 provisional — Principle VII SubDL amendment drafted on this branch (v2.1.0 → 2.2.0, Sync Impact Report cites DEV-501 / ADR 0041); awaiting Keel reread before Rigger marks PR #523 ready. Owner checkbox ticked; caps 8 MiB / 32 MiB / 128 approved. T000 still requires merged amended constitution on `origin/main` and landed DEV-551 before Build.

**Tests**: Required (`brief.md` Test and acceptance evidence / Q6; FR-001–FR-015; SC-001–SC-007). TDD optional. Use existing integration/unit homes and packages only. No new test framework/dependency.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence tasks are never `[P]`.

**Conventions**: Edit only the authorized envelope in `plan.md` / brief Approach; STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` / `needs decision` if a required fact/decision is missing; no new NuGet/npm package; no new port/layer/schema/public API; no LocalPlay/`Process.Start`; never commit keys; never lower harness thresholds; no explanatory comments on changed C# beyond AGENTS exemptions. Named seats own Spec PR, pickup, refactor, gates, review, ship, and live receipt per `task-pipeline`. Vendor-neutral Core names: `ArchiveSubtitleProviderOptions` / `ArchiveSubtitleProviderOptionsValidator`.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: After plan challenge clearance, Bernstein coordinates; Rigger prepares/opens the Spec Kit PR carrying the narrow VII amendment draft, constitution version bump, Sync Impact Report, and the **unchecked** owner checkbox from `brief.md:99` / `spec.md` Owner question. Only the user ticks and merges. Keel records Gate 1 merge receipt. No Build tasks (T001+) before T000 evidence passes. Quill does not claim the amendment is already on `origin/main`.

**Governance (Step 3.3)**: Principle VII amendment + owner checkbox are explicit Gate 1 / T000 prerequisites. Owner rejection → revert amendment on the spec branch and return DEV-554 for §2.3(a) re-scope; no workaround.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR + T000 evidence   (Bernstein/Rigger open; user ticks/merges; Keel Gate 1; T000 checks) — blocks T001+
Phase 1   T001                      (Wisp pickup: DEV-551 + exact 409/health/registration paths + client redirect inventory + Principle VI + Q2 attribution/reset + test-only HTTPS + verified year mapping + fixture location; Keel reanalysis) — blocks production edits
Phase 2   T003 → T002 → T004        (US1 reader call shape, then unit+FsCheck tests, then verification; not parallel)
Phase 3   T005–T010                 (US2 options + SubDL adapter + explicit 409 impl + health registration + separate composition evidence + contract suite)
Phase 4   T011–T012                 (US3 credential sentinel only after Phase 3 checkpoint + setup docs)
Phase 5   T013–T015                 (Cog refactor + Gauge delivery-diff gates + Size M review)
Phase 6   T017 then T016            (Gauge ship checkpoint on rebased head finishes BEFORE opening delivery PR; then home-cluster receipt on delivery PR)
```

Ordering per `brief.md` Task order, Step 3.3, and Step 6 Quill fix list. No dependency cycle. Live home-cluster receipt remains open until Phase 6 — do not claim it from planning. T014 covers the initial delivery diff only. **T017** is the named ship checkpoint on the rebased head and MUST finish **before opening the delivery PR**; **T016** then attaches the live receipt to that delivery PR.

---

## Phase 0: Spec PR, Gate 1, and T000 evidence (blocks Phase 1+)

**Purpose**: Open the Spec Kit PR with the VII amendment draft and unchecked owner checkbox; after owner merge, verify T000 evidence before any Build work.
**Checkpoint**: Spec PR opened with amendment + Sync Impact Report + unchecked checkbox; verified user-merge receipt in `CONCLUSIONS.md`; T000 evidence recorded; or stop on rejection path. No Build tasks before that receipt.

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-554 Spec Kit PR carrying: (1) the narrow Principle VII amendment that **adds** the typed `Subtitles:SubDl` options entry to the closed list (constitution.md:782-784), **replaces** the existing singular-provider 409 clause (:795-801) with Step 3.3 provider-set search and chosen-provider download rules, and **updates** the corresponding VII checklist (:946-948) — vendor-neutral Core options/validator; effective-disabled vs keyed-invalid; required positive caps; preserve OpenSubtitles binding and validation, `SaveEnabled`, read-only precedence, existing-track serving, languages, and secret isolation while stating that search aggregation **changes** for blank OpenSubtitles with valid SubDL, (2) constitution version bump + Sync Impact Report that records DEV-501 / ADR 0041 impact (any separate ADR edit needs a cited Q4 ruling), and (3) the unchecked owner checkbox exactly: `blocked: structural — approve the VII amendment adding the SubDL acquisition options exception? With the approved caps and valid SubDL settings, search returns SubDL results even when the OpenSubtitles key is blank. Search returns 409 only when no provider has effective, valid settings.` Only the user ticks and merges. After merge, **Keel** records the verified Gate 1 Spec Kit PR merge receipt in `specs/DEV-554/CONCLUSIONS.md`. **T000 evidence (all required; record each)**: (a) owner checkbox ticked on the merged spec PR (`gh pr view`); (b) `origin/main:.specify/memory/constitution.md` contains the amended VII clause (options entry + replaced 409 clause + checklist) and the new version line; (c) DEV-551 provider seam and per-provider state are on main (absent at spec HEAD per `recon-DEV-554:111`; 409 rule depends on them); (d) owner-approved cap values (example/home 8 MiB / 32 MiB / 128 subject to the checkbox). If any check fails, T000 fails and no Build starts. **Rejection path**: if the owner rejects, revert the constitution edit on the spec branch, return DEV-554 to the owner to re-scope under §2.3(a), and do not start Build (no workaround; nesting under `SubtitleProviderOptions` still breaks the `Subtitles:OpenSubtitles` path clause). Quill does not claim the amendment is already applied or approved before this evidence. T001–T017 MUST NOT proceed without verified T000 pass (`brief.md` Step 3.3 / Step 6; FR-015).

---

## Phase 1: Pickup recon — DEV-551, year mapping, fixture location (blocks production edits)

**Purpose**: Confirm hard prerequisites and refresh authorized paths. Read-only.
**Checkpoint**: Written pickup receipt cites DEV-551 seams, a **verified documented** Q5 year-mapping choice (or an explicit transport lock), Q4 fixture location, and authorized file paths; Keel read-only reanalysis clear. T001 itself does not edit production files.

- [ ] T001 After T000: **Wisp** obtains Phase B pickup recon in the Build worktree confirming DEV-551 has landed (provider registry, per-provider quota state, shared contract suite — **hard prerequisite** per Q4; T000 already required them on main). Inventory **exact registration/composition/health paths and lines** and the shared suite entrypoint. **Explicit 409-implementation pickup**: locate and cite the exact landed DEV-551 search/download paths/lines that today produce or gate `409` (multi-provider effective/valid settings, read-only library precedence on download); **list which of those files require edits for Step 3.3 and which sit outside Q4's registration/composition allowance**. For any out-of-envelope 409 file, **cited Patron approval MUST be recorded in `CONCLUSIONS.md` before T008 edits it**. **Principle VI health-seam pickup** (constitution.md:760-770, :944-945): identify and cite the exact landed subtitle-provider health-check registration seam SubDL must join (grill-HEAD pattern at `recon-DEV-554:85` — `SubtitleProviderServiceCollectionExtensions` registers `SubtitleProviderHealthCheck` as `"subtitle-provider"`; `SubtitleProviderHealthRegistrationTests` covers untagged registration and `/health/live` + `/health/ready`). Record whether landed DEV-551 keeps, renames, or replaces that seam. **Provider identity / VIII**: record how the provider ID reaches DEV-551 state and telemetry as **runtime data** under existing vendor-neutral Core names (including Core test/support types); a proposed vendor-named Core constant requires a constitution ruling (§2.3(b) for any departure) — fail closed. **Reuse pickup** (`recon-DEV-554:83–98`): verify and record compatible landed host-matching, URI-redaction, structured-logging layout (including whether `SubDlSubtitleProvider.Log.cs` matches the LoggerMessage convention), existing SRT seams, typed subtitle-options registration (optional acquisition out of startup validation / no `ValidateOnStart`), and HttpClient patterns. **Client/redirect inventory**: record how search vs download clients are built and whether redirects auto-follow, so T007 can disable automatic following and enforce per-hop checks. **Test-only HTTPS transport**: record an HTTPS WireMock configuration or an isolated test-only transport seam that preserves production HTTPS validation (`recon-DEV-554:85–96` patterns to check). **Q2 attribution + reset representation pickup evidence**: against current SubDL docs + sanitized fixtures, record whether the download-response `X-RateLimit-Limit` / `Remaining` / `Reset` trio is **documented as download usage** and how Reset is represented; cite the evidence (URL/section or fixture note). If attribution or reset representation is absent or ambiguous at pickup, download-quota mapping MUST stay on the `Unknown` fallback (no header mapping, no inferred reset, no `/me`) until a later cited ruling — do not invent attribution (Q2; `recon-DEV-554:110`). If docs name an `nId` shape, record it for T007 single-segment validation. Verify current SubDL v2 title-and-year mapping against live docs and any sanitized fixtures: prefer documented request-year filter; else documented result-year field with filter-before-return. A **verified documented year mapping is an explicit passing condition** for the normal T003→T002 sequence. If neither mapping exists: **STOP** `needs recon` + cited Patron ruling; **leave SubDL transport locked** pending that recon/ruling (Q5; no legacy API) — a documented stop is **not** a green year-mapping pass and MUST NOT unlock T003+ transport/adapter work. Establish sanitized recorded WireMock fixture location: landed DEV-549 path if present, else **Conductor-confirmed** location before any fixture write (Q4; one fake framework; DEV-549 remains the preferred placement fallback path). Refresh allowed file list against landed seams (`brief.md` Approach). If T008 health registration or any new telemetry name would require a tracked path outside the Q4 / Step 3.3 approved envelope (for example `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs`), **STOP for a cited Patron ruling recorded in `CONCLUSIONS.md` before touching that path**. Record mutation applicability/exclusions anticipated verdicts for production C# scope. **Keel** reanalyzes brief/spec/plan/tasks against pickup HEAD **read-only**. If authorized drift needs artifact amendment, record a separate follow-up before T003+ production edits. **STOP** `needs recon:` on missing seams; **STOP** for cited Patron ruling rather than expanding file/protected surface. **Strictly read-only reconnaissance and analysis**.

**Checkpoint**: T003+ unlocks only with a passing Wisp receipt that includes (1) DEV-551 hard-prerequisite confirmation, (2) **exact cited 409/health/registration path+line inventory** for T008 with out-of-envelope 409 files listed and Patron-approved in `CONCLUSIONS.md` before those edits, (3) **cited Principle VI health-check registration seam** for T008 SubDL registration, (4) provider-identity runtime-data / VIII closed check, (5) reuse + client-redirect + test-only HTTPS transport records, (6) **recorded Q2 attribution and reset-representation pickup evidence** (attributed → mapping allowed; absent/ambiguous → `Unknown` fallback locked), (7) **verified documented year mapping** (transport remains locked on Q5 stop), (8) Q4 fixture location (DEV-549 or Conductor-confirmed), Keel pickup analysis clear, and any authorized drift follow-up amendments already applied.

---

## Phase 2: User Story 1 - Shared ZIP archive reader (Priority: P1) 🎯 MVP

**Goal**: Concrete BCL ZipArchive reader with caller-passed caps, unsafe-name rejection, ranking, and distinct safe reasons; selected bytes ready for existing SRT seam. Archive behaviour is independently testable without SubDL HTTP.
**Independent Test**: Unit-project archive tests green for FR-006 / SC-002 without SubDL live calls.

### Implementation then tests for User Story 1 (ordered; not parallel)

- [ ] T003 [US1] **Build lane**. After T001 with verified documented year mapping (transport not locked): Implement `src/LamuFlix.Infrastructure/Adapters/ZipSubtitleArchiveReader.cs` as a concrete BCL `System.IO.Compression.ZipArchive` reader with **caller-passed validated limits only** (the reader supplies **no** numeric defaults / fallbacks; **no mutable static state**). Enforce the compressed-size cap by a **bounded actual-byte read before ZIP open**; a length header may only provide early rejection. Keep limits and counters **per invocation**; propagate cancellation through compressed/expanded reads; dispose stream and ZIP resources according to ownership on all exits including rejection. Use fixed failure reasons and structured safe fields only (omit raw entry names from diagnostics). SubDL-side required settings (example/home config subject to owner approval) are 8 MiB compressed / 32 MiB expanded aggregate / 128 entries and are **not** baked into the shared reader. No disk extraction; entry names as data only; Q3 ranking; distinct safe rejection reasons (FR-006; standing ruling 3–4; Step 3.3 / Step 6). No new NuGet package. Establish the reader call shape before T002 consumes it.
- [ ] T002 [US1] **Build lane**. After T003: Add `tests/LamuFlix.UnitTests/ZipSubtitleArchiveReaderTests.cs` using repo `Method_Condition_Expected` naming and property-gate discovery conventions, covering compressed/expanded/entry cap breaches including zip-bomb, too-many-entries, and **over-cap actual data without a reliable length header**, `../x.srt` and peer unsafe forms (rooted, drive-qualified, either separator), no `.srt`, malformed ZIP, multi-entry ranking per Q3, invalid selected SRT rejected through the existing validation helpers used by `DownloadSubtitleCommandHandler` (`recon-DEV-554:87`), independent concurrent calls, cancellation/disposal on rejection, and absence of untrusted marker text from reasons. Assert distinct fixed safe failure reasons. Add FsCheck property cases for order-independent selection when decisive ranks differ and for limit boundaries using the existing FsCheck package only. Prove archive behaviour **independently** of SubDL HTTP / DEV-551 wiring (FR-006, FR-010; SC-002; Q3, Q6; Step 6 fix 7). WireMock/health/composition stay in IntegrationTests.
- [ ] T004 [US1] **Build lane**. After T003 then T002: Make T002 pass against T003; ensure selected bytes are plain SRT payloads suitable for the existing handler size check + `EnsureValidSrt` without changing `DownloadSubtitleCommandHandler` public behaviour (FR-007 suitability; `recon-DEV-554:7`). T004 remains the phase verification checkpoint. Normal-composition publication/rejection through the live handler path is asserted later in T009, not claimed complete by T004 alone.

**Checkpoint**: Green T003→T002→T004 unlock Phase 3 adapter work that consumes the reader.

---

## Phase 3: User Story 2 - SubDL search, ZIP download, quota, DEV-551 wire (Priority: P1)

**Goal**: Typed vendor-neutral options, guarded v2 adapter, explicit Step 3.3 409 implementation and Principle VI health registration on DEV-551 seams, separate normal-composition evidence, shared suite green.
**Independent Test**: Contract suite + options/409/health-registration cases + **separate** composition evidence for FR-001–FR-004, FR-008–FR-009, FR-013–FR-014 / SC-001, SC-004, SC-007.

### Tests for User Story 2

- [ ] T005 [P] [US2] **Build lane**. After T001 fixture-location clearance and T004: Add sanitized recorded WireMock fixtures for SubDL v2 search/download (and quota header cases) at the Q4 location; scrub all credential material. Add `tests/LamuFlix.IntegrationTests/SubDlSubtitleProviderTests.cs` (name may align to DEV-551 suite invocation recorded at T001) covering IMDb ID, TMDb ID with `type=movie`, title plus year including same-title/different-year, ZIP download success through the archive reader, **guarded-transport cases** (allowed multi-hop redirect, disallowed target with **zero outbound request** to it, hop exhaustion, rejected invalid `nId`, title metacharacters), **HTTP-adapter compressed-cap case** (controlled response stream exceeding the compressed cap **without a reliable length**, proving the adapter enforces the actual-byte bound **without reading to EOF** and disposes the adapter-owned response/stream on rejection — use the existing test-only transport option if needed; T002 already covers the reader-side bound), coherent attributed and unknown download quota (**Unknown** when T001 attribution/reset evidence is absent/ambiguous or when headers are absent/partial/malformed/negative/overflowing/incoherent — without throwing), search counters not filling download quota, zero `/api/v2/me` calls, enablement / blank-key effective-disabled, keyed-invalid / redacted misconfigured provider-state cases, and production configuration rejecting non-HTTPS with **zero outbound calls** (FR-002–FR-006, FR-008, FR-009; SC-001, SC-004, SC-007; Q1, Q2, Q5, Q6; Step 3.3 / Step 6 fix 4 HTTP boundary). **Do not** place normal-composition handler/publication evidence here — that is separate after T008 409 wiring (see T009). Provider-dependent 409 regressions that need the wired registration path land in T008.

### Implementation for User Story 2

- [ ] T006 [P] [US2] **Build lane**. After T001 and T000: Add `src/LamuFlix.Core/Options/ArchiveSubtitleProviderOptions.cs` and `src/LamuFlix.Core/Options/ArchiveSubtitleProviderOptionsValidator.cs` binding section `Subtitles:SubDl` — `Enabled`, `ApiKey`, `BaseUrl`, search/download allowlists, **required positive** archive caps (no code-supplied defaults; example/home use owner-approved 8 MiB / 32 MiB / 128) — following Step 3.3 / VIII vendor-neutral naming and the landed typed subtitle-options pattern (typed section binding; **optional provider out of startup validation** / no `ValidateOnStart`; typed validator applied before outbound use). Effective-disabled when `Enabled=false` or blank key; keyed-invalid → startup ok, zero outbound, redacted misconfigured DEV-551 state. Prove production configuration rejects non-HTTPS with zero outbound calls (FR-001; Q1, Q4; Wanted 1; Step 3.3 / Step 6 fix 5).
- [ ] T007 [US2] **Build lane**. After T004 and T006: Implement `src/LamuFlix.Infrastructure/Adapters/SubDlSubtitleProvider.cs` (and `SubDlSubtitleProvider.Log.cs` if T001 verified that logging convention) implementing `ISubtitleProvider` with v2 search/ZIP download. **Guarded transport**: reuse landed typed/named HttpClient patterns where compatible; disable automatic redirect following; keep search on its validated origin; for ZIP redirects resolve and validate each target as HTTPS and download-allowlisted before requesting, with a finite hop limit and immediate rejection at the first invalid hop; send `X-API-Key` only on an eligible initial request to the validated HTTPS provider origin; every redirected or separately hosted ZIP fetch is credential-free; build request paths from a validated single-segment `nId` (honor the documented shape if T001 verified one) and encode query values structurally. Defensive URI/error redaction with fixed failure reasons and structured safe fields only (omit raw entry names, URLs, identifiers, quota values); include required movie ID context in structured logs where available. Archive reader integration with **caller-passed** SubDL caps and per-invocation limits. **HTTP-adapter compressed bound (fix 4)**: the bounded actual-byte read of the response body applies **before any full response buffering or ZIP open**; `Content-Length` is only an early-rejection aid; on compressed-cap rejection cancel the HTTP request and dispose adapter-owned response/stream (reader-side bound remains T002/T003). Propagate cancellation through HTTP and compressed/expanded reads; dispose response/stream/ZIP resources according to ownership on all exits. Q2 download-quota mapping that **requires T001's recorded attribution and reset-representation pickup evidence** before mapping the coherent download-response header trio into DEV-551 quota types — reject malformed/negative/overflowing/incoherent values to **`Unknown` without throwing**; use injected `TimeProvider` for any relative reset calculation; otherwise **`Unknown` fallback** (absent/partial/malformed/ambiguous headers or missing attribution → `Unknown`; no inferred reset; no `/me`; search counters stay separate) (FR-001–FR-005, FR-008; Q1–Q2; Step 3.3 / Step 6; `contracts/subdl-v2-adapter.md`).
- [ ] T008 [US2] **Build lane — explicit 409 implementation + Principle VI health registration** (not composition evidence). After T001 and T007: Apply the minimum DEV-551 registration change to register SubDL (exact path/lines from T001), including **Principle VI health-check registration** on the T001-cited landed health seam (constitution.md:767-770, :944-945; grill-HEAD pattern `recon-DEV-554:85`). **Implement** Step 3.3 multi-provider 409 semantics only on the T001-listed files — for any file T001 marked outside Q4's registration/composition allowance, **cited Patron approval MUST already be recorded in `CONCLUSIONS.md` before editing it** — preserve public API shape aside from those semantics: (1) search returns SubDL results when SubDL is valid and OpenSubtitles key is blank; (2) search returns valid providers' results (not 409) when SubDL is off/misconfigured and another provider is valid; (3) search returns 409 only when no provider has effective valid settings; (4) download returns 409 for the chosen provider's missing/invalid settings after read-only library precedence; (5) effective-disabled / blank-key leaves SubDL out of the effective-valid set without host failure; (6) keyed-invalid reports redacted misconfigured DEV-551 state and stays out of the effective-valid set. Provider identity in state/telemetry remains runtime data under vendor-neutral Core names. **Health registration safety**: verify SubDL effective-disabled and keyed-invalid leave `/health/live` and `/health/ready` reachable without authentication, do not turn optional acquisition into a failing ready dependency, and leak no credential in health data; any new telemetry name MUST go through `TelemetryConstants` (reuse existing subtitle constants when they already cover the state); a proposed vendor-named Core constant requires a constitution ruling. **STOP for a cited Patron ruling in `CONCLUSIONS.md` before editing any path outside the Q4 / Step 3.3 approved envelope** (including `TelemetryConstants.cs` if a new name is required). Focused 409/provider-state/health-registration tests in the Q4 envelope prove (1)–(6) and the health safety cases. **Composition evidence is not part of T008** — it is T009 (FR-013, FR-014; SC-007 409 half; Q4; Step 3.3; Principle VI; `recon-DEV-554:85` pattern).
- [ ] T009 [US2] **Build lane — separate composition evidence + contract suite**. After T005–T008: **Separate composition evidence** (distinct from T008 409 implementation; focused tests in the Q4 envelope): (1) selected valid SRT reaches `DownloadSubtitleCommandHandler` publication through normal composition; (2) invalid selected SRT is rejected through that same handler path; (3) derived download quota reaches DEV-551 per-provider state only when T001 attribution evidence allows mapping, otherwise `Unknown`. Then run the DEV-551 shared contract suite against the SubDL WireMock fake until green; fix only in-scope adapter/options/fixture issues (FR-007, FR-008, FR-009, FR-010; SC-001; SC-007 composition half; Done when bullet 1).
- [ ] T010 [US2] **Gauge checkpoint**. After T009: Run ArchitectureTests and the three static gates on this phase’s changed `.cs` (`scripts/run-roslyn-analyzers.ps1`, `scripts/run-cyclomatic-complexity.ps1`, `scripts/run-jetbrains-inspectcode.ps1`, no `-All`). Record verdicts.

**Checkpoint (Phase 3)**: Green T005–T010. Phase 4 / T011 MUST NOT start until this checkpoint is complete.

---

## Phase 4: User Story 3 - Credential safety and setup docs (Priority: P1)

**Goal**: Sentinel leakage coverage and operator setup documentation.
**Independent Test**: SC-003 and SC-004 evidence.

- [ ] T011 [US3] **Build lane**. After **Phase 3 checkpoint** (T005–T010 green) and T007: Extend the same file `tests/LamuFlix.IntegrationTests/SubDlSubtitleProviderTests.cs` so a credential sentinel appears in no log line at any level and in no captured trace or surfaced error, including upstream failure and redirected download; assert absence of credentials on **all followed hops** and that untrusted marker text (entry names, URLs, identifiers, quota values) is absent from reasons, logs, traces, and errors (FR-005, FR-010; SC-003; Q1, Q6; Step 6 fixes 3–4; Done when bullet 3). **Not `[P]`**: same-file edit as T005; MUST wait for the full Phase 3 checkpoint — do **not** overlap T008–T010.
- [ ] T012 [P] [US3] **Build lane**. After T006: Update `README.md`, `docker/api.settings.example.json`, and `tests/LamuFlix.UnitTests/SubtitleSetupDocumentationTests.cs` for SubDL settings (including required caps at owner-approved example values), effective-disabled and keyed-invalid behaviour, and approved credential channels; keep example `ApiKey` empty; no committed key value (FR-011; SC-004; Wanted 7; Step 3.3; `recon-DEV-554:91–93`). T012 may proceed after T006 without waiting for the Phase 3 checkpoint; T011 may not.

**Checkpoint**: Green T011–T012 unlock refactor/gates/review.

---

## Phase 5: Polish — refactor, delivery-diff gates, Size M review

**Purpose**: Meet size-M backend quality bar under the brief closing bar on the delivery diff.
**Checkpoint**: Actual gate verdicts recorded for the delivery diff; review dispositions under Q7; no undeserved PASS. (Rebased-head ship gates are T017, not T014.)

- [ ] T013 **Cog**. After T010–T012: Run refactor gate `./scripts/run-cyclomatic-complexity.ps1 -Threshold` from `harness.yml` refactor setting on changed production C#; extract helpers as needed; no threshold edits.
- [ ] T014 **Gauge**. After T013: Run size-M backend matrix on the **initial delivery diff** — Roslyn, cyclomatic complexity, InspectCode, property tests, vulnerable packages, format, `dotnet test`; mutation for production C# scope at harness threshold with **actual** verdict preserved; on mutation share/span failure, **correct deficient tests** while retaining configured thresholds; web gates `SKIPPED (scope-empty)` if no web files (`brief.md` Gates; Q6; SC-006; Step 6 fix 7). T014 does **not** satisfy the rebased-head ship checkpoint.
- [ ] T015 **Size M review**. After T014: Risk (Sentry), Standards (Ledger), Spec (Compass) against the closing bar — block on Critical/High with concrete scenario or any demonstrated acceptance/credential/archive safety violation; disposition all other findings; max two rounds / two fix commits per round (Q7).

**Checkpoint**: Review closed under bar; ready for T017 ship checkpoint on rebased head, then delivery PR + T016 receipt.

---

## Phase 6: User Story 4 - Ship checkpoint then home-cluster receipt (Priority: P2)

**Goal**: Gauge finishes the size-M matrix on the rebased delivery head **before opening the delivery PR**; then T016 attaches the scrubbed live search + one download receipt on that delivery PR.
**Independent Test**: SC-006 ship half / SC-005 / Done when bullet 4; `task-pipeline` Phase 5 steps 1–2.

- [ ] T017 **Ship checkpoint (Gauge) — before opening delivery PR**. After T015 closed and after Rigger rebases/pushes the delivery branch (`task-pipeline` Phase 5 step 1): **Gauge** re-runs the size-M backend matrix on the **rebased delivery head** with **actual** verdicts — Roslyn, cyclomatic complexity, InspectCode, property tests, vulnerable packages, format, `dotnet test`; mutation actual verdict (on share/span failure, correct deficient tests while retaining configured thresholds); web `SKIPPED (scope-empty)` if applicable (`brief.md:33,40`; `task-pipeline` Phase 5 step 2; SC-006; Step 6 fix 7). Record verdicts for the rebased head. **T017 MUST finish before the delivery PR is opened.** T014's initial delivery-diff run does not substitute. T017 does **not** wait on T016.
- [ ] T016 [US4] **Rigger / operator evidence — after T017, on delivery PR**. After **T017 finishes** and after the delivery PR is opened: Attach a real home-cluster SubDL movie search and one successful download to the delivery PR — record movie, languages, candidate count, and success; scrub credentials. Keep the live receipt attached to the delivery PR. CI remains WireMock (FR-012; SC-005; Q6; Done when bullet 4; `brief.md:33,40`).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (T000)**: Blocks all Build work; requires owner checkbox tick + VII amendment evidence (options entry + replaced 409 clause + checklist + DEV-501/ADR 0041 Sync Impact Report) or rejection path stop.
- **Phase 1 (T001)**: Blocks all production edits; read-only; verified documented year mapping, exact 409/health/registration path+line inventory (out-of-envelope 409 files Patron-approved in `CONCLUSIONS.md` before T008), cited Principle VI health seam, client-redirect and test-only HTTPS records, provider-identity runtime-data / VIII check, and Q2 attribution+reset pickup evidence required to unlock T003+; Q5 stop leaves transport locked; missing/ambiguous attribution locks mapping to `Unknown`; out-of-envelope paths (including new TelemetryConstants names) need a cited Patron ruling in `CONCLUSIONS.md` first.
- **Phase 2 (US1)**: After T001 with year-mapping pass; **T003 then T002 then T004** (not parallel); blocks adapter download path that needs the reader.
- **Phase 3 (US2)**: After T004 (+ T000 for options); T008 = explicit 409 implementation + Principle VI health registration after wiring (Patron-approved out-of-envelope 409 paths only); T009 = **separate** composition evidence + contract suite; Phase 3 checkpoint is T005–T010 green.
- **Phase 4 (US3)**: T011 only after **Phase 3 checkpoint** (T005–T010) + T007 (same file as T005; not `[P]`; no overlap with T008–T010; asserts no credentials on all followed hops); T012 after T006 (may proceed without waiting for Phase 3 checkpoint).
- **Phase 5**: After T010–T012; T014 = delivery-diff gates only (correct tests on mutation share/span failure).
- **Phase 6**: After T015; **T017 first** (ship checkpoint on rebased head **must finish before opening** the delivery PR; same mutation share/span rule); **then T016** (live receipt on that delivery PR).

### User Story Dependencies

- **US1**: No dependency on SubDL HTTP; foundation for US2 download; unit-project archive + FsCheck; T003 precedes T002.
- **US2**: Depends on US1 reader for ZIP path; depends on DEV-551 from T000/T001; depends on verified year mapping; 409 implementation (T008) separate from composition evidence (T009).
- **US3**: Depends on adapter diagnostics paths from US2; docs depend on options shape; sentinel (T011) shares T005's file only after Phase 3 checkpoint.
- **US4**: Ship checkpoint (T017) must finish before opening the delivery PR; live receipt (T016) after T017 on that PR; not a CI blocker substitute for WireMock suite.

### Parallel Opportunities

- T003 and T002 are **not** parallel — T003 establishes the reader call shape before T002.
- T005 fixtures/tests authoring alongside T006 once T001 fixture location is known (T005 must not write fixtures before location clearance); different files → `[P]` OK.
- T012 after T006 may run while Phase 3 finishes; T011 must **not** start until Phase 3 checkpoint (T011 is not `[P]` and must not overlap T008–T010).
- US4: T017 finishes before opening the delivery PR; T016 after T017 — never parallel with unfinished review bar (T015).

---

## Ordered Example: User Story 1

```text
Task: "ZipSubtitleArchiveReader in src/LamuFlix.Infrastructure/Adapters/ZipSubtitleArchiveReader.cs" (T003 first)
Task: "ZipSubtitleArchiveReaderTests in tests/LamuFlix.UnitTests/ZipSubtitleArchiveReaderTests.cs" (T002 after T003)
Task: "T004 verification checkpoint"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 0–1 (Gate 1 / T000 evidence + pickup with verified year mapping)
2. Complete Phase 2 in order (T003 reader → T002 unit+FsCheck → T004 verify)
3. **STOP and VALIDATE** archive safety independently
4. Continue to SubDL adapter

### Incremental Delivery

1. Gate 1 + T000 evidence + pickup clearance (verified year mapping, exact 409/health paths, Q2 attribution+reset evidence, test-only HTTPS)
2. Archive reader (T003→T002→T004) → SubDL adapter + explicit 409 implementation + health registration → separate composition evidence + contract suite → credential/docs (T011 after Phase 3) → delivery-diff gates/review → rebased-head ship checkpoint (T017 finishes before opening delivery PR) → home-cluster receipt (T016)
3. Each story adds value without expanding frozen scope

---

## Notes

- `[P]` tasks = different files, no incomplete deps (T002/T003 deliberately not `[P]`)
- `[Story]` label maps to US1–US4 in `spec.md`
- Verify tests fail before implementing when using TDD
- Commit after each local phase or logical group on the Build branch (`DEV-554 - <phase title>`)
- Stop at any checkpoint to validate independently
- Avoid: inventing year mapping; treating Q5 stop as T003+ pass; inventing Q2 attribution/reset representation; mapping quota without T001 attribution evidence; conflating T008 409/health with T009 composition; skipping SubDL health-check registration; inventing telemetry name literals or vendor-named Core constants outside constitution rules; editing out-of-envelope paths without a cited Patron ruling in `CONCLUSIONS.md`; starting T011 before Phase 3 checkpoint; opening delivery PR before T017 finishes; auto-following redirects; parallel fake frameworks; `/me` calls; new packages/layers; public API drift; committed keys; vendor-bearing Core names; reader-side cap defaults or mutable static state; lowering mutation thresholds on share/span failure; claiming VII amendment already approved
