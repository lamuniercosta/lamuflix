# DEV-453 — Subtitle provider and sidecar planning brief

## Status and authority

Phase 2 grill closed with Patron at Q12/12 on 2026-11-09. This is the authoritative planning handoff for Quill, not spec approval, PLAN FROZEN, Gate 1, implementation pickup, runtime acceptance or merge clearance. No structural blocker or constitution departure remains from the grill. Quill drafts spec.md, plan.md and tasks.md; Keel subsequently analyzes them read-only and conducts plan challenge.

Worktree: `F:/Dev/LamuFlix.worktrees/feature-453-spec`; branch: `feature/453-spec`; recorded base: `2ced085e21f2916c035d13de0d281716d823ed2f`. ADR 0038 was assigned by Bernstein and recorded in task/chain notes.

Authority: task note DEV-453 scope; unified recon-DEV-453 lines 1–21; Patron Q1–Q12, reproduced in full in CONCLUSIONS.md. Patron cites specs/PRODUCT.md §§3/5, the charter and ADR 0037 in the rulings. Those citations are recorded rulings, not independent recon by Keel. Earlier recon revisions were replaced; historical line references in the transcript must not be mistaken for current line numbers.

## Frozen scope and loop discipline

Ship one verified subtitle provider, ordered language preferences, BS Player-compatible sidecar names, writable daily-driver saving and existing-track serving from the read-only demo library. Evaluate BS Player hash service first. Deliver backend preparation/serving, generated API contracts, configuration, setup documentation and tests under Q3–Q10; anything else is a follow-up issue, not a finding in this round.

High and Medium findings block. Security, public-contract or ticket failures block regardless of label. Every Low needs an explicit cited disposition: fix, accepted nonblocking risk, or follow-up. Size L requires all three reports from Sentry, Ledger and Compass; findings and summary must be published on the PR and pinned to the exact reviewed head. Two review/remediation rounds maximum; unresolved blockers then go to Bernstein for a deliberate next-step decision, never automatic clearance. Grill cap was 12 questions and was reached. Patron approved these terms in Q11.

## Vocabulary

- **Ordered language preferences:** explicit server-configured provider-language codes in selection priority order; no invented default language.
- **Selected subtitle:** compatible SRT in the first configured preferred language that has an eligible result.
- **Sidecar:** subtitle alongside the primary video, with its exact basename: `Film.mkv` → `Film.srt`.
- **Preparation:** explicit per-movie acquisition/save operation, separate from byte-range video streaming.
- **Existing track:** an authorized exact-basename SRT already on library storage; its language may be unknown.

This feature-local glossary is settled for Quill; no root glossary rewrite is authorized.

## Provider and selection

BS Player was evaluated first; unified recon line 13 found no public supported hash-service API contract. Do not integrate it or claim its endpoint is dead. Patron Q3 selected OpenSubtitles.com REST as the sole provider for personal non-commercial media-player use, based on official staff endorsement and documented integration (recon lines 17–21). This satisfies the planning prerequisite from Q1/Q2; it is not a live successful download or a blanket subtitle-content license. No redistribution rights are inferred.

Use an injected application Api-Key and descriptive application/version User-Agent. Anonymous allowance is documented as 5 downloads/24h/IP; verify current key settings and responses at runtime. Do not cache temporary download URLs or rely on a fixed URL lifetime: unified recon line 21 explicitly limits that evidence. No automatic download retries or invented local quota reset counters. Propagate safe upstream Retry-After on quota responses.

Look up eligible moviehash first, falling back to title only when hash is ineligible or yields no match within preferred languages. Q8 adopts the 16-hex-character size + first/last 65,536-byte algorithm for videos at least 131,072 bytes, with authoritative reference-vector verification required at Phase B pickup before implementing it. These algorithm values describe a protocol, not configurable safety thresholds.

Choose the first matching preferred language, then moviehash match first, highest download_count, lowest subtitle file_id. Never fall back outside preferences. Multiple candidates are resolved deterministically, not rejected merely for a tie. Download compatible SRT only; no eligible result is noMatch. Preserve existing sidecars without overwrite or another download. Language suffixes are not established BS Player autodetection and are not used. The single exact-basename SRT represents one selected language per video.

## Trigger, settings and deployment

Explicit per-movie preparation only; ordinary streaming never initiates provider traffic. Backend-only scope was deliberately ratified in Q5; a web workflow is a follow-up. Existing sidecar checks occur after movie/video/library authorization but before acquisition configuration or write checks, allowing alreadyPresent even with saving disabled, a read-only mount, or no Api-Key.

Server options:

| Setting | Decision |
| --- | --- |
| Subtitles:SaveEnabled | Defaults false; writable acquisition explicitly enabled |
| Subtitles:PreferredLanguages | Empty by default; ordered provider codes required for acquisition |
| Subtitles:OpenSubtitles:ApiKey | Secret injection only; never committed or logged |
| Subtitles:OpenSubtitles:BaseUrl | Explicit validated provider API URL |
| Subtitles:OpenSubtitles:UserAgent | Explicit descriptive application/version value |
| DownloadAllowedHosts, MaxDownloadBytes, RequestTimeout | Explicit acquisition options; allowlist and positive bounds validated; Quill nests under the provider options section |

