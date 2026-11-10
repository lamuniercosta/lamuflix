# Tasks: Subtitle provider and BS Player-compatible sidecar

**Input**: `specs/DEV-453/` spec.md, plan.md, grill-closed brief.md (Q1–Q12), CONCLUSIONS.md, ASSUMPTIONS.md, ADR 0038, recon-DEV-453, note DEV-453, specs/PRODUCT.md

**Status**: Draft — Plan Challenge round 1 planning repairs against brief Authoritative Plan Challenge adjudication addendum (2026-11-10). Code tasks unchecked. **blocked: structural (C4/VII)**. No PLAN FROZEN / Gate 1 clearance / analyze-clean / runtime PASS claim. Gate 1 stays closed until the owner checkbox in `spec.md` is answered.

**Gate 1**: Unresolved owner structural checkbox (brief addendum C4/VII; exact verbatim question in `spec.md` Owner question — Gate 1). Spec-PR body handoff MUST carry the same unchecked question. Do not check it or amend constitution on assumed approval. Affected SubtitlesOptions / OpenSubtitlesOptions ValidateOnStart implementation stays blocked until owner approval + scoped constitution/governance amendment that also appends the new subtitle options record(s) to Principle VII's closed options-record list. Implementation C# types remain functionality-named (`SubtitlesOptions` / `SubtitleProviderOptions`) with vendor config path `Subtitles:OpenSubtitles:*` retained. Preserve Q10 / no numeric defaults. ADR **0038** already exists (Proposed; accepted historical grill-closure context — no Quill ADR rewrite). Principle I constitution enumeration + feature-folder/isolation compliance + ArchitectureTests whitelist for `ISubtitleProvider` and `ISubtitleSidecarStore` remain independent residual compliance work (Keel/Build), not blocked by C4/VII.

**Tests**: Required (brief Q9; FR-016). Prefer new-behaviour regressions red before production edits where feasible, only after referenced compile surfaces exist. Real filesystem containment/atomic-write proofs never mocked. Provider HTTP via WireMock.NET. No real credentials in fixtures. No new packages. Parallel only on disjoint files after prerequisites; same-file work is never parallel.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete. Same-file edits are never `[P]`. Shared gate scripts are never `[P]`.

**Conventions**: Existing xUnit v3 / Shouldly / NSubstitute / FsCheck / WireMock.NET only; never hand-edit generated OpenAPI/TS; never lower harness thresholds; ApiKey secret-injected only; no path/secret/content logging; no LocalPlay/Process.Start changes; exact DI/mapping filenames from Phase B Wisp — do not guess; no explanatory comments outside allowed exemptions.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement Phase N` dispatch. They are **not** canonical `task-pipeline` phases.

---

## Local phase dependency / execution order

```text
Phase 0  T000 (Keel Gate 1 record) — blocks T001+
Phase 1  T001 (Wisp pickup facts) → T001b (Keel analyze / task-drift)
         → T001c (Keel verify ADR 0038 + constitution/governance for two ports)
         — blocks T002+
