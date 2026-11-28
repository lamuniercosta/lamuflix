# Feature Specification: SubDL provider and shared archive reader

**Feature Branch**: `feature/554-spec`

**Created**: 2026-11-28

**Status**: gate1: provisional

**Input**: Ticket DEV-554 (size M) as recorded in `recon-DEV-554:19–75`; grill-closed `brief.md` (Q1–Q7 + Step 3.3 governance ruling); `CONCLUSIONS.md`; Maestri notes `DEV-554`, `recon-DEV-554`; `specs/PRODUCT.md`. Quill invents no decisions beyond the brief.

## Owner question — Gate 1

Constitution departure (charter §2.3(b)): Principle VII's closed options-record list does not enumerate a `Subtitles:SubDl` acquisition exception (constitution.md:782-807; OpenSubtitles-only exception at :788-801). Patron recorded the required narrow VII amendment and Gate 1 checkbox in `brief.md` Step 3.3 / `CONCLUSIONS.md` Step 3.3 at `508f8e48`. The Spec Kit PR MUST carry the amendment draft that (1) adds the typed `Subtitles:SubDl` options entry to the closed list (:782-784), (2) **replaces** the existing singular-provider 409 clause (:795-801) with Step 3.3 provider-set search and chosen-provider download rules, (3) updates the corresponding VII checklist (:946-948), and (4) records DEV-501 / ADR 0041 impact in the Sync Impact Report with the constitution version bump. Preserve OpenSubtitles binding and validation, `SaveEnabled`, read-only precedence, existing-track serving, languages route, and secret isolation. Search aggregation **changes**: with valid SubDL and a blank OpenSubtitles key, search returns SubDL results (today that blank key yields 409). The PR carries this **unchecked** owner checkbox; Gate 1 stays closed until the owner ticks it and merges. Quill does **not** claim the amendment is already applied on `origin/main` or already approved. Any separate ADR edit requires a cited ruling under Q4.

- [ ] blocked: structural — approve the VII amendment adding the SubDL acquisition options exception? With the approved caps and valid SubDL settings, search returns SubDL results even when the OpenSubtitles key is blank. Search returns 409 only when no provider has effective, valid settings.

## Clarifications

### Session 2026-11-28

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Transport (v2 search + ZIP download, `X-API-Key` only at validated HTTPS origin), movie-only three search kinds, archive limits/ranking/rejections, download-quota mapping, DEV-551 hard pickup prerequisite, DEV-549-preferred fixture placement with Conductor confirmation fallback, title-and-year year-mapping pickup stop, verification evidence, closing bar / frozen scope / round cap are settled in `brief.md` Q1–Q7. Step 3.3 settles VII amendment / owner checkbox, effective-disabled vs keyed-invalid behaviour, search/download 409 rules with read-only precedence, required caller-supplied caps, and vendor-neutral Core names (`brief.md:89–99`, `CONCLUSIONS.md` Step 3.3). No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour. Any gap Quill cannot close from brief/CONCLUSIONS goes to `DRAFTING_RECEIPT.md` as `needs decision:`.

## What the User Will See *(mandatory)*

Nothing visible: this ticket is backend provider/adapter work. Default plain `docker compose up` product UI is unchanged. No SPA controls, new pages, or browser-visible subtitle UI ship here.

Operator / API outcomes (not default-Compose UI chrome):

- **Where**: Existing subtitle search and download flows that already call `ISubtitleProvider`; per-provider state from DEV-551; setup docs / sample settings for `Subtitles:SubDl`.
- **What the operator or API caller sees or does**: With SubDL configured and enabled with a key and valid settings, movie subtitle search can return SubDL candidates; download yields SRT through the existing publication path; per-provider download quota appears when SubDL docs attribute a coherent download-response snapshot as download usage; with no key (or `Enabled=false`) the provider stays effectively disabled without error; with enabled keyed but invalid settings the host still starts, SubDL makes no outbound calls, and DEV-551 reports a redacted misconfigured state. Search returns 409 only when no provider has effective, valid settings. Download returns 409 when the chosen candidate's provider has missing or invalid settings, after read-only library precedence.
- **What it needs to show up**: Landed DEV-551 registry/quota/shared suite; SubDL options (HTTPS base URL, allowlists, required positive archive caps) plus a nonblank API key via approved credential channels; sanitized WireMock fixtures for CI; home-cluster key only for the delivery-PR live receipt; Gate 1 owner tick + merged VII amendment on `origin/main` before Build.