No guessed numeric safety defaults. Missing/invalid acquisition settings yield an actionable disabled/unconfigured 409 on preparation, without preventing demo startup or existing-track serving. Read-only demo makes no acquisition calls or writes. Daily-driver setup documents a deliberately writable library mount/config without committing machine-specific paths or credentials. Existing-track serving remains bounded and read-only.

## Public contract

Contract chain is C# DTO → web/src/api/openapi.json → generated web/src/api/types.ts, even with no frontend workflow. Never hand-write duplicate TS types.

| Operation | Approved result |
| --- | --- |
| POST /movies/{id:int}/subtitles/prepare | No client path/provider URL or language override; server preferences apply. 200 body `{ outcome: saved\|alreadyPresent\|noMatch, selectedLanguage: string? }`; language populated on saved when known, null for alreadyPresent/noMatch |
| GET /movies/{id}/subtitles/sidecar | Authorized exact-basename SRT bytes, 200 application/x-subrip; no provider traffic |

Prepare failures: 400 malformed ID, 403 library authorization/containment denial, 404 movie/video absent, 409 acquisition disabled/unconfigured/unwritable or local write-permission failure, 429 upstream quota, 502 provider/content/unsafe-URL failure, 504 provider timeout. GET returns 404 for missing movie/video/sidecar and 403 for unauthorized library access. Q10 refines Q6: preparation write-permission/read-only failures are 409; containment denials stay 403. Errors disclose no filesystem paths or secrets. Quill must encode the declared malformed-ID behavior explicitly rather than rely on route-constraint defaults.

## Architecture and file responsibilities

Existing Core, Infrastructure and Api projects only; no new NuGet/npm dependencies, schema changes, top-level folder, project or architectural layer. Two narrow Core ports: `ISubtitleProvider` and `ISubtitleSidecarStore`. Keep `ILibraryFileReader` read-only per ADR 0037. No generic provider registry/repository or unrelated refactor. ADR 0038 records the provider and separate read/write boundary.

Planned responsibility map (new filenames are design targets, not claims of existing code):

- `src/LamuFlix.Core/Ports/ISubtitleProvider.cs`, `ISubtitleSidecarStore.cs`: narrow provider/storage contracts; subtitle models and orchestration follow existing Core conventions.
- `src/LamuFlix.Infrastructure/`: OpenSubtitles HTTP/hash adapter; `FileSystem/` sidecar adapter with contained reads and atomic saves.
- `src/LamuFlix.Api/Endpoints/SubtitleEndpoints.cs`: approved routes, response DTO/options and composition following existing conventions.
- Existing DI/endpoint mapping entrypoints: names verified by scoped recon at Phase B pickup before edits; no guessed filename rewrites.
- Configuration, Compose and README: explicit read-only/writable setup and injected secrets; retain safe demo defaults.
- `web/src/api/openapi.json`, `web/src/api/types.ts`: contract regeneration only.
- Existing UnitTests/IntegrationTests projects: focused new tests and bounded existing-test edits.

Local subtitle HTTP preparation/serving is independent of Features:LocalPlay. No Process.Start, player arguments or launch-path changes. Actual local-player execution remains gated behind Features:LocalPlay. These are deliberate Q7 care-item rulings, not an exemption for future launch work.

## Safety and failure behavior

Authorize resolved primary video folder and regular file before hashing, reading or writing; server derives the exact sidecar path. Reject traversal, reparse/symlink escapes and nonregular files. Same-directory temporary files, cancellation cleanup and atomic create-without-replace avoid partial sidecars and overwrite. A concurrent-create collision resolves to alreadyPresent after safe recheck.

Validate provider download URLs as HTTPS on an explicit provider-host allowlist; reject redirects outside it and caller-controlled URLs. Verify allowed hosts during pickup. Enforce configured size/time bounds; reject non-SRT/invalid/oversized downloads. No automatic download retry. Never persist temporary URLs or log keys, paths or subtitle content. Bound serving resources as well. Follow existing TimeProvider/seeded Random and harness conventions wherever applicable.

## Task ordering, test strategy and gate expectations

1. After Gate 1, Phase B pickup: recon merged/delivered DEV-457 and DEV-454 contracts, actual composition filenames, current provider headers/error schema and safe download-host allowlist; verify authoritative hash vectors. Amend drift before implementation. Queued dependencies are not proof of delivery.
2. Define approved DTO/options, models and the two narrow ports; validate configuration without breaking demo startup.
3. Implement/test hash and deterministic language selection; then HTTP provider adapter using existing WireMock.NET.
4. Implement/test contained sidecar reads and atomic writes using real temporary filesystem fixtures; then orchestration and API integration.
5. Regenerate OpenAPI/TS, document demo/writable setup and run applicable verification gates.
6. Collect separate live configured-key, writable-save, BS Player autodetection and read-only-serving runtime receipts; report absent receipts honestly.