Phase 2  T002 → T004   (ports/DTO/options + atomic whitelist)
Phase 3  T005 → T008   (US2 foundation: hash/selection + provider adapter; content/redirect/no-retry)
Phase 4  T009 → T012b  (US1/US2: sidecar store + handlers/endpoints + bounded observability)
Phase 5  T013 → T015   (US3: OpenAPI/TS contract chain)
Phase 6  T016 → T017   (US4: demo/writable docs)
Phase 7  T018 → T021   (verification gates — sequential)
Phase 8  T022          (US4: runtime receipts)
Phase 9  T023 → T024   (Size L review + delivery)
```

---

## Phase 0: Gate 1 record (blocks Phase 1)

**Purpose**: Confirm user Gate 1 merge before any Build work.
**Checkpoint**: Verified merge receipt in CONCLUSIONS.md; or stop.

- [ ] T000 **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-453 in `specs/DEV-453/CONCLUSIONS.md` (append-only; single writer; preserves prior Patron rulings) only after the owner structural C4/VII checkbox on the spec PR is answered and Gate 1 is otherwise approved. Receipt only — does not create a new ruling and does not itself approve the constitution departure. T001–T024 MUST NOT proceed until this receipt exists (Q9; brief addendum C4/VII; FR-017).

---

## Phase 1: Pickup — Wisp facts, Keel analyze, Principle I compliance (blocks feature implementation)

**Purpose**: Confirm delivered DEV-457/DEV-454 contracts; identify exact DI/endpoint mapping filenames; verify hash vectors, provider schema, download-host allowlist; complete Principle I prerequisites for the two brief-approved ports.
**Checkpoint**: Fresh recon recorded; dependency delivery evidenced; Keel analysis clean or findings disposed; ADR 0038 verified present; constitution/governance for `ISubtitleProvider` + `ISubtitleSidecarStore` landed or exact-equivalent evidence verified; whitelist **plan** confirmed. Unresolved drift/decision stops — no invention.

- [ ] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main for: delivered DEV-457 and DEV-454 contracts (plans alone do not prove delivery); existing API DI registration and endpoint-mapping entrypoint filenames (and any MapGroup prefix) for subtitle registration — Phase A docs deliberately do **not** assert `ApiEndpoints.cs` or `/api` grouping; current OpenSubtitles headers/error schema; **exact** OpenSubtitles API host and download host(s) plus protocol compatibility (HTTPS; whether live protocol requires redirects — if yes or hosts unknown, **STOP** before T008); safe download-host allowlist candidates (no invented host strings); authoritative moviehash reference vectors; exact `ILibraryFileReader` read/stream/seek contract for first/last 65,536-byte windows (**STOP** before T002/T006 if head/tail windows unsupported without contract drift); authorized Movie primary-title property name (**STOP** before T005/T006 if unconfirmed — no guessed property); whether `/download` returns raw `.srt` bytes or a compressed wrapper (**STOP** before T007/T008 on mismatch; no invented decompression dependency); Principle I feature-folder enumeration in constitution and ArchitectureTests namespace-isolation rules; ArchitectureTests `PortWhitelistPattern` current entries/count for append-only plan; Compose/README library mount patterns for read-only vs writable docs. **Wisp owns pickup facts only** — do not run analyze, do not amend tasks, do not invent filenames or hosts. **STOP** on missing dependency delivery or missing facts. No production feature edits in this task (Q9–Q10; FR-017; Plan Challenge items 1, 5–6, 9–10).
- [ ] T001b After T001: **Keel** runs pickup cross-artifact reanalysis of brief/spec/plan/tasks against Wisp facts and performs authorized task-drift adjudication only. Amend Quill-owned task text only where authorized by demonstrated drift. **Feature implementation (T002+) MUST NOT start until this analysis completes clean or with findings disposed.** Unresolved drift stops for a cited decision — no invention (FR-017).
- [ ] T001c After T001b, **before T002**: **Keel-owned verify/governance pickup** — verify existing `docs/adr/0038-subtitle-provider-and-sidecar-boundary.md` is present (**do not reauthor**; remains Proposed until acceptance lifecycle; accepted historical grill-closure context); edit `.specify/memory/constitution.md` to add only `ISubtitleProvider` and `ISubtitleSidecarStore` to the named-port enumeration with required governance record **and** any required feature-folder enumeration extension / isolation compliance (appropriate MINOR; Sync Impact Report; Last Amended; template review; migration/follow-up), or verify exact-equivalent evidence already landed; confirm ArchitectureTests whitelist **plan** for append-only insertion of both ports (exact position/count from T001) and namespace-isolation rules for the new feature folder. Handlers MUST resolve movie/video/library only via shared `Core/Ports` and `Core/Domain`. **Do not** backfill historically omitted ports. Whitelist **code** lands atomically with T002 only. **STOP** if ADR missing, feature-folder/isolation unconfirmed, or governance/whitelist plan unconfirmed — before creating a new Core feature folder or ports. Do not mark premature Principle I implementation PASS (Q7; ADR 0038; constitution Principle I; Plan Challenge item 5.2). **Principle I remains independent of C4/VII.** **T001c does not resolve** C4/VII — ValidateOnStart vs Q10 remains the unresolved owner structural checkbox (verbatim; unchecked); if/when the owner approves, the corresponding scoped Principle VII constitution/governance amendment must complete through its normal lifecycle **and** append the new subtitle options record(s) to Principle VII's closed options-record list before affected ValidateOnStart-departure implementation. C4/VIII is already satisfied by the Phase A append-only root `CONTEXT.md` edit in this same spec PR (do not defer).

---

## Phase 2: Ports, options, and DTOs (Foundational — blocks user stories)

**Purpose**: Compile-ready Core ports + options/DTO surfaces; ArchitectureTests whitelist append; demo-safe configuration validation.
**Independent Test**: Solution builds with ports whitelisted; missing acquisition settings do not break demo startup; options bind under `Subtitles` / `Subtitles:OpenSubtitles`.
**Order**: T002 → T003 → T004.

- [ ] T002 After T001c: as **one atomic buildable type slice**, add `src/LamuFlix.Core/Ports/ISubtitleProvider.cs` and `src/LamuFlix.Core/Ports/ISubtitleSidecarStore.cs` plus minimal Core subtitle models required by those ports (follow existing Core conventions; no new project/layer), with `ISubtitleProvider` accepting computed moviehash (`string?`), file size (`long`), title (`string?`), and ordered language preferences as data only — never a local path or stream; **and** append only `|ISubtitleProvider|ISubtitleSidecarStore` (exact form confirmed by T001) to `PortWhitelistPattern` in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` immediately before `)$`, preserving all existing entries and order — no other whitelist body/floor changes. **Blocked on T001b/T001c.** Do not claim Principle I PASS prematurely (Q7; FR-010; Plan Challenge item 5.1).
- [ ] T003 [P] After T002 compile surfaces exist: add functionality-named Subtitles options + prepare response DTO under existing Api/Core options conventions — C# types `SubtitlesOptions` / nested `SubtitleProviderOptions` (never a vendor-named options class) binding vendor-mandated config path `Subtitles:OpenSubtitles:*` (`Subtitles:SaveEnabled` default false; `Subtitles:PreferredLanguages` empty default; `Subtitles:OpenSubtitles:ApiKey` secret; `BaseUrl`; `UserAgent`; nested `DownloadAllowedHosts`, `MaxDownloadBytes`, `RequestTimeout` with positive validated bounds — no guessed defaults) in the established options/DTO locations beside existing endpoint options patterns; DTO `selectedLanguage` remains `string?` across outcomes and is always the matched preferred code on `saved`; ensure missing/invalid acquisition settings (including invalid local BaseUrl) do not prevent API demo startup and yield prepare-time 409 (Q6, Q10; FR-005; FR-009; Plan Challenge items 3–4, 6.1). **Affected ValidateOnStart departure path remains BLOCKED** until the owner C4/VII checkbox is approved and the scoped constitution/governance amendment lands (including options-record enumeration append) — do not implement assumed approval; preserve Q10 / no numeric defaults; owner checkbox wording stays verbatim/unchecked (brief addendum C4/VII; Plan Challenge items 3–4; FR-009).
- [ ] T004 After T003: register options binding and DI stubs only in the **exact** existing DI registration entrypoint identified by T001 (no guessed filename rewrites); keep SaveEnabled false / empty preferences as safe demo defaults; do not register production provider/store adapters until later phases (Q10; FR-009). Do not wire ValidateOnStart for acquisition options under assumed C4/VII approval — that path stays blocked pending owner checkbox + scoped constitution amendment (including options-record enumeration).