## Scope

**In scope** (`brief.md` Frozen scope / Approach and file scope / Task order; Q1–Q7; Step 3.3; ticket Wanted 1–7 and Done when at `recon-DEV-554:36–55`):

1. **Movie-only SubDL v2 adapter** under `Subtitles:SubDl` with `Enabled`, `ApiKey`, `BaseUrl`, search and download host allowlists, and required positive archive-cap settings. Core options/validator types use **vendor-neutral** names (picked: `ArchiveSubtitleProviderOptions` / `ArchiveSubtitleProviderOptionsValidator`); the vendor token appears on the Infrastructure adapter (`SubDlSubtitleProvider`) and in the `Subtitles:SubDl` configuration key only (VIII; Step 3.3). `Enabled=false` or a blank key → effectively disabled: no error, no host startup failure, no outbound calls. Enabled keyed settings that fail validation → host still starts, zero outbound calls, DEV-551 reports redacted misconfigured state; validate before any outbound use. Search kinds: IMDb ID, TMDb ID (with `type=movie`), and title and year as one kind. Search at `GET /api/v2/subtitles/search`; download at `GET /api/v2/subtitles/{nId}/download` in default ZIP mode. No `unpack=1`, no `format=file`, no TV, no legacy API approval under Q1/Q5.
2. **Multi-provider 409 and precedence** (Step 3.3): Search returns 409 only when no provider has effective, valid settings. SubDL off or misconfigured while another provider is valid is not a search 409 — return the valid providers' results and show SubDL's state. With valid SubDL and a blank OpenSubtitles key, search returns SubDL results. Download returns 409 when the chosen candidate's provider has missing or invalid settings; read-only library policy is checked before that configuration failure. Preserve other provider validation, `SaveEnabled`, existing-track serving, key-free languages route, and secret isolation.
3. **Credential and guarded transport**: Send `X-API-Key` only on an eligible initial request to the validated HTTPS `BaseUrl` origin. Disable automatic redirect following. Keep search on its validated origin. For ZIP redirects, resolve and validate each target as HTTPS and download-allowlisted before requesting it, with a finite hop limit and immediate rejection at the first invalid hop. Every redirected or separately hosted ZIP fetch is credential-free. Build download paths from a validated single-segment `nId` (honor the documented shape if pickup verifies one) and encode query values structurally. Request URIs / errors / logs / traces redact credentials and omit raw entry names, URLs, identifiers, and quota values from diagnostics (fixed failure reasons and structured safe fields only). No key value is committed.
4. **Shared bounded ZIP-to-SRT reader** (BCL `System.IO.Compression.ZipArchive`, concrete class beside Infrastructure adapters, no new package or layer). Limits are **caller-passed required values**; the reader supplies **no** fallback numeric defaults. SubDL options carry the three required positive caps; example and home config use 8 MiB compressed, 32 MiB total expanded, and 128 entries subject to owner approval on the Gate 1 checkbox (Step 3.3 supersedes Q3 only on where caps are set). Enforce the compressed-size cap by a **bounded actual-byte read before ZIP open**; a length header may only provide early rejection. Count every entry and actual expanded bytes including ignored extensions. Keep limits and counters **per invocation**; propagate cancellation through HTTP and compressed/expanded reads; dispose response, stream, and ZIP resources according to ownership on all exits. Reject malformed ZIP, cap breaches, missing `.srt`, and unsafe names (rooted, drive-qualified, or parent traversal with either separator) with distinct safe reasons (fixed tokens only). Never extract to disk. Rank `.srt` entries: exact normalized stem match to candidate release name, then shared distinct release-name tokens, then largest actual size, then ordinal entry name, then archive order. Selected bytes return to the existing `DownloadSubtitleCommandHandler` size check, `EnsureValidSrt`, and publication path.
5. **Download quota in DEV-551 state**: Phase B pickup MUST record **Q2 attribution evidence** and the **documented reset representation** (whether current SubDL docs attribute the download-response `X-RateLimit-Limit` / `X-RateLimit-Remaining` / `X-RateLimit-Reset` trio as download usage, and how Reset is represented). Only when that pickup evidence affirms attribution, map a coherent snapshot into per-provider download quota. Reject malformed, negative, overflowing, or incoherent values to **`Unknown` without throwing**. Use injected `TimeProvider` for any relative reset calculation. Search counters are search usage only and MUST NOT fill download quota. Absent pickup attribution, or absent/malformed/partial/ambiguous snapshots → **`Unknown` fallback**. No inferred midnight reset; no `/api/v2/me` call in DEV-554 (Q2; Step 3.3; `recon-DEV-554:110`).
6. **Tests and fixtures**: DEV-551 shared contract suite against sanitized recorded WireMock responses in the DEV-549 location when landed (else Conductor-confirmed location before writing fixtures; one fake framework). Standalone archive + FsCheck coverage under `tests/LamuFlix.UnitTests/ZipSubtitleArchiveReaderTests.cs`; WireMock/health/composition in integration. Credential-sentinel (after Phase 3 checkpoint), options, **explicit Step 3.3 409 implementation** cases (effective-disabled, keyed-invalid/misconfigured state, search/download 409), and **separate** normal-composition evidence (SRT publication / rejection through the handler path; download-quota → DEV-551 or `Unknown`) per brief Test and acceptance evidence. Cover guarded-transport cases (allowed multi-hop, disallowed hop with zero outbound to it, hop exhaustion, rejected invalid `nId`, title metacharacters), over-cap actual data without a reliable length header, concurrent independent calls, cancellation/disposal on rejection, and absence of untrusted marker text from reasons/logs/traces/errors.
7. **Setup documentation**: README / `docker/api.settings.example.json` / `SubtitleSetupDocumentationTests` list SubDL settings (including the three required caps at the owner-approved example values), off-without-key behaviour, keyed-invalid behaviour, and approved credential channels; example key empty.
8. **Ship checkpoint then home-cluster receipt**: Gauge finishes size-M gates on the rebased delivery head **before opening the delivery PR**; then T016 attaches a real movie search and one successful download (movie, languages, candidate count, scrubbed credentials) on that PR. CI uses WireMock. Mutation share/span failures require correcting deficient tests while retaining configured thresholds.
9. **Spec-PR governance**: Spec Kit PR includes the narrow VII amendment draft (typed options entry; singular 409 clause replaced by provider-set/chosen-provider rules; checklist update; DEV-501 / ADR 0041 Sync Impact Report; version bump) and the unchecked owner checkbox above. Build (T001+) does not start until T000 evidence passes. If the owner rejects, revert the amendment on the spec branch and return DEV-554 to the owner to re-scope under §2.3(a); no workaround.