Use existing xUnit/FsCheck/WireMock.NET conventions and packages only. Cover preference order/ties/noMatch; hash vectors/boundaries; existing-sidecar precedence over missing key/disabled writes; read-only no-network/no-write; ordinary writable save; concurrent creates/no overwrite/cancellation cleanup; containment/reparse/nonregular denial; oversized/invalid content; URL/redirect rejection; provider auth/quota/timeout/error mapping; exact served bytes and playback regressions. No real credentials in fixtures; no driver mocks for translation/serialization-dependent behavior.

Implementation gates: all applicable harness build/analyzer/complexity/inspection/test/property/security/format/mutation gates, with thresholds from harness.yml and no lowering; OpenAPI/TS drift and relevant web checks for generated contract diff. Preserve PASS, SKIPPED (scope-empty), SKIP, N/A and Could not run distinctions. This planning-only commit does not claim those implementation gates ran.

Runtime acceptance needs actual provider search/download using a configured key, successful writable sidecar save, supported BS Player autodetection of exact-basename SRT, and read-only demo serving an existing sidecar without acquisition. Live checks that cannot run are Could not run, never PASS. Automated tests and documented viability do not substitute for runtime evidence.

## Complete grill decision index

Full question and answer text, cited bases, rationale and implications are append-only in CONCLUSIONS.md; that companion is part of this brief, not optional background.

| Question | Settled answer |
| --- | --- |
| Q1 | Preserve ticket scope; provider verification required; initial recon stop |
| Q2 | Uphold stop pending auth/quota/terms and exact filename evidence |
| Q3 | Documentation/staff evidence satisfies planning prerequisite; OpenSubtitles selected; live verification separate |
| Q4 | Exact-basename SRT, configured language order, preserve existing, deterministic tie-break amendment |
| Q5 | Explicit prepare/read operations, backend-only, separate writable/read-only modes |
| Q6 | Approved routes/statuses/options/contract chain; selectedLanguage and existing-sidecar precedence amendments |
| Q7 | Two named narrow ports, no dependencies/schema/new projects, bounded file scope, LocalPlay separation |
| Q8 | Containment/atomic save/URL bounds/quota hygiene; hash-first with limited title fallback |
| Q9 | Task/test/gate ordering; technical verification at Phase B pickup, runtime receipts separately |
| Q10 | Safe settings, bounded responsibilities/serving, 409 write-denial refinement |
| Q11 | Blocking bar, three-axis exact-head publication, frozen scope, two review rounds |
| Q12 | Shared understanding, grill closed; four-file handoff approved, no structural departure |

ASSUMPTIONS.md records the policy/taste choices tagged [assumed]. Quill must use this vocabulary and proposed ADR 0038 consistently. New decision changes are recorded in this brief before any corresponding spec/plan/tasks repair.

## Authoritative analyze adjudication addendum (2026-11-09)

Current status: **blocked: structural**. This addendum supersedes the original no-structural-blocker/no-constitution-departure assertions and the Q12 feature-local-only file restriction. It does not reopen the grill or grant analyze clean, PLAN FROZEN, Gate 1, implementation or runtime clearance. Full questions, superseded proposal, correction request and corrected cited Patron rulings are appended in CONCLUSIONS.md under Analyze round 1 adjudication follow-up.

1. **C1/V resolved:** Malformed/unparseable transport route ID remains explicit 400 ProblemDetails with traceId; it must be deliberately implemented rather than trusting an int route constraint. Validly bound commands/queries use sealed handlers and FluentValidation decorators; ValidationException remains 422 in the single exception mapper. Document/test the distinct validation 422 contract as well as the existing approved statuses. Basis: constitution Principle II and V:451-466; Q6; Patron corrected item 1.
2. **C2/VI resolved:** Subtitle-provider health registration uses ServiceDefaults/Infrastructure-owned wiring, zero external HTTP/network calls and no quota-consuming operations. This optional-provider check always reports Healthy with sanitized local metadata ready or disabled_or_unconfigured, including enabled but invalid/missing acquisition config. This does not assert upstream reachability or alter other dependency health results. Names belong in TelemetryConstants; processing/error logs carry movie id. Add coverage for the complete enabled/disabled/configured/invalid matrix and zero provider calls. Basis: Principle VI:478-490/623-624; Q5/Q8/Q10; Patron corrected item 2.
3. **C4/VII unresolved owner decision:** blocked: structural — Approve a narrowly scoped Principle VII departure for SubtitlesOptions / OpenSubtitlesOptions so missing or invalid subtitle acquisition settings, with no guessed numeric defaults, do not fail host startup via ValidateOnStart(), and are instead evaluated at POST /movies/{id}/subtitles/prepare to return 409 Conflict, preserving read-only startup and existing-track serving? Put this exact unresolved checkbox in the spec and spec PR. Gate 1 stays closed until the user approves; Patron cannot close it. Preserve typed options and secrets isolation. Do not implement or amend the constitution on assumed approval. If approved, require the corresponding scoped constitution/governance amendment through its normal approval lifecycle before affected feature implementation. Principle I port enumeration governance remains independently required. Basis: Principle VII:500-503/625-626; Q8/Q10; role section 2.3(b); Patron corrected item 3.
4. **C4/VIII resolved:** Authorize append-only repository-root CONTEXT.md entries for Ordered language preferences, Selected subtitle, Sidecar, Preparation and Existing track, using settled term/definition/avoid meanings and preserving all prior content. Schedule in the same PR introducing the terms; the spec PR must not introduce them while deferring this compliance to a later delivery PR. Quill may append these five documentation entries as a specifically authorized planning compliance edit; no root glossary rewrite. This supersedes only the original Q12 file restriction. Basis: Principle VIII:522/537/629; care item 6; recon-DEV-453:52-56; Patron corrected item 4.