---

## Phase 3: Hash, selection, and OpenSubtitles provider (Priority: P1 — User Story 2 foundation)

**Goal**: Deterministic moviehash + language selection and WireMock-backed OpenSubtitles adapter.
**Independent Test**: Unit/property hash/selection suites green; provider adapter green against WireMock.NET without real credentials.

- [ ] T005 [US2] After T002 and confirmed T001 Movie primary-title + hash-vector facts: add failing unit/property coverage under `tests/LamuFlix.UnitTests/` (exact new test file at Build) for moviehash reference vectors and boundaries (eligible ≥131,072 bytes; ineligible small-file path), preference order, within-language tie-break (moviehash match → highest download_count → lowest file_id), title-fallback skip when primary title is null/empty/whitespace → noMatch, and noMatch when no preferred language is eligible — no invented vectors/property names; use T001 authoritative vectors and confirmed title field (Q4, Q8–Q9; FR-002; FR-013; FR-016; Plan Challenge item 6.2).
- [ ] T006 [US2] Implement hash + deterministic selection logic in the Infrastructure/Core locations established by T002/T005 (existing project folders only) to make T005 green; compute moviehash through authorized read-only `ILibraryFileReader` head/tail windows (no path/stream into provider); title fallback only when hash is ineligible or yields no preferred-language match, using the T001-confirmed Movie primary-title property; blank title skips the title request and yields noMatch without overriding a successful hash match; never select outside preferences (Q8; FR-002; FR-010; FR-013; Plan Challenge items 5.1, 6.2). **Blocked on T001 reader/title STOP facts.**
- [ ] T007 [US2] After T005/T006 and confirmed T001 raw/wrapper + host/protocol facts: add failing WireMock.NET provider-adapter coverage under `tests/LamuFlix.UnitTests/` or `tests/LamuFlix.IntegrationTests/` (exact file at Build) for Api-Key + User-Agent headers, moviehash-first then title search (data-only inputs), download URL handling without caching temporary URLs, **pre-request HTTPS + verified host checks for both search BaseUrl and download URLs**, **`AllowAutoRedirect = false` with all `3xx` rejected as 502 and proof rejected destinations are never contacted**, 429 + Retry-After propagation for valid nonnegative delta-seconds **and** HTTP-date (omit absent/malformed; no invented cap), **provider timeout→504 mapped failure vs caller-abort never→504**, auth/error mapping, rejection of non-allowlisted/unsafe URLs, **rejection of downloaded content failing minimal SRT criteria** (non-empty; `<= MaxDownloadBytes`; valid UTF-8 with optional BOM; blocked prefixes `<`/`{`/`[`/`PK`/gzip `1F 8B`; require at least one `HH:MM:SS,mmm --> HH:MM:SS,mmm` or `.` millisecond cue line; bounds from configured options only — no numeric defaults invented), and **proof that automatic download retries do not occur** — **no real credentials** (Q3, Q8–Q9; FR-001; FR-007; FR-012; FR-016; Plan Challenge items 1, 3, 10–11). **STOP if T001 reports redirect-required protocol or raw/wrapper mismatch.**
- [ ] T008 [US2] Implement OpenSubtitles HTTP adapter fulfilling data-only `ISubtitleProvider` under `src/LamuFlix.Infrastructure/` (exact subfolder at Build within existing layout); never open local filesystem paths inside the adapter; enforce HTTPS + verified API/download hosts from T001 before every request; disable automatic redirects and reject all `3xx` as 502; configured MaxDownloadBytes/RequestTimeout only (no invented numeric defaults / hop caps); reject non-SRT/invalid/oversized/blocked-prefix/missing-timestamp content; no automatic download retries (prove via T007); never persist temporary URLs; never log ApiKey/paths/content; make T007 green; register adapter in the T001 DI entrypoint (Q3, Q7–Q8; FR-001; FR-010; FR-012; Plan Challenge items 1, 5.1, 10). **Blocked on T001 host/protocol/raw-wrapper STOP facts.**