**Out of scope**:

- TV shows.
- Formats other than SRT inside the archive (unless a later ruling finds SRT rare — not claimed here).
- YIFY integration (DEV-556 consumes the shared reader later).
- New public routes, DTO fields, or status codes beyond the Step 3.3 multi-provider 409 semantics on existing search/download surfaces; schema/migrations; web UI; new NuGet/npm dependency; new project/top-level folder/architectural layer; `Features:LocalPlay`; `Process.Start`; hardcoding secrets.
- `/api/v2/me` polling or bootstrap; inferred quota reset schedule; `unpack=1` / `format=file`; legacy SubDL API under this ticket.
- Claiming the VII amendment is already merged or approved before the owner ticks the checkbox.
- Anything else is a follow-up issue, not a finding in this round (`brief.md` Frozen scope / Q7).

**Precedence**: Ticket Wanted / Done when / Where it lands (`recon-DEV-554`) are approved authority (`PRODUCT.md` §3). Brief Q1–Q7, Step 3.3, and `CONCLUSIONS.md` govern plan boundaries. Quill invents no decisions.

**Dependencies**: DEV-551 is a hard Phase B pickup prerequisite (registry, per-provider quota state, shared contract suite). DEV-549 placement is preferred for fixtures; if unavailable at fixture time, confirm location with the Conductor before writing fixtures (Q4). Gate 1 owner tick + amended VII on `origin/main` are Build prerequisites (Step 3.3 / T000).

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**SubDL v2 provider**
Movie-only subtitle acquisition adapter using SubDL’s current v2 search and ZIP download endpoints behind the existing `ISubtitleProvider` seam.
_Avoid_: legacy query-string API as the approved transport; TV search kinds; unpack/file download modes