Scope remains the same subtitle deliverable. No invented numeric defaults, new packages or production changes are authorized by this adjudication. The structural checkbox is an owner decision and is not resolved by recording it.

## Authoritative Plan Challenge adjudication addendum (2026-11-10)

Recorded by Keel from the consolidated Patron response to Phase 2 step 6. These cited rulings govern the one numbered Quill repair list and supersede earlier implementation-name and underspecified mechanism language only as stated below. Planning repairs remain pending independent reread; no PLAN FROZEN, analyze-clean, Gate 1, implementation, runtime, or merge clearance is granted. The original C4/VII owner checkbox remains verbatim and unchecked. No new owner checkbox is added.

# DEV-453 — Phase 2 Step 6 Plan Challenge Adjudication Rulings (from Patron)

**Overall Verdict**: All seven adjudication items across `findings-DEV-453-Sentry`, `findings-DEV-453-Ledger`, and `findings-DEV-453-Compass` are settled below with cited bases. **No new owner checkbox is introduced.** The existing unresolved C4/VII owner structural checkbox (`specs/DEV-453/spec.md:12`) remains the sole Gate 1 blocker; **Gate 1 stays closed and no structural approval can be inferred.**

---

## 1. Sentry F1 & F2 — Redirect Prohibition, Pre-Request Host/HTTPS Validation (Search + Download), and T001 STOP

- **Verdict**: **Fix in spec/plan/tasks (blocks T001/T008)** — Prohibit automatic HTTP redirect following (`AllowAutoRedirect = false`) on the OpenSubtitles HTTP client; validate `https://` scheme and membership in the explicit verified provider-host allowlist **before issuing every request** (covering both configured search `Subtitles:OpenSubtitles:BaseUrl` and temporary `/download` URLs); **reject all HTTP redirects (`3xx`) outright as `502`** rather than inventing an unverified numeric hop-count cap; require Phase B pickup `T001` to verify the exact OpenSubtitles API host and download host(s) with a hard **STOP** before `T008` if unverified or if live protocol evidence requires redirect handling.
- **Cited Basis**:
  - **Challenger evidence**: `findings-DEV-453-Sentry` F1 (`lines 21–39`) and F2 (`lines 41–52`) identify that default `HttpClientHandler` follows redirect hops before caller host checks (SSRF vector) and that `Subtitles:OpenSubtitles:BaseUrl` lacked explicit HTTPS + host-allowlist bounds.
  - **Brief & Spec authority**: `specs/DEV-453/brief.md:48, 52, 85, 89`, `specs/DEV-453/CONCLUSIONS.md` Q8/Q9/Q10 (`lines 108, 120, 132`), `specs/DEV-453/spec.md:34, 191, 194, 199` (`FR-009`, `FR-012`, `FR-017`), `recon-DEV-453:18`, and `AGENTS.md` Non-negotiables (*"Never hardcode a threshold"*). Because `/download` returns a temporary download URL in its JSON payload (`recon-DEV-453:18`), rejecting `3xx` redirects outright eliminates redirect-chain SSRF without inventing a hardcoded hop threshold.

---

## 2. Sentry F4, F5 & F6 (plus F3 route-capture note) — Serve Anti-Sniff Headers, Client Abort vs Provider Timeout, and Personal-Scale Concurrency/Crash Dispositions

- **Verdict**:
  1. **Sentry F4 (Fix in spec/tasks T011/T012)**: On `GET /movies/{id}/subtitles/sidecar`, require response headers `X-Content-Type-Options: nosniff` and `Content-Disposition: attachment; filename="<safe-ascii-or-rfc5987-basename>.srt"` derived strictly from the server-resolved safe basename (never exposing directory paths), and assert both headers in `T011`.
  2. **Sentry F5 (Fix in spec/tasks T007/T008/T011/T012)**: Distinguish caller disconnect (`HttpContext.RequestAborted`) from upstream provider timeout (`RequestTimeout` / internal timeout token): only upstream provider timeout maps to `504 Gateway Timeout` (`FR-007`), whereas client request cancellation propagates as request cancellation (never logged or mapped as `504`), without promising rollback after an atomic sidecar creation (`FR-011`) has already committed.
  3. **Sentry F6 (Accepted nonblocking risk in plan/tasks T009/T012)**: Record (a) concurrent pre-create double download (two concurrent prepare requests both pass the initial absent-sidecar check and download before one wins atomic create and the loser resolves to `alreadyPresent`) and (b) process-crash orphan temp files (when the process dies abruptly between temp file creation and atomic move, outside normal cancellation cleanup) as **accepted nonblocking risks** at personal-library scale; do **not** add per-movie locking or background temp-janitor scope.
  4. **Sentry F3 alignment (`findings-DEV-453-Sentry:54–66`)**: Ensure `T012`/`T011` explicitly state that the route captures the raw `{id}` segment (or custom binding) and parses it so a non-integer malformed ID deterministically returns `400 ProblemDetails + traceId` (`FR-007`) rather than falling through an `{id:int}` route constraint to `404`.