---

## Phase 4: Sidecar store, handlers, prepare/serve API, observability (Priority: P1 — User Stories 1 & 2)

**Goal**: Contained exact-basename read/atomic write; sealed Core command/query handlers + thin TypedResults endpoints; bounded Principle VI telemetry/health work.
**Independent Test**: Real temp-FS store matrix; endpoint/host matrix for GET serve (independent of acquisition settings) and POST prepare outcomes/errors; content/redirect/no-retry coverage; no provider calls on alreadyPresent/GET.

- [ ] T009 [US1] After T002: add failing real temp-filesystem coverage under `tests/LamuFlix.UnitTests/` (exact file at Build) for contained exact-basename read; missing sidecar; traversal/reparse/nonregular denial; atomic create-without-replace; no overwrite of existing sidecar; concurrent create → alreadyPresent after recheck; ordinary cancellation cleanup with no partial file — never mock filesystem authorization. Record accepted personal-scale risks in test/plan notes only: concurrent prepares may download twice before atomic-create winner; abrupt process death may orphan temp — **no** locking or janitor coverage added (Q8; FR-003; FR-011; FR-016; Plan Challenge item 2.3 / Sentry F6).
- [ ] T010 [US1] Implement `ISubtitleSidecarStore` filesystem adapter under `src/LamuFlix.Infrastructure/FileSystem/` (exact filename at Build); authorize primary video folder/file before read/write; server-derived `Film.srt` path only; same-directory temp + atomic create-without-replace; **bounded existing-track read/serve path that does not depend on acquisition settings** (SaveEnabled/ApiKey/allowlist/bounds unused for read; exact bytes without downloaded-SRT validity gate); make T009 green; register in the T001 DI entrypoint; keep `ILibraryFileReader` read-only and available for authorized hash head/tail reads without write widening (Q7–Q8, Q10; FR-008; FR-010; FR-011; Plan Challenge O3).
- [ ] T011 [US1] [US2] After T008/T010: add failing endpoint/host coverage under `tests/LamuFlix.IntegrationTests/` (exact file at Build) for: GET 200 `application/x-subrip` exact bytes with no provider traffic and headers `X-Content-Type-Options: nosniff` + `Content-Disposition: attachment` with safe server-derived basename (no path disclosure); **GET success and 404/403 maps proven with acquisition settings missing/invalid/disabled (independent of SaveEnabled/ApiKey)**; prepare 200 `alreadyPresent` before acquisition checks (SaveEnabled false / no Api-Key / read-only) with null `selectedLanguage`; prepare 200 `saved` with always-matched non-null `selectedLanguage`; prepare 200 `noMatch` with null `selectedLanguage`; raw-route-segment malformed-id **400** ProblemDetails+traceId (true non-integer, not `{id:int}` dead branch); well-formed absent movie **404**; validly bound FluentValidation failure **422** on real reachable validators only (never remapped to 400; do not invent unreachable GET 422; do not claim all validation is 400); 403 containment; 404 missing movie/video; 409 disabled/unconfigured/unwritable/write-permission/invalid local BaseUrl; 429 with Retry-After delta-seconds and HTTP-date forms; 502 content/unsafe-URL/all-`3xx` redirect failures with never-contacted proof; 504 **provider** timeout only; caller-abort never maps to 504 and no rollback promise after committed atomic save; oversized/invalid downloaded-SRT rejection path; no-retry proof at API boundary where applicable; no stream-path provider traffic; LocalPlay independence / no Process.Start; **bounded existing-track serving assertions that do not require acquisition configuration** (Q5–Q8, Q10; brief addendum C1/V; FR-004–FR-008; FR-012; FR-014; FR-016; Plan Challenge items 2–3, 8, 11).
- [ ] T012 [US1] [US2] Implement thin TypedResults `src/LamuFlix.Api/Endpoints/SubtitleEndpoints.cs` for brief-relative `POST /movies/{id}/subtitles/prepare` and `GET /movies/{id}/subtitles` capturing raw `{id}` (or equivalent reachable binding/fallback) with explicit parse so malformed non-integer → **400** and well-formed absent → **404** (exact MapGroup/registration entrypoint filename from T001 — **do not assert `ApiEndpoints.cs` or `/api` grouping as Phase A facts**); add `sealed record` prepare command + serve query, `sealed` Core handlers, FluentValidation validators, and `AddHandler` registration with Tracing → Logging → Validation → handler decorators (Principle II — no endpoint-local orchestration substitute; no new MediatR/repository/result layer); handler flow authorize → existing-sidecar check → acquisition validation → data-only provider → atomic save; all declared failures as typed Core exceptions/categories through the existing single ProblemDetails+traceId mapper (no endpoint-local status mapping); GET bounded existing-track serve independent of acquisition settings with nosniff + safe-basename attachment headers; distinguish caller abort from provider timeout; record accepted concurrent double-download / crash-orphan risks without adding lock/janitor; make T011 green (Q5–Q7, Q10; FR-004–FR-008; FR-007; Plan Challenge items 2–5).
- [ ] T012b [P] After T003/T012 compile surfaces exist (or with T012 if same registration file — then not `[P]`): implement resolved C2/VI Principle VI work — add **functionality-named** TelemetryConstants names for subtitle prepare/serve/provider spans/metrics/attributes (never vendor-named telemetry constants); emit structured events carrying movie id; register wiring only through ServiceDefaults/Infrastructure-owned patterns (hosts MUST NOT duplicate). Register ONE always-Healthy optional-provider local health check with sanitized metadata `ready` or `disabled_or_unconfigured` (including enabled-but-invalid/missing acquisition config), zero network/quota calls, full enabled/disabled/configured/invalid test matrix, and **explicit exclusion from `/health/live`**; do not assert upstream reachability or change other dependency health results (brief addendum C2/VI; Plan Challenge item 7; Principle VI:478–490; checklist:623–624; FR-016; FR-021).