**Title-and-year search kind**
One search kind that carries both movie title and year; year must participate via a documented v2 request filter or documented result-year filter before candidates are returned.
_Avoid_: silently dropping year; returning a conflicting year; inventing a legacy endpoint when v2 documents neither mapping

**Shared archive reader**
Concrete BCL ZIP reader that returns selected SRT bytes under caller-passed size/entry caps without writing to disk and without inventing numeric defaults.
_Avoid_: third-party archive packages; treating entry names as filesystem paths; extracting to disk; baking SubDL cap numbers into the shared reader

**Effective-disabled SubDL**
`Enabled=false` or blank API key: provider off without error, without host startup failure, and with zero outbound calls.
_Avoid_: treating blank key as a host-failing configuration error; conflating with keyed-invalid

**Keyed-invalid / misconfigured SubDL**
Enabled with a nonblank key but failing validation (non-HTTPS base URL, missing host, missing or non-positive cap, etc.): host starts, zero outbound calls, DEV-551 reports redacted misconfigured state.
_Avoid_: failing host startup for SubDL-only invalid settings; leaking the key in that state

**Download quota snapshot**
Coherent `X-RateLimit-Limit` / `Remaining` / `Reset` values from a download response that SubDL docs identify as download usage, mapped into DEV-551 per-provider state; otherwise `Unknown`.
_Avoid_: filling download quota from search counters; inferred midnight UTC reset; `/me` bootstrap in this ticket; mapping headers without documented download-usage attribution

Consumed vocabulary (already in use; not redefined): `ISubtitleProvider`, `SubtitleQuota` / `Unknown`, `DownloadSubtitleCommandHandler` SRT validation seam, WireMock recorded fakes, DEV-551 provider registry / shared contract suite, DEV-549 fake placement.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Shared ZIP archive reader returns safe SRT bytes (Priority: P1) 🎯 MVP

Operators and later adapters (SubDL now; YIFY later) need one bounded ZIP-to-SRT reader that rejects bombs, traversal names, and missing SRT with clear safe reasons, then hands selected SRT bytes to the existing validation seam.

**Why this priority**: Without the reader, SubDL ZIP downloads cannot meet ticket Wanted 4 / Done when bullet 2, and YIFY cannot reuse the seam.

**Independent Test**: Focused unit tests in `tests/LamuFlix.UnitTests/ZipSubtitleArchiveReaderTests.cs` cover compressed/expanded/entry caps (including zip-bomb, too-many-entries, and over-cap actual data without a reliable length header), `../x.srt` and other unsafe path forms, no SRT, malformed ZIP, multi-entry ranking, invalid selected SRT rejected by existing checks, cancellation/disposal on rejection, and absence of untrusted marker text from reasons; distinct safe failure reasons (fixed tokens); FsCheck for order-independent selection when ranks decide and for limit boundaries. Archive behaviour is provable without SubDL live calls or DEV-551 composition.

**Acceptance Scenarios**:

1. **Given** a ZIP within caps containing one safe `.srt`, **When** the reader runs with the candidate release name, **Then** it returns those SRT bytes without writing to disk.
2. **Given** a ZIP that breaches compressed, expanded, or entry caps, **When** the reader runs, **Then** it rejects with a distinct safe reason for that breach.
3. **Given** a ZIP containing an unsafe entry name such as `../x.srt` (or rooted / drive-qualified / either-separator traversal), **When** the reader runs, **Then** the whole archive is rejected with a distinct safe reason.
4. **Given** multiple `.srt` entries, **When** the reader ranks them, **Then** selection follows exact normalized stem match, then shared distinct tokens, then largest actual size, then ordinal entry name, then archive order.

---

### User Story 2 - SubDL searches movies and downloads ZIP through the provider seam (Priority: P1)

With SubDL enabled and keyed with valid settings, subtitle search and download use the three movie search kinds, preserve year for title-and-year, download ZIP from allowed hosts without credentials on redirected fetches, and publish through the existing SRT handler path. Multi-provider 409 semantics follow Step 3.3.