- **Cited Basis**:
  - **Challenger evidence**: `findings-DEV-453-Sentry` F3 (`lines 54–66`), F4 (`lines 68–75`), F5 (`lines 77–85`), and F6 (`lines 87–99`).
  - **Brief & Spec authority**: `specs/DEV-453/brief.md:15, 29, 63, 83–85, 127`, `specs/DEV-453/CONCLUSIONS.md` Q6/Q8/Q10/Q11 & C1/V (`lines 84, 108, 132, 144, 179`), `specs/DEV-453/spec.md:30–31, 34, 189–190, 193` (`FR-007`, `FR-008`, `FR-011`), and `specs/DEV-453/plan.md:29` (personal-library scale; frozen Q3–Q10 scope; Q11 every-Low disposition bar).

---

## 3. Ledger M1 — Retain Exact Existing C4/VII Owner Checkbox and Q10; Include Options Enumeration in Eventual Principle VII Amendment

- **Verdict**: **Retain existing C4/VII checkbox verbatim; reject Option B alternative; expand post-approval governance note** — Keep the exact unresolved C4/VII owner structural checkbox in `specs/DEV-453/spec.md:12` and settled Q10 (`brief.md:52, 129`) unchanged; do **not** reopen or add Ledger's conditional-`ValidateOnStart` alternative (which would fail host startup when `SaveEnabled=true` and acquisition settings are invalid, contradicting Q10). In `plan.md` and `tasks.md` (`T001c`/`T003`), record that if/when the owner approves C4/VII, the resulting scoped Principle VII constitution/governance amendment must also append the new subtitle options record(s) to Principle VII's closed options-record list (`.specify/memory/constitution.md:500–507`).
- **Cited Basis**:
  - **Challenger evidence**: `findings-DEV-453-Ledger` M1 (`lines 10–12`).
  - **Brief, Recon & Constitution authority**: `specs/DEV-453/brief.md:52, 129`, `specs/DEV-453/CONCLUSIONS.md` Q8/Q10 & Corrected Item 3 (`lines 108, 132, 193–199`), `specs/DEV-453/spec.md:8–14`, and `recon-DEV-453:34–36` (`.specify/memory/constitution.md:500–507`).

---

## 4. Ledger M2 — Functionality-Named Options/Telemetry Types vs Vendor Config Nesting and Verbatim Checkbox Preservation

- **Verdict**: **Fix in spec/plan/tasks** — Name all C# options records, properties, and `TelemetryConstants` identifiers by functionality (e.g., `SubtitlesOptions` and `SubtitleProviderOptions`, plus functional telemetry constants—never `OpenSubtitlesOptions` or an `OpenSubtitles` telemetry constant in C# identifiers) so Principle VIII is satisfied with **zero vendor-allowlist departure**. Keep the settled configuration section path `Subtitles:OpenSubtitles:*` (`brief.md:47–50`) as a Principle VIII-permitted vendor-mandated configuration key. Do **not** claim that placing a vendor-named options class in `Infrastructure` exempts it from Principle VIII (Principle VIII permits vendor names in code only on the Infrastructure adapter class itself and the closed allowlist). Preserve the existing C4/VII checkbox text in `spec.md:12` **verbatim** (`SubtitlesOptions / OpenSubtitlesOptions`) and add a one-line note in `spec.md`/`plan.md`/`tasks.md` stating that the implementation type for the `Subtitles:OpenSubtitles` config section is functionality-named (`SubtitleProviderOptions` or nested functional record) to honor Principle VIII.
- **Cited Basis**:
  - **Challenger evidence**: `findings-DEV-453-Ledger` M2 (`lines 14–15`).
  - **Brief, Recon & Constitution authority**: `recon-DEV-453:42–48` (`.specify/memory/constitution.md:522–537`, Principle VIII: *"Types, members, and telemetry identifiers MUST be named by functionality, not by vendor... Vendor names are permitted only on the Infrastructure adapter class itself, on vendor-mandated configuration keys, on the Core port `IOmdbRequestGate`, on the options records `OmdbQuotaOptions` and `TmdbOptions` in `LamuFlix.Core.Options`, and on the provider tag-value constants `OmdbProviderName` and `TmdbProviderName` in `TelemetryConstants`"*); `specs/DEV-453/brief.md:47–50, 128–129`.

---

## 5. Ledger M3 & M4 — Contained Hash Read Seam (`ILibraryFileReader`), Data-Only Provider Port, Feature-Folder/ArchitectureTests Pickup Verification, and Single Exception Mapper