---

## Phase 5: Generated OpenAPI contract chain (Priority: P2 — User Story 3)

**Goal**: Tooling-regenerated OpenAPI + TypeScript for prepare/serve contracts.
**Independent Test**: Drift/inventory checks green; no hand-edited generated files.

- [ ] T013 [P] [US3] Update `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` ExpectedOperations (and related expectations forced by regeneration) for prepare/serve operations, approved statuses/DTO shape, and application-validation **422** metadata alongside transport malformed-id **400** (do not claim all validation is 400) (Q6; brief addendum C1/V; FR-007; FR-015).
- [ ] T014 [P] [US3] Update endpoint-name inventory tests under `tests/LamuFlix.IntegrationTests/` (existing inventory file(s) identified at Build, e.g. LibraryEndpointsTests patterns) to include subtitle endpoints (Q6; FR-015).
- [ ] T015 [US3] After T012–T014: regenerate contracts via supported workflow — set `LAMUFLIX_REGENERATE_OPENAPI=true`, run OpenAPI regeneration test (writes `web/src/api/openapi.json` + Verify snapshot together), then from `web/` run `npm run generate:api` producing `web/src/api/types.ts`; verify with `npm run check:api`. **Never hand-edit** generated artifacts (Q6; FR-015; SC-005).

---