**Why this priority**: This is the ticket’s primary provider deliverable (Wanted 1–2, Done when 1).

**Independent Test**: DEV-551 shared contract suite against sanitized WireMock fixtures covers IMDb ID, TMDb ID with movie type, title plus year (including same-title/different-year), ZIP download success, enablement / blank-key off, keyed-invalid misconfigured state, and Step 3.3 search/download 409 cases. Registration **implements** multi-provider 409 on the landed DEV-551 seams (pickup-cited) without new public API shape. **Separate** composition evidence (after 409 wiring) asserts valid selected SRT reaches handler publication, invalid selected SRT is rejected on that same handler path, and derived download quota reaches DEV-551 per-provider state only when pickup attribution evidence allows mapping (else `Unknown`).

**Acceptance Scenarios**:

1. **Given** SubDL enabled with a nonblank key and valid HTTPS options including required positive caps, **When** search runs for each of the three kinds, **Then** candidates are returned from the v2 search contract (TMDb includes `type=movie`).
2. **Given** two movies sharing a title but different years, **When** title-and-year search requests one year, **Then** only matching-year candidates are returned (year mapping verified at pickup per Q5; stop and leave transport locked pending recon/ruling if v2 documents neither mapping).
3. **Given** a selected SubDL candidate, **When** download runs through normal composition, **Then** the adapter fetches ZIP mode with automatic redirects disabled, validates each hop as HTTPS and download-allowlisted before requesting it (finite hop limit; reject at first invalid hop), omits the API key on every redirected/separately hosted fetch, the archive reader selects SRT bytes after a bounded actual-byte read, and the existing handler validates and publishes.
4. **Given** selected SRT bytes that fail the existing handler checks, **When** download runs through that same handler path, **Then** the payload is rejected and not published.
5. **Given** `Enabled=true` with a blank key (or `Enabled=false`), **When** acquisition would use SubDL, **Then** the provider is effectively disabled, makes no outbound call, and does not fail host startup.
6. **Given** SubDL enabled with a key but invalid settings (for example missing/non-positive cap or non-HTTPS base URL), **When** the host starts and search/download would use SubDL, **Then** startup succeeds, SubDL makes zero outbound calls, and DEV-551 reports redacted misconfigured state.
7. **Given** valid SubDL settings and a blank OpenSubtitles key, **When** search runs, **Then** search returns SubDL results (not 409 solely because OpenSubtitles is blank).
8. **Given** SubDL disabled or misconfigured while another provider has effective valid settings, **When** search runs, **Then** search returns the valid providers' results, shows SubDL's state, and does not return 409.
9. **Given** no provider has effective valid settings, **When** search runs, **Then** search returns 409.
10. **Given** a chosen download candidate whose provider has missing or invalid settings, **When** download runs after read-only library precedence is applied, **Then** download returns 409.

---

### User Story 3 - Credentials stay out of diagnostics and git; download quota surfaces when documented (Priority: P1)

API keys never appear in logs, traces, errors, or committed files. Documented coherent download-response rate-limit headers populate DEV-551 download quota only when docs attribute them as download usage; otherwise quota is `Unknown`.

**Why this priority**: Ticket Wanted 3 and 5 and Done when bullet 3 are safety and state requirements that block merge if breached at any severity (`brief.md` Closing bar).

**Independent Test**: Sentinel-key tests (after Phase 3 checkpoint) capture all log levels, traces, and surfaced errors including upstream failure and redirected download; assert no leak and no credential on **any followed hop**. Contract cases cover known and unknown download quota (known only with **pickup-recorded** download-usage attribution and documented reset representation; otherwise `Unknown` fallback), malformed/negative/overflowing/incoherent header values → `Unknown` without throwing, and keep search counters separate with zero `/api/v2/me` calls.

**Acceptance Scenarios**:

1. **Given** a sentinel API key and a failing or redirected SubDL exchange, **When** diagnostics are captured at every log level and on traces/errors, **Then** the sentinel appears in no line.
2. **Given** a download response with a coherent rate-limit snapshot that docs attribute as download usage, **When** quota is mapped, **Then** DEV-551 per-provider download quota reflects that snapshot.
3. **Given** absent, partial, malformed, or ambiguous rate-limit headers on download, or missing documented download-usage attribution for the trio, **When** quota is mapped, **Then** download quota is `Unknown` with no inferred reset and no `/me` call.
4. **Given** setup docs and sample settings, **When** documentation tests run, **Then** SubDL settings (including required caps at approved example values), credential channels, effective-disabled and keyed-invalid behaviour are documented and no key value is committed.

---

### User Story 4 - Ship checkpoint before delivery PR; live receipt on the PR (Priority: P2)

The rebased delivery head clears the size-M ship gate matrix before the delivery PR; that PR then carries a real home-cluster movie search and one successful SubDL download with scrubbed credentials.

**Why this priority**: Ticket Done when bullet 4; `task-pipeline` Phase 5 steps 1–2; CI remains WireMock (Q6).

**Independent Test**: Gauge finishes the size-M matrix on the rebased delivery head and records actual verdicts **before opening the delivery PR**; T016 then attaches the live receipt on that PR (movie, languages, candidate count, successful download, credentials scrubbed). Not a CI green path.

**Acceptance Scenarios**:

1. **Given** review closed and the delivery branch rebased, **When** Gauge re-runs the size-M matrix on that head, **Then** actual verdicts are recorded and T017 finishes **before** the delivery PR is opened.
2. **Given** that ship checkpoint and a configured home-cluster SubDL key, **When** one movie is searched and one candidate downloaded, **Then** the delivery PR records movie, languages, candidate count, and success with credentials removed.

---

### Edge Cases