- **Verdict**: **Fix in plan/tasks (blocks T001/T001c/T002/T006/T012)** —
  1. **M3 (Hash read containment seam)**: `ISubtitleProvider` must accept computed moviehash (`string?`), file size (`long`), title (`string?`), and ordered language preferences as data—**never a local filesystem path or stream**. Moviehash computation reads the first and last 65,536 bytes through the existing authorized read-only `ILibraryFileReader` seam (`ADR 0037`) after movie/video/library authorization (`FR-011`), without widening `ILibraryFileReader` to writes and without opening filesystem paths inside the OpenSubtitles adapter. Phase B pickup `T001` must verify the exact `ILibraryFileReader` read/stream contract on `main` (**STOP** before `T002`/`T006` if the seam cannot read the head/tail windows without contract drift).
  2. **M4a (Feature-folder enumeration & ArchitectureTests)**: At Phase B pickup (`T001`/`T001c`), Wisp must inspect the exact Principle I feature-folder enumeration in `.specify/memory/constitution.md` and namespace-isolation rules in `ArchitectureTests.cs`; Keel's `T001c` Principle I compliance step must include any required feature-folder enumeration extension alongside the two named ports as compliance-only (no assumed exemption; handlers resolve movie/video/library strictly via shared `Core/Ports` and `Core/Domain` without cross-referencing sibling feature folders; **STOP** before creating a new Core feature folder or ports if unconfirmed).
  3. **M4b (Single ProblemDetails mapper)**: All domain/application prepare and serve failure statuses (`403`, `404`, `409`, `429` with optional `Retry-After`, `502`, `504`) must be represented by typed Core exceptions/failure categories translated in the existing single `ProblemDetails` exception mapper (`Principle V`), with no new result-type abstraction layer and no endpoint-local try/catch status mapping.
- **Cited Basis**:
  - **Challenger evidence**: `findings-DEV-453-Ledger` M3 (`lines 17–18`) and M4 (`lines 20–21`).
  - **Brief, ADR & Constitution authority**: `docs/adr/0038-subtitle-provider-and-sidecar-boundary.md:9–11`, `specs/DEV-453/brief.md:67, 83, 127`, `specs/DEV-453/CONCLUSIONS.md` Q7/Q8 & C1/V (`lines 96, 108, 179`), `specs/DEV-453/plan.md:41–42, 45, 70`, and `recon-DEV-453:5, 38–40` (Principle I & Principle V).

---

## 6. Compass F2–F5 (plus F1 & Ledger L2–L4 Editorial Fixes) — `selectedLanguage` on `saved`, Title Source, SRT Validity Criteria, and `Retry-After` Policy

- **Verdict**: **Fix in spec/plan/tasks** —
  1. **Compass F2 (`selectedLanguage` on `saved`)**: Because `saved` is reached only when a candidate matches a configured code in `Subtitles:PreferredLanguages` (`FR-002`), `selectedLanguage` is **always populated (non-null)** when `outcome == saved`, and **always `null`** when `outcome` is `alreadyPresent` or `noMatch`. Keep the DTO field `string? SelectedLanguage` nullable across the three-outcome union (no DTO nullability break), and tighten `spec.md` (`Scope item 6`, `US2`, `FR-005`) from *"populated on saved when known"* to *"always populated with the matched preferred language code on `saved`, and `null` on `alreadyPresent`/`noMatch`"*.
  2. **Compass F3 (Title fallback source & empty-title rule)**: Add the existing `Movie` aggregate/metadata title field inspection to `T001` pickup facts (**STOP** before `T005`/`T006` if unconfirmed). Specify in `FR-013`/`T006` that title fallback uses the authorized movie's primary title property when hash lookup is ineligible (`< 131,072` bytes) or returns no match within `PreferredLanguages`; if that title is null/empty/whitespace, skip the provider title request and return `200 { outcome: noMatch, selectedLanguage: null }`.
  3. **Compass F4 (Compatible SRT content/encoding criteria)**: Specify explicit minimal SRT validity criteria in `FR-012`/`T007`/`T008` (with `T001` verifying whether OpenSubtitles `/download` returns raw `.srt` bytes or a compressed wrapper, **STOP** on mismatch before `T007`/`T008`): payload must be non-empty, `<= MaxDownloadBytes`, valid UTF-8 text (UTF-8 BOM permitted), must not start with HTML/XML/JSON/binary archive signatures (`<`, `{`, `[`, `PK`, gzip `1F 8B`), and must contain at least one valid SubRip timestamp cue line (`HH:MM:SS,mmm --> HH:MM:SS,mmm` or `.` millisecond separator); otherwise reject as `502` without writing.
  4. **Compass F5 (`Retry-After` propagation policy)**: On upstream `429`, preserve and propagate the upstream `Retry-After` header whenever it parses as a valid RFC 9110 `Retry-After` value (`HttpResponseHeaders.RetryAfter`: either a non-negative integer `delta-seconds` or a valid `HTTP-date`), without inventing a numeric cap and without dropping valid `HTTP-date` values; omit the header when absent or malformed.
  5. **Low editorial repairs (`Compass F1`, `Ledger L2`, `Ledger L3`, `Ledger L4`)**: Renumber the second scenario `7.` in `spec.md:131` to `8.` (`Compass F1`); outdent `CONTEXT.md` to repository root in `plan.md:98` (`Ledger L2`); verify/preserve clean UTF-8 `>= 131,072 bytes` and arrows in `spec.md` (`Ledger L4`); for `Ledger L3`, ADR 0038 remains Keel-owned Proposed and is dispositioned as accepted historical grill-closure context (or Keel may add a one-line pointer to the brief addendum).