## Phase 6: Demo / writable setup documentation (Priority: P1 — User Story 4 docs)

**Goal**: Document read-only demo serving vs deliberately writable daily-driver acquisition without committing secrets or machine-specific paths.
**Independent Test**: Doc review against Q5/Q10; safe demo defaults retained.

- [ ] T016 [US4] Update configuration/Compose documentation surfaces named by T001 recon (existing README/Compose example settings only — no new top-level docs project) to describe read-only demo existing-track serving without acquisition calls/writes (Q5, Q10; FR-009; SC-004).
- [ ] T017 [US4] In the same authorized doc/config surfaces: document writable daily-driver enablement (`Subtitles:SaveEnabled`, PreferredLanguages, secret-injected ApiKey, BaseUrl/UserAgent/allowlist/bounds, writable library mount) without committing credentials or machine-specific paths; retain safe demo defaults (Q5, Q10; FR-009; SC-004).

---

## Phase 7: Verification gates (sequential)

**Purpose**: Applicable harness gates with honest exits; frozen scope only.
**Independent Test**: Gate scripts report actual PASS/FAIL/SKIPPED/SKIP/N/A/Could not run — never relabelled.

- [ ] T018 Confirm frozen scope only: Q3–Q10 provider/preferences/exact-basename prepare/serve, ports/adapters/endpoints, generated `openapi.json`/`types.ts`, config/docs, focused tests, Principle I surfaces limited to ADR 0038 + constitution enumeration for the two ports + whitelist append. No UI workflow, LocalPlay/Process.Start changes, packages/schema/new layers, `ILibraryFileReader` write widening, or unrelated rewrites (Q7, Q11; FR-020).
- [ ] T019 **Gauge** runs `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (then refactor threshold from harness.yml), `./scripts/run-jetbrains-inspectcode.ps1` on changed `.cs`; `dotnet format --verify-no-changes` (FR-018; Q9). Sequential — not parallel.
- [ ] T020 **Gauge** runs configured `dotnet test` / property / vulnerable-packages per harness; report actual applicability; SKIPPED/SKIP never PASS (FR-016; FR-018; Q9). After T019.
- [ ] T021 **Gauge** runs configured mutation (`pwsh -NoProfile -File ./scripts/run-mutation.ps1`) and OpenAPI/TS drift (`npm run check:api` / applicable web gates). Report actual exits only including mutation N/A / Could not run — never manufacture PASS (FR-018; Q9; SC-007). After T020.

---

## Phase 8: Runtime receipts (Priority: P1 — User Story 4)

**Goal**: Separate live evidence for provider download, writable save, BS Player autodetection, and read-only serving.
**Independent Test**: Scrubbed `specs/DEV-453/runtime-acceptance.md` fields, or `Could not run` with reason — never silent PASS.

- [ ] T022 [US4] On the delivery SHA, collect scrubbed runtime receipts in `specs/DEV-453/runtime-acceptance.md` for: (1) configured-key OpenSubtitles search/download, (2) successful writable exact-basename sidecar save, (3) supported BS Player autodetection of exact-basename SRT, (4) read-only demo serving existing sidecar without acquisition. Redact secrets/paths. If prerequisites unavailable, record `Could not run` with reason and keep acceptance open — never PASS by automation alone (Q9; FR-019; SC-006).

---

## Phase 9: Size L review and delivery

- [ ] T023 **Sentry / Ledger / Compass** three-axis review under brief Q11 closing bar (High/Medium and any security/contract/ticket failure block; every Low explicitly dispositioned; frozen Q3–Q10 scope; two rounds max; unresolved blockers → Bernstein). Publish findings and summary on the PR pinned to the exact reviewed head (FR-020; Q11; SC-007).
- [ ] T024 Exact-head delivery workflow under Conductor (rebase/ship/PR as routed); user merges; no autonomous merge. Ticket acceptance/Done requires T022 runtime evidence success (or remains open on `Could not run`) (Q9, Q11; FR-019).

---

## Dependencies & Execution Order

- Phase 0 blocks all Build.
- Phase 1 blocks production feature edits: Gate 1 + delivered DEV-457/DEV-454 + Wisp facts + Keel analyze + ADR/constitution/whitelist plan before T002+.
- Phase 2 ports/options before provider/store/API.
- Phase 3 provider before prepare integration that downloads.
- Phase 4 store before handlers/API green; US1 GET can be proven once store exists even before provider, but T011/T012 cover both stories together after T008/T010; T012b observability may trail T012 when registration files differ.
- Phase 5 after endpoint shape exists; T013/T014 may proceed in parallel after T012 metadata exists; T015 after both.
- Phase 6 docs may trail or follow Phase 4–5; T016–T017 share surfaces — sequential if same file.
- Phase 7 sequential gates after implementation.
- Phase 8 runtime after gates-ready delivery SHA.
- Phase 9 review/delivery last.

### Parallel opportunities

- T003 may proceed once T002 compile surfaces exist (disjoint from whitelist file if options live elsewhere).
- T013 and T014 inventory edits may proceed in parallel after endpoint shape exists (different files).
- Never mark `[P]` across shared files (`SubtitleEndpoints.cs`, T001 DI/mapping entrypoints, `ArchitectureTests.cs`, provider/store/handler surfaces, ServiceDefaults wiring) or incomplete prerequisites.
- T019–T021 are never parallel.

### MVP

US1 existing-track GET serve + US2 prepare with alreadyPresent/saved/noMatch against WireMock + real temp FS is the minimum operable backend increment; US3 contracts and US4 docs/runtime receipts remain mandatory for ticket acceptance (runtime may stay open as `Could not run`).

## Notes

- `[P]` tasks = different files, no incomplete deps, no shared build outputs.
- Local Phase N ≠ canonical task-pipeline phase numbers.
- Quill does not invent exact DI/mapping filenames, `/api` grouping facts, allowlist host strings, hash vectors, Movie primary-title property, reader head/tail contract, or download raw/wrapper shape in Phase A; T001 resolves them with STOP gates.
- C1/V and C2/VI are resolved (brief addendum items 1–2); C4/VIII satisfied by Phase A append-only root `CONTEXT.md` in this same spec PR. Plan Challenge round 1 repairs applied from Keel numbered list 1–11 / brief Authoritative Plan Challenge adjudication addendum. **C4/VII remains unresolved owner structural checkbox** — see DRAFTING_RECEIPT and `spec.md` Owner question; Gate 1 stays closed; no PLAN FROZEN / analyze-clean claim.
- Exact new test/adapter filenames under existing folders are chosen at Build without expanding frozen scope (Q7, Q10).
- Keel owns analyze / plan challenge / constitution governance; Quill does not execute T000–T024 in Phase A, does not mark code tasks complete, and does not start another challenge round (two-round cap).
- Does **not** claim Gate 1, PLAN FROZEN, analyze-clean, runtime acceptance, or merge clearance.