- ZIP bomb / oversized compressed payload before accept; expanded-byte and entry-count breaches while reading, including ignored extensions.
- Unsafe entry names anywhere in the archive (rooted, drive-qualified, `..` with `/` or `\`).
- No `.srt` entry; malformed ZIP; selected SRT failing existing handler checks on the normal publication path.
- Multiple `.srt` entries with ties across ranking tiers.
- Title-and-year when v2 documents neither year request filter nor result-year field → stop transport work; leave transport locked pending recon and Patron ruling (Q5); do not weaken the ticket; do not treat the stop as a green year-mapping pass for T002+.
- Redirected archive download: disable auto-follow; validate each hop as HTTPS + download-allowlisted before requesting; reject at first invalid hop or hop exhaustion with zero outbound to a disallowed target; every redirected/separate-host fetch carries no credential.
- Untrusted `nId` (non-single-segment / non-documented shape) or title query metacharacters → reject or structurally encode; never shift the validated origin path/query.
- Over-cap actual compressed bytes without a reliable length header → distinct compressed-cap rejection before ZIP open; length header may only early-reject.
- Concurrent independent reader/adapter invocations must not share mutable limit counters; cancellation and disposal apply on rejection exits; untrusted entry names/URLs/identifiers/quota values must not appear in reasons, logs, traces, or errors.
- Search-response rate-limit headers must not populate download quota.
- Download-response header trio without **pickup-recorded** documented download-usage attribution and reset representation → `Unknown` fallback (do not map; do not invent attribution); malformed/negative/overflowing/incoherent values → `Unknown` without throwing.
- Enabled with blank key → effectively disabled; enabled keyed-invalid → misconfigured state, not host failure.
- Search 409 only when every provider lacks effective valid settings; download 409 for the chosen provider's missing/invalid settings after read-only precedence.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST bind `Subtitles:SubDl` through vendor-neutral Core options/validator types (`ArchiveSubtitleProviderOptions` / `ArchiveSubtitleProviderOptionsValidator`) with enablement, API key, HTTPS base URL, search/download host allowlists, and required positive archive-cap settings; `Enabled=false` or blank key MUST be effectively disabled (no error, no startup failure, no outbound calls); enabled keyed-invalid settings MUST allow host startup, make zero outbound calls, and report redacted misconfigured state via DEV-551; settings MUST be validated before any outbound use.
- **FR-002**: System MUST declare exactly three SubDL search kinds: IMDb ID, TMDb ID (with movie type), and title and year as one kind; year MUST participate and MUST NOT return a conflicting year (pickup verifies documented v2 mapping; if neither mapping exists, transport stays locked pending recon and Patron ruling).
- **FR-003**: System MUST search via `GET /api/v2/subtitles/search` on the validated HTTPS origin and download via `GET /api/v2/subtitles/{nId}/download` in default ZIP mode; MUST build download paths from a validated single-segment `nId` (honor the documented shape if pickup verifies one) and encode query values structurally; MUST NOT use `unpack=1`, `format=file`, TV kinds, or the legacy API under this ticket.
- **FR-004**: System MUST disable automatic redirect following; MUST send `X-API-Key` only on an eligible initial request to the validated HTTPS `BaseUrl` origin; for ZIP redirects MUST resolve and validate each target as HTTPS and download-allowlisted before requesting it, with a finite hop limit and immediate rejection at the first invalid hop; MUST omit credentials on every redirected or separately hosted archive fetch.
- **FR-005**: System MUST redact credentials from request URIs, errors, logs, and traces; MUST use fixed failure reasons and structured safe fields only (omit raw entry names, URLs, identifiers, and quota values from diagnostics); and MUST NOT commit any key value.
- **FR-006**: System MUST provide a shared BCL `ZipArchive` reader beside Infrastructure adapters that enforces caller-passed compressed, expanded, and entry caps via a **bounded actual-byte read before ZIP open** (a length header may only early-reject; no reader-side numeric defaults; SubDL required settings use owner-approved example/home values 8 MiB / 32 MiB / 128), keeps limits/counters per invocation, propagates cancellation, disposes response/stream/ZIP resources on all exits, never extracts to disk, treats entry names as data, rejects unsafe names and missing SRT with distinct safe reasons, and ranks multiple `.srt` entries per Q3 ranking.
- **FR-007**: System MUST return selected SRT bytes through the existing `DownloadSubtitleCommandHandler` nonempty/size check, `EnsureValidSrt`, and publication path without changing public API shape; normal composition MUST publish valid selected SRT and MUST reject invalid selected SRT on that same handler path.
- **FR-008**: Phase B pickup MUST record Q2 attribution evidence and the documented reset representation for whether docs attribute the download-response `X-RateLimit-*` trio as download usage. System MUST map coherent snapshots into DEV-551 per-provider download quota only when that pickup evidence affirms attribution; MUST reject malformed, negative, overflowing, or incoherent values to **`Unknown` without throwing**; MUST use injected `TimeProvider` for any relative reset calculation; otherwise **`Unknown` fallback**; MUST NOT infer reset schedule or call `/api/v2/me` in DEV-554; search counters MUST NOT fill download quota.
- **FR-009**: System MUST run the DEV-551 shared contract suite against sanitized recorded WireMock fixtures placed per Q4, covering the three search kinds, year disambiguation, ZIP download, known/unknown download quota, enablement/blank-key, keyed-invalid/misconfigured state, and Step 3.3 search/download 409 cases.
- **FR-010**: System MUST cover archive safety; credential-sentinel leakage after Phase 3 checkpoint (all levels + traces/errors, including upstream failure and redirected download); options; **explicit 409 implementation** and provider-state cases; and **separate** normal-composition handler publication/rejection plus download-quota → DEV-551 or `Unknown`; MUST use existing FsCheck for ranking/limit invariants.
- **FR-011**: System MUST document SubDL settings (including required caps at approved example values), effective-disabled and keyed-invalid behaviour, and approved credential channels in README and sample settings, covered by `SubtitleSetupDocumentationTests`, with empty example key.
- **FR-012**: After the rebased-head ship checkpoint, the delivery PR MUST include a scrubbed home-cluster real search + one successful download receipt (movie, languages, candidate count, success).
- **FR-013**: System MUST register SubDL through the landed DEV-551 provider registry/composition with only the minimum touch required and MUST **implement** Step 3.3 multi-provider 409 semantics on the pickup-cited existing search/download seams; no new routes, DTO fields, or status codes beyond those semantics.
- **FR-014**: Search MUST return 409 only when no provider has effective, valid settings; download MUST return 409 when the chosen candidate's provider has missing or invalid settings after read-only library precedence; other provider validation, `SaveEnabled`, existing-track serving, languages route, and secret isolation MUST be preserved. 409 implementation evidence is distinct from normal-composition SRT/quota evidence.
- **FR-015**: Spec Kit PR MUST present the narrow VII amendment that adds the typed `Subtitles:SubDl` options entry (:782-784), **replaces** the singular-provider 409 clause (:795-801) with Step 3.3 provider-set search and chosen-provider download rules, updates the VII checklist (:946-948), carries DEV-501 / ADR 0041 impact in the Sync Impact Report with the version bump, and includes the unchecked owner checkbox; OpenSubtitles binding and validation, `SaveEnabled`, read-only precedence, existing-track serving, languages, and secret isolation MUST be preserved while search aggregation changes for blank OpenSubtitles with valid SubDL; Build MUST NOT start until T000 verifies the owner tick, amended VII + version on `origin/main`, landed DEV-551 seam/state, and owner-approved cap values; owner rejection MUST revert the amendment and return the ticket for §2.3(a) re-scope (no workaround).

### Key Entities

- **Archive subtitle provider options**: Vendor-neutral Core record/validator binding `Subtitles:SubDl` enablement, API key, HTTPS base URL, search/download allowlists, and required positive archive caps.
- **Subtitle candidate / download**: Existing provider-seam types; SubDL supplies ZIP that the archive reader turns into SRT bytes.
- **Archive selection result**: Selected SRT bytes or a distinct safe rejection reason.
- **Per-provider download quota / state**: DEV-551 state fed from coherent attributed download-response snapshots or `Unknown`, plus effective-disabled and redacted misconfigured reporting.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: DEV-551 shared contract suite passes for SubDL against sanitized WireMock fixtures for all three search kinds, year disambiguation, ZIP download, and known/unknown download quota.
- **SC-002**: Archive tests reject zip-bomb, too-many-entries, unsafe `../x.srt` (and peer unsafe forms), and no-`.srt` archives with distinct safe reasons; ranking and existing SRT rejection are covered.
- **SC-003**: A sentinel API key appears in no log line at any level, and in no captured trace or surfaced error, including upstream failure and redirected download cases.
- **SC-004**: Effective-disabled (`Enabled=false` or blank key) leaves SubDL off without startup failure; keyed-invalid reports redacted misconfigured state; setup docs list settings (including required caps) and keep the example key empty.
- **SC-005**: After the rebased-head ship checkpoint, the delivery PR carries a scrubbed home-cluster receipt with movie, languages, candidate count, and one successful download.
- **SC-006**: Size-M backend gates and review close under the brief closing bar (Critical/High with concrete scenario, or any demonstrated acceptance/credential/archive safety violation); web gates `SKIPPED (scope-empty)` when no web files change; mutation reports its actual harness verdict; Gauge finishes the size-M matrix on the rebased delivery head with actual verdicts **before opening the delivery PR** (`task-pipeline` Phase 5 steps 1–2).
- **SC-007**: Explicit 409-implementation cases prove Step 3.3 search/download 409 behaviour and DEV-551 provider-state reporting for effective-disabled and keyed-invalid SubDL; **separate** composition evidence proves valid SRT publication, invalid SRT rejection on the handler path, and attributed download quota reaching per-provider state (or `Unknown` when pickup attribution is absent).

## Assumptions

- DEV-551 lands with a usable provider registry, per-provider quota state, and shared contract suite before Phase B production edits (hard prerequisite; Q4; T000 evidence).
- DEV-549 fake placement is preferred; if absent at fixture time, Conductor confirms the single fixture location before fixtures are written (Q4).
- Current v2 title-and-year mapping is verified at pickup against docs and sanitized fixtures; a **verified documented** mapping is a required passing condition for the normal T002+ sequence; if neither documented request-year nor result-year mapping exists, transport stays locked pending recon and Patron ruling rather than inventing behaviour (Q5) — a documented stop is not a green year-mapping pass.
- Q2 download-quota attribution is verified at pickup against current SubDL docs (`recon-DEV-554:110`); mapping is allowed only with recorded attribution evidence; otherwise `Unknown` fallback — Quill invents no attribution.
- Spec-PR VII amendment is required governance work; it is not presumed applied or approved until the owner ticks the checkbox and merges (Step 3.3).
- No `ASSUMPTIONS.md` taste tags were required for this grill; Quill adds none.