- **Cited Basis**:
  - **Challenger evidence**: `findings-DEV-453-Compass` F1–F5 (`lines 24–32`); `findings-DEV-453-Ledger` L2–L4 (`lines 27–32`).
  - **Brief & Spec authority**: `specs/DEV-453/brief.md:19–20, 31–35, 60, 85, 89`, `specs/DEV-453/CONCLUSIONS.md` Q4/Q6/Q8/Q9 (`lines 60, 84, 108, 120`), `specs/DEV-453/spec.md:184–195` (`FR-002`, `FR-005`, `FR-007`, `FR-012`, `FR-013`), and `AGENTS.md` Non-negotiables (*"Never hardcode a threshold"*).

---

## 7. Ledger L1 — Retain Always-Healthy Optional-Provider Health Ruling and Exclude from `/health/live`

- **Verdict**: **Accepted nonblocking risk + explicit `/health/live` exclusion in spec/plan/tasks (`FR-021`, `T012b`)** — Retain the settled C2/VI ruling that the optional subtitle-provider health check performs zero network/quota calls and always reports `HealthStatus.Healthy` with sanitized local readiness metadata (`ready` or `disabled_or_unconfigured`), and explicitly specify in `FR-021`/`plan.md`/`T012b` that this check is excluded from `/health/live` (which runs no dependency checks per Principle VI).
- **Cited Basis**:
  - **Challenger evidence**: `findings-DEV-453-Ledger` L1 (`line 25`).
  - **Brief & Constitution authority**: `specs/DEV-453/brief.md:128` (Addendum item 2, C2/VI), `specs/DEV-453/CONCLUSIONS.md:184–190`, and `.specify/memory/constitution.md:478–490, 623–624` (Principle VI).


## Authoritative Phase B pickup adjudication addendum (2026-11-11)

Authority: Patron ruling committed at `261290ff9510a2ecc7438352ce5178030d8bb548`, reproduced below from CONCLUSIONS.md. This supersedes the prior external-source pickup STOPs (including Q8 external reference-vector verification and Plan Challenge item 1 host/protocol pickup verification) for T001/T001b and dependent implementation. It does not claim actual live provider compatibility or completed T005/T007 tests. T022 retains all live acceptance obligations.

Pickup facts: delivered DEV-457/DEV-454; authorized reader implementation returns a seekable file-backed Stream without changing ILibraryFileReader; title is Movie.Title; AddLamuFlixHandlers lives in Api/HandlerRegistration.cs; existing subtitle endpoints and /api composition are extended preserving delivered DEV-454 contracts. ArchitectureTests enforces sibling isolation; append the two approved ports after the existing 17 whitelist alternatives with their definitions in T002 (19 total), preserving order and all test/count rules. Demo uses the read-only Compose bind mount; daily-driver Api uses host Library:RootPath and host process write permissions, not an invented library mount in daily-driver Compose. Basis: recon-DEV-453 supplemental pickup and isolation/mount records supplied by Bernstein.

T001c compliance: retain Proposed ADR 0038; add only ISubtitleProvider and ISubtitleSidecarStore to Principle I ports and Subtitles to feature enumeration, with MINOR governance amendment and template review. No omitted-port backfill or production changes. Principle VII owner approval is recorded in CONCLUSIONS.md, but its scoped options-list/ValidateOnStart amendment remains separately required before affected T003/T004 implementation; T001c does not silently provide it.

## Phase B T001 / T001b Pickup Scope Ruling — Configurable Provider Options, WireMock.NET Contract, and Synthetic Moviehash Vectors (2026-11-11)

- **Verdict**: Approved — clear the `T001`/`T001b` external-source hard STOPs by settling OpenSubtitles provider host/protocol/wire details as explicit configurable options (`Subtitles:OpenSubtitles:BaseUrl`, `Subtitles:OpenSubtitles:DownloadAllowedHosts`, `ApiKey`, `UserAgent`, `MaxDownloadBytes`, `RequestTimeout`; with no hardcoded runtime fallback defaults so missing/invalid settings return prepare-time `409 Conflict` per Q10/C4-VII, and `https://api.opensubtitles.com` documented only in sample/operator config) verified via WireMock.NET per `T007` without live network calls (keeping `AllowAutoRedirect = false`, `3xx` rejected as `502`, JSON `/api/v1/subtitles` search + `/api/v1/download` returning a temporary `link` URL, and raw UTF-8 `.srt` bytes at the temporary link validated by the Compass F4 SRT criteria), and by implementing the 16-hex-character moviehash algorithm per `brief.md:33` (`uint64` file size + little-endian `uint64` word sums of the first and last 65,536 bytes modulo $2^{64}$, lowercase `:x16`, for files $\ge 131,072$ bytes) verified via deterministic synthetic reference vectors and property tests per `T005`, while keeping live credentialed requests strictly in `T022` (`Could not run` if credentials are absent, never folded into PASS).
- **Cited Basis**:
  - **Exhausted Repo Roots & Configurable Provider Options (`recon-DEV-453:16–17, 37–42`, `specs/DEV-453/brief.md:31, 47–52, 146–149`, `CONCLUSIONS.md` Q3/Q8/Q10)**: `recon-DEV-453:16–17, 37–42` confirms that external OpenSubtitles hostnames, live protocol responses, and third-party test vectors do not exist in repository roots and search cannot be widened beyond the worktree; under Q8/Q10 (`brief.md:47–52`) and Plan Challenge item 1 (`brief.md:146`), API and download hosts are operator-configured options (`Subtitles:OpenSubtitles:BaseUrl` and `Subtitles:OpenSubtitles:DownloadAllowedHosts`) validated for `https://` and allowlist membership before each request (`AllowAutoRedirect = false`, all `3xx` rejected as `502`), and tested deterministically via WireMock.NET in `T007` with zero live network calls or hardcoded runtime thresholds (`AGENTS.md` Non-negotiables).
  - **Self-Contained Moviehash Algorithm & Synthetic Vector Verification (`specs/DEV-453/brief.md:33`, `CONCLUSIONS.md` Q8/Q9, `specs/DEV-453/tasks.md:90–91`)**: `brief.md:33` and Q8 (`CONCLUSIONS.md:108`) fully specify the deterministic 16-hex-character OpenSubtitles moviehash algorithm (`uint64` file length summed with 8-byte little-endian `uint64` words across the first 65,536 bytes and last 65,536 bytes via unchecked modulo-$2^{64}$ arithmetic, formatted as 16 lowercase hex digits, eligible at $\ge 131,072$ bytes); deterministic synthetic reference vectors (e.g., all-zero 131,072-byte payload $\rightarrow$ `0000000000020000`, known byte patterns, and FsCheck properties in `T005`) verify the exact mathematical specification without requiring external fixture files not present in the repository.
  - **Separation of Automated Pickup/Build Verification from Live Runtime Receipts (`specs/DEV-453/brief.md:29–31, 94, 100`, `CONCLUSIONS.md` Q3/Q9, `tasks.md:67–68`)**: Q3 (`CONCLUSIONS.md:48`) and Q9 (`CONCLUSIONS.md:120`) explicitly separate automated build/test verification (WireMock.NET + unit/property suites) from live credentialed runtime verification (`T022`), so external live network/vector absence in repo roots does not block `T001`/`T001b` closure or `T002`–`T021` implementation.


## Principle VII amendment execution (2026-11-11)

Owner authority: CONCLUSIONS.md Phase 2 Step 7 (PR #358 C4/VII approval, lines 211–216); Conductor explicitly directed application. Constitution v1.15.0 appends functionality-named SubtitlesOptions and SubtitleProviderOptions to the closed options-record list and applies only the approved subtitle-acquisition ValidateOnStart departure: missing/invalid acquisition settings, with no guessed numeric defaults, never block host startup or existing-track serving and are checked at explicit preparation for 409 Conflict after the existing-sidecar check. Typed options and secret isolation remain mandatory; vendor config path Subtitles:OpenSubtitles remains unchanged, without a vendor-named options type or Principle VIII expansion. All other startup validation and LocalPlay rules are preserved. This locally applies the previously required scoped amendment; it supersedes earlier pending-amendment statements in this brief and current pickup summaries. Normal delivery review/approval remains required; this records no production implementation, runtime acceptance or gate PASS. T003 still depends on T002; T004 still depends on T003. No implementation task is marked complete by the governance edit.

## Phase B sidecar route drift alignment (2026-11-11)

Authority: CONCLUSIONS.md, Phase B T011 / T012 Scope & API Shape Ruling (2026-11-11), and the Conductor-directed bounded alignment. This addendum supersedes the earlier Q6 route shape and serve-route references in this brief. Preserve delivered DEV-454 `GET /movies/{id:int}/subtitles` (`200 SubtitleTrackDto[]`) and `GET /movies/{id:int}/subtitles/{trackId}/content` (`200 text/vtt; charset=utf-8`). DEV-453 serves exact-basename SRT bytes only at `GET /movies/{id}/subtitles/sidecar`; preparation remains `POST /movies/{id}/subtitles/prepare`. Both DEV-453 operations retain raw `{id}` parsing for reachable malformed-id 400 versus absent-movie 404. All settled serve headers, bytes, error maps and acquisition-setting independence remain unchanged. Patron ruled that this preserves ticket scope and needs no YouTrack change or owner checkbox.

Apply the route to spec.md, plan.md and tasks.md T011-T015. T011 proves sidecar serving and preservation of the delivered listing/content contracts; T012 extends the existing endpoint module without replacing either delivered endpoint. T013/T014 preserve existing operation/name inventories while adding prepare and sidecar serve; T015 regenerates the C# to OpenAPI to TypeScript chain. T009 and T010 remain checked as committed by Anvil at 84fc6125. No production edits, new completion claims, runtime acceptance or gate PASS are implied by this document alignment.
