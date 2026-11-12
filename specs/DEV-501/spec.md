# Feature Specification: Explicit subtitle search and download

**Feature Branch**: `feature/501-spec`
**Created**: 2026-11-12
**Status**: gate1: provisional — PLAN FROZEN; Phase 2 challenge round 1 closed by Keel (see PLAN_ADJUDICATION.md); zero owner structural checkboxes; awaiting owner Gate 1 PR approval; runtime receipts OPEN.
**Input**: Ticket DEV-501 (parent DEV-100); grill-closed `brief.md` (Q1–Q9 + D1–D5 + Principle V clarification); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Palette `design.md`; Keel-authored `docs/adr/0041-explicit-subtitle-acquisition.md` (Proposed); Maestri notes `DEV-501`, `recon-DEV-501` (incl. 111–130); `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only). Dependencies: Player CC panel ticket; Subs folder discovery ticket; home-cluster ticket for runtime receipts.

## Clarifications

### Plan-challenge Patron rulings (operative)

- **Catalog (Compass F1/F2)**: `GET /api/subtitles/languages` returns 200 `{defaultLanguage, languages:[{code, englishName, nativeName}]}` via `SubtitleLanguagesResponse` / `SubtitleLanguageOptionDto`. Single Core catalog `SupportedSubtitleLanguages` is authoritative for search codes and configured preferences. Preferences first (configured order, deduplicated), then remaining supported entries by ordinal English name then code. Default is first preference or null. Empty preferences are valid, yield null default and no initial search, and do not produce acquisition 409; an explicit supported-language selection may search with otherwise valid provider settings. Catalog uses `Cache-Control: private, no-store`, makes no provider call, and requires no provider key. Parameterless catalog is unaffected by FluentValidation 422 unless a validator applies.
- **Principle V (`CONCLUSIONS.md` Cross-Artifact Alignment)**: Preserve constitution Principle V unchanged. Actual FluentValidation `ValidationException` from the validation decorator → **422** `invalid_request` through the single centralized `IExceptionHandler` with `AddProblemDetails()` and `traceId` on every ProblemDetails. Malformed HTTP/route/query/JSON binding and domain/token prechecks outside `ValidationException` → **400**. No global remap of `ValidationException`; no raw endpoint exception text. No new structural checkbox.
- **D1–D5**: Operative directions in `brief.md` Plan-challenge decisions; full bases in `CONCLUSIONS.md`. Keel's final consistency analysis and plan freeze are recorded in `PLAN_ADJUDICATION.md`; they grant no implementation or owner Gate 1 approval.

### Session 2026-11-12

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Product decisions for drafting were settled in Patron Q1–Q9. Subsequent plan-challenge catalog, D1–D5, and Principle V rulings further refine contracts. Phase B verifies CC panel and Subs discovery dependency readiness and pickup drift against main before implementation — ordering prerequisites, not open product decisions. Gate 1 and plan freeze remain separate.

## Scope

**In scope** (`brief.md` frozen scope; ticket Scope via `recon-DEV-501` lines 9–77; owner rulings 5–7; D1–D5):

1. **Language catalog + search control**: bilingual “Search by language” from server catalog; default to first configured preferred language; empty preferences → choose-language prompt, no auto-search; canonical code normalization (trim, `OrdinalIgnoreCase`, emit catalog `Code`).
2. **Ranked remote candidates**: truncated release name (full on hover/focus), match percent, download count; sort match% ↓, downloadCount ↓, ordinal releaseName, candidateId; moviehash 100; otherwise distinct-token Jaccard with `MidpointRounding.AwayFromZero`.
3. **Local tracks above results**: Off and discovered local tracks (including `Subs\`) usable in every acquisition state; **active check only on the actually active downloaded local track** (D5) — never on candidate rows; no release-name/file-name heuristic matching or candidate-to-track persistence.
4. **Explicit download only**: client sends only `{candidateId}`; never auto-download; never automatic paid retry; serialize UI Get while in flight.
5. **Public APIs**: `GET /api/subtitles/languages`; `GET /api/movies/{id}/subtitles/search?language=`; `POST /api/movies/{id}/subtitles/download`; always-present `quota` object with nullable fields; `Cache-Control: private, no-store` on catalog and acquisition success **and** mapped error responses; reuse `SubtitleTrackDto`.
6. **Bounded candidate binding**: concrete process-local Singleton `SubtitleCandidateBinder` (no binder interface/port); opaque 32-hex tokens from injected seeded `Random` + `TimeProvider` (not cryptographically secure claims); TTL/cap typed options; earliest-expiry FIFO eviction; stale before paid call.
7. **Guarded Subs publication**: streaming 2 MiB body cap; SRT validate before stage; one leading-dot staging file reused across next-free `(N)` attempts; containment before create, after create/before stage, and before each publication; `MaxSubtitlePublicationAttempts` default 10_000; cleanup owned staging on cancel/finally; never overwrite/delete published library files.
8. **Quota and read-only honesty**: provider-observed quota only; Retry-After/relative reset via `timeProvider.GetUtcNow().Add(duration)`; known-zero download quota → POST 429 before paid call; distinct GET search-rate-limit 429 vs POST download-quota 429; caller cancellation ≠ provider timeout.
9. **Prepare-only removal**: D4 delete/update/retain table; verify current sources before deletion; preserve dirty harness; sequential shared edits then final generated contracts.
10. **Provider seam**: existing `ISubtitleProvider`; OpenSubtitles only this ticket.
11. **Contract chain**: C# DTO → OpenAPI → generated TypeScript.
12. **Constitution / ADR**: post-Gate-1 Principle VII `1.15.0` → `1.16.0` with ADR 0041 acceptance together; ADR 0038 remains Proposed with prepare-route wording-only relationship; no Principle I port-list change (no new binder port).
13. **Tests, gates, runtime**: unit/property/provider/filesystem/API/UI; mandatory applicable property gate for scorer/ranking; honest gate labels; runtime OPEN.

**Out of scope**: SubDL; new NuGet/npm dependency; player library swap; new architectural layer; database schema; server delay/style persistence; `Features:LocalPlay` / `Process.Start`; client re-ranking; non-SRT conversion; automatic paid retries; inventing Gate 1 / plan freeze / runtime PASS; anything outside `brief.md` frozen scope (follow-up, not this round).

**Precedence**: Ticket text; brief Q1–Q9 + D1–D5 + Principle V; `CONCLUSIONS.md`; `ASSUMPTIONS.md`; ADR 0041; Palette `design.md`. Quill invents no decisions.

## Domain vocabulary *(mandatory)*

Qualified glossary names for Principle VIII `CONTEXT.md` (additive; do not rewrite Movie / Library / exact-basename sidecar serving):

**Subtitle Candidate**
A remote search result; never an installed local track merely because it appears in search.

**Candidate ID**
Opaque process-local selection token bound server-side to movie, language, and provider candidate; not a path, URL, or raw provider file ID. Issued as 32-hex / 128-bit under lock; not claimed cryptographically secure.

**Local Subtitle Track**
A discovered subtitle, including a successfully committed downloaded SRT in Subs; selectable independently of acquisition availability.

**Subtitle Match Percent**
Moviehash 100, otherwise rounded distinct-token overlap; no extra confidence claim.

**Subtitle Download Availability**
Server-reported acquisition write capability/reason, separate from free search and existing-track playback.

Consumed identifiers (unchanged): **SubtitleTrackDto**, **ISubtitleProvider**, **Subtitles:SaveEnabled**, **Subtitles:PreferredLanguages**, **Features:LocalPlay**, **MovieId**, **SupportedSubtitleLanguages**, **SubtitleCandidateBinder**, **SubtitleMatchScorer**.

## What the User Will See *(mandatory)*

- **Where**: Movie playback → CC → Subtitles tab (`design.md`; extends `SubtitleSelector`).
- **What**: Bilingual language control from catalog; quota/reset when provider supplies them; local tracks and Off above ranked remote results; Get on one candidate; active check only on active local track; inline reasons for empty/quota/timeout/failure/stale/read-only/search-rate-limit — without stopping video.
- **Needs**: CC panel + Subs discovery dependencies; authorized movie; valid OpenSubtitles settings for search; writable policy + non-exhausted known quota for download; empty preferences still allow explicit language pick.

### Smoke Test

- **SMOKE-001**: Writable library, acquisition configured, preferred language includes pt-BR (or explicit pick when empty preferences) → ranked remote results under local tracks; video keeps playing.
- **SMOKE-002**: Get on one candidate → Subs BS Player layout file; track listed locally with active check; captions load; video keeps playing.
- **SMOKE-003**: Read-only, download-quota exhausted, search rate-limited, or provider timeout → inline reason; Get disabled or manually retryable as specified; local tracks/Off usable; video keeps playing.

Home-cluster / BS Player receipts remain OPEN runtime acceptance, not Compose smoke substitutes.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Search by language with ranked candidates (Priority: P1)

Viewer opens Subtitles tab; catalog-backed bilingual control defaults to first preferred language (or choose-language when none); free ranked remote candidates; local tracks/Off above results; language change triggers new free search, discards stale responses, preserves playback/active track.

**Why this priority**: Primary entry path (recon:12–25).

**Independent Test**: Catalog + search API; scorer/ranking unit and property tests; UI ordering/labels/stale discard without download.

**Acceptance Scenarios**:

1. **Given** preferred languages including pt-BR and valid provider key, **When** `GET .../search?language=` runs, **Then** 200 with sorted candidates, always-present quota (nullable fields), downloadAvailability, private no-store.
2. **Given** empty PreferredLanguages, **When** tab opens, **Then** choose-language prompt and no search until pick; catalog `defaultLanguage` is null; empty preferences alone do not 409 acquisition.
3. **Given** in-flight search A, **When** viewer switches to B, **Then** A’s late response is ignored; B replaces remote list; active local track/playback unchanged.
4. **Given** zero remote matches, **When** search completes, **Then** “No subtitles found for this language.”; local tracks/Off usable.
5. **Given** `GET /api/subtitles/languages`, **When** called, **Then** 200 ordered catalog DTO, private no-store, no provider call/key.

---

### User Story 2 - Explicit download into Subs and activate track (Priority: P1)

Viewer selects one candidate. Server runs POST prechecks (movie → token → read-only → provider config → containment) before any paid call; streams body with 2 MiB bound; validates SRT; stages once; publishes non-replacing with containment rechecks; activates only for matching movie/session.

**Why this priority**: Pick → play → BS Player file (recon:66–69).

**Independent Test**: Filesystem containment/collision/staging/exclusion; API 201 + full POST matrix including 400/422 split; UI serialized Get and session-safe activation.

**Acceptance Scenarios**:

1. **Given** fresh candidateId on writable library with unknown or positive last-known quota, **When** POST succeeds, **Then** 201 `{track, quota}` and Subs `<stem>-<canonicalCode>.srt` or next free `(N)`.
2. **Given** target exists, **When** publish races, **Then** next free `(N)` used; existing file unchanged; same staged payload reused.
3. **Given** stale/unknown/evicted/restart-lost/cross-movie candidateId, **When** POST, **Then** 400 `stale_candidate` before paid call.
4. **Given** FluentValidation failure on search/download request, **When** decorator throws `ValidationException`, **Then** 422 `invalid_request` via centralized handler with `traceId`.
5. **Given** known remainingDownloads == 0, **When** POST after other prechecks, **Then** 429 `download_quota_exhausted` before paid call; no elapsed-reset inference replenishes quota.
6. **Given** download completes after language switch, **When** 201 arrives, **Then** local track added without replacing current remote results; activation only if movie/session matches.

---

### User Story 3 - Quota, timeout, and read-only states without stopping video (Priority: P2)

Across download-quota exhaustion, search rate-limit, provider timeout/failure, missing/invalid acquisition configuration, read-only policy, and caller cancellation, video and local-track switching continue. Quota never guessed. Read-only disables Get in place. Configuration failures must not break host startup or local serving.

**Why this priority**: Ticket AC (recon:70).

**Independent Test**: Provider fixtures; API read-only GET + POST 409 order; distinct 429 copy; cancellation≠timeout proofs; UI continuity.

**Acceptance Scenarios**:

1. **Given** known exhausted download quota, **Then** POST 429 `download_quota_exhausted` with optional quota extension; UI “Download quota reached. Resets at {localTime}.” or “Download quota reached.”; Get disabled; video continues.
2. **Given** free-search provider rate limit, **Then** GET 429 `search_rate_limited`; UI “Subtitle search is temporarily rate-limited. Try again in a moment.”; does not mark download quota exhausted.
3. **Given** SaveEnabled false with valid provider settings, **Then** GET 200 with `downloadAvailability:{enabled:false, reason:"read_only_library"}`; POST 409 `read_only_library` before config 409 and before paid call.
4. **Given** missing/invalid provider configuration, **Then** GET/POST 409 `provider_not_configured` when required for execution; empty preferred languages alone are not that failure.
5. **Given** unknown quota fields, **Then** UI shows unavailable — never inferred zero/reset.
6. **Given** caller cancellation during search/body read/staging/publication, **Then** owned staging cleaned; never mapped as 504 `provider_timeout` or guessed quota consumption.

---

### User Story 4 - Retire prepare-only acquisition surface (Priority: P2)

Prepare route/flow gone (404). Shared client, moviehash, local serving, atomic store, typed options, secrets, SaveEnabled retained. D4 table + current-source verification before deletion. Post-Gate-1 constitution 1.15.0→1.16.0 and ADR 0041 acceptance; ADR 0038 Proposed prepare-wording relationship only.

**Why this priority**: Owner ruling 5 / AC (recon:39,61,71).

**Independent Test**: Prepare 404; verified live-reference sweep; SaveEnabled still drives read-only; harness dirty edit preserved.

**Acceptance Scenarios**:

1. **Given** shipped API, **When** `POST .../prepare`, **Then** 404.
2. **Given** D4 delete list, **When** ticket lands, **Then** those prepare-only artifacts are removed only after current-checkout verification; shared mechanisms retained.
3. **Given** read-only after removal, **Then** SaveEnabled still governs write policy for new routes.

### Edge Cases

- Escape/symlink/reparse outside Subs refused before directory creation, after create/before stage, and before each publication attempt (403 `access_denied`).
- Missing Subs created; publication stays inside Subs.
- Concurrent `(N)` race: retry next slot; never overwrite/delete competitor; stop at configured publication-attempt ceiling → 409 `read_only_library` + owned staging cleanup.
- Partial staging failure / cancellation: owned staging cleaned; no discoverable partial track.
- Process restart / TTL eviction / earliest-expiry FIFO: prior candidateId → 400 `stale_candidate` without paid call; process-local only (no replica affinity claim).
- Oversized (>2 MiB streaming) or invalid/non-SRT payload → 502 `provider_unavailable` before staging; no invented quota charge/refund.
- Permission race after pre-check → safe 403/409; no pretended refund.
- Acquisition config missing/invalid: host starts; local list/content/playback work.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST expose free `GET /api/movies/{id}/subtitles/search?language=` returning 200 `{candidates[{candidateId, language, releaseName, matchPercent, downloadCount}], quota{remainingDownloads, resetAtUtc}, downloadAvailability{enabled, reason}}` with nullable quota fields when unknown, never guessed, and `Cache-Control: private, no-store` on success and mapped errors (Q3/D1).
- **FR-002**: System MUST expose `POST /api/movies/{id}/subtitles/download` accepting only `{candidateId}` and returning 201 `{track, quota}` reusing `SubtitleTrackDto`, with the same no-store policy (Q3).
- **FR-003**: System MUST map acquisition errors via the centralized `IExceptionHandler` + `AddProblemDetails()` with `traceId` on every ProblemDetails and D1 `extensions["reason"]` plus optional `extensions["quota"]` when known. Approved reasons: `invalid_request`, `stale_candidate`, `access_denied`, `movie_not_found`, `read_only_library`, `provider_not_configured`, `search_rate_limited`, `download_quota_exhausted`, `provider_unavailable`, `provider_timeout`. Statuses: **422** for actual FluentValidation `ValidationException` (`invalid_request`); **400** for malformed HTTP/route/query/JSON binding and domain/token prechecks outside that exception (including `stale_candidate`); **403** containment/access (`access_denied`); **404** missing movie (`movie_not_found`); **409** read-only (`read_only_library`) or missing/invalid provider configuration (`provider_not_configured`) — on POST, read-only before config; **429** GET `search_rate_limited` vs POST `download_quota_exhausted` (known remainingDownloads == 0 refuses before paid call; unknown/positive last-known permits after prechecks; no elapsed-reset replenishment); **502** provider failure / invalid-or-oversized payload (`provider_unavailable`); **504** only upstream timeout while caller request remains active (`provider_timeout`). Caller cancellation MUST NOT map to 504 or invent quota narratives. MUST NEVER expose credentials, paths, or raw provider/exception text. MUST NOT globally remap `ValidationException` or return raw endpoint exceptions (Principle V clarification).
- **FR-004**: System MUST implement concrete `SubtitleCandidateBinder` as process-local DI Singleton in `LamuFlix.Core.Features.Subtitles` (thread-safe; no `ISubtitleCandidateBinder` interface/port/layer/distributed store; no mutable static initialization). Typed `SubtitlesOptions`: `CandidateTokenTtl` default `00:05:00` (> zero), `CandidateStoreMaxEntries` default 1000 (>= 1). Mint opaque 32-hex/128-bit tokens under lock using injected seeded `Random` and `TimeProvider` without cryptographic-security claims. On insert: purge `expiresAtUtc <= now`, then if full evict earliest expiry with FIFO tie-break so new search issuance succeeds. Expired/evicted/restart-lost/cross-movie → 400 `stale_candidate` before paid calls. Process-local stale across replicas is intentional; no affinity implementation claimed (D2).
- **FR-005**: System MUST implement concrete `SubtitleMatchScorer` in `LamuFlix.Core.Features.Subtitles`: moviehash 100; else distinct-token Jaccard after invariant case-fold and Unicode letter/digit tokenization, `MidpointRounding.AwayFromZero`, empty union 0; sort matchPercent ↓, downloadCount ↓, ordinal releaseName, candidateId. Meaningful property tests for bounds, hash override, deterministic ordering, overlap symmetry/empty behavior are required with the applicable property gate (D2; brief final drafting directions).
- **FR-006**: System MUST expose `GET /api/subtitles/languages` returning 200 `SubtitleLanguagesResponse` / `SubtitleLanguageOptionDto` from single `SupportedSubtitleLanguages` catalog; private no-store; no provider call/key. MUST trim/match language query and preference entries with `OrdinalIgnoreCase` and emit canonical `Code` in DTOs, bindings, provider requests, and filenames. Empty/unknown search codes that are domain prechecks outside ValidationException → 400 `invalid_request`; FluentValidation decorator failures → 422 `invalid_request`. Empty PreferredLanguages valid; preference entries MUST resolve to supported codes. UI MUST show English — Native, default first preference, prompt without searching when null default. Catalog parameterless and unaffected unless a validator applies (catalog ruling; D2; Principle V).
- **FR-007**: System MUST NEVER auto-download or automatically retry a paid download; UI MUST disable additional Get actions while one download is in flight (Q4–Q6).
- **FR-008**: System MUST derive movie folder/video stem/canonical language code server-side; MUST write only `.srt` under `<movie folder>\Subs\`; MUST create Subs if missing; MUST use `<stem>-<code>.srt` then `<stem>-<code>(N).srt` with no space before `(N)` (Q5/Q7).
- **FR-009**: System MUST refuse resolved escapes and unsafe symlink/reparse paths before directory creation, after creation/before staging, and immediately before each publication attempt; tests MUST exercise changed-path refusal and non-overwrite without claiming elimination of every OS path race (Q5/Q7; brief repair direction).
- **FR-010**: System MUST enforce `MaxSubtitleDownloadBytes` default `2 * 1024 * 1024` while streaming the provider body (not after unbounded buffering); invalid/non-SRT or oversized → 502 `provider_unavailable` before staging with no invented quota charge/refund. MUST stage as one private leading-dot `.srt` reused across next-free non-replacing attempts; MUST publish with non-replacing move/link; MUST exclude staging from discovery (explicit tests); MUST apply `MaxSubtitlePublicationAttempts` default 10_000 (typed positive option); on attempt exhaustion return 409 `read_only_library` and clean owned staging in finally; MUST propagate caller cancellation through search/body read/staging/publication with owned-staging cleanup; MUST NEVER overwrite or delete existing/published library files (D3/Q7/Q9).
- **FR-011**: POST MUST complete local prechecks before paid call in order: missing movie 404 → invalid/stale binding 400 → read-only policy 409 → missing/invalid provider configuration 409 → containment 403. Read-only GET MUST still search when provider settings are valid and return disabled downloadAvailability `read_only_library`; GET requires valid provider configuration else 409 `provider_not_configured`; empty preferred languages alone MUST NOT 409 (D1; catalog ruling).
- **FR-012**: UI MUST list local tracks and Off above remote results in every state; MUST show truncated release names with full value on hover/focus; MUST show active check **only** on the actually active downloaded track in LocalTrackList (D5) — candidates have Get only; MUST render provider UTC reset in local time; MUST show unknown quota as unavailable; MUST use distinct search-429 vs download-429 copy; MUST keep video playing through acquisition failures (`design.md`; Q6/D1/D5).
- **FR-013**: Successful POST MUST refresh local tracks and activate only for the matching current movie/session; completion after a language switch MUST NOT replace newer remote results (Q5/Q6).
- **FR-014**: System MUST remove prepare-only artifacts per D4 delete list after current-source verification; update shared references only; retain shared mechanisms; prepare returns 404; retain `Subtitles:SaveEnabled`. MUST NOT delete shared behavior by name alone; MUST verify live references/installer/snapshot workflow at pickup because recon:113–130 inventories are incomplete (D4; recon:116/119/125/128).
- **FR-015**: Search and download MUST go through existing `ISubtitleProvider` with OpenSubtitles as the only implementation in this ticket; public DTOs MUST remain provider-neutral (owner ruling 6; ADR 0041).
- **FR-016**: Web MUST consume generated TypeScript from OpenAPI; MUST NOT hand-roll duplicate contract types or a parallel web language catalog (PRODUCT; catalog ruling).
- **FR-017**: After Gate 1, implementation MUST apply scoped Principle VII constitution maintenance `1.15.0` → `1.16.0` together with ADR 0041 acceptance (Sync Impact Report + Last Amended), superseding only ADR 0038 prepare-route wording while ADR 0038 remains Proposed and provider/read-write separation preserved; no Principle I port-list amendment; preserve Principle VI telemetry conventions when retiring prepare-only signals; no owner structural checkbox (Q2/D4). Principle V mapping is preserved, not amended, by the Cross-Artifact Alignment ruling.
- **FR-018**: Required automated coverage MUST include unit scoring/binding/language/catalog tests, **meaningful scorer/ranking property tests**, provider HTTP fixtures, real filesystem write-guard tests, API envelope/status/prepare-404/422 proofs, and Vitest/RTL/browser UI state tests; exact current existing paths verified at pickup (`brief.md` Final drafting and test directions; Q8).
- **FR-019**: Harness gates MUST use actual script interfaces and changed scope; property gate is mandatory when scorer/ranking property tests apply; MUST NEVER lower thresholds or treat `SKIPPED` / `SKIP` / `N/A` / `Could not run` as PASS; MUST NOT assume `scripts/install.ps1` exists — discover installer/snapshot interfaces from current checkout (Q8; recon:128).
- **FR-020**: Runtime acceptance remains OPEN until prerequisites execute; unexecuted checks are `Could not run`, never PASS (Q8–Q9).
- **FR-021**: Closing bar: all verified Critical/High/Medium/Low contract, safety, and acceptance findings block closure; unnecessary abstractions become follow-ups; review round cap two; Size L requires Sentry, Ledger, and Compass evidence with explicit dispositions (Q1).
- **FR-022**: No new dependency, architectural layer, database schema, player library, or LocalPlay/Process.Start change is authorized (PRODUCT; Q1/Q8).
- **FR-023**: Quota fields MUST report only provider-observed values; relative durations/Retry-After MUST convert with injected `timeProvider.GetUtcNow().Add(duration)`; MUST NOT claim a failed call was charged/refunded without provider evidence (D1).

### Key Entities

- **Subtitle Candidate**: remote search hit with candidateId, language, releaseName, matchPercent, downloadCount.
- **Candidate binding**: server-side (movieId, language, provider candidate) keyed by opaque 32-hex candidateId with expiry in Singleton binder.
- **Quota snapshot**: always-present response object; remainingDownloads and resetAtUtc nullable (never a null whole quota object).
- **Subtitle Download Availability**: enabled flag plus human-safe reason.
- **Local Subtitle Track**: existing `SubtitleTrackDto` / discovered sidecar including committed Subs downloads.
- **Published Subs file**: base `<stem>-<code>.srt`, then `<stem>-<code>(1).srt`, … under movie Subs.
- **Staging artifact**: private leading-dot `.srt` under Subs, never listed as a local track; reused across publication attempts.
- **SupportedSubtitleLanguages**: single Core catalog of canonical `(Code, EnglishName, NativeName)`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Viewer with configured key on writable library can search pt-BR (default or explicit) and see ranked results with match percent and download count without interrupting playback. → T011–T015, T021–T024, T032
- **SC-002**: Picking one result creates BS Player-named Subs file, activates captions for current session, leaves track listed locally. → T016–T020, T023–T024, T032
- **SC-003**: Download-quota exhaustion, search rate-limit, provider timeout, and read-only each show inline reason while video and local-track switching continue. → T015, T017, T021, T024, T032
- **SC-004**: Prepare route returns 404; prepare-only live references gone after verified sweep; shared SaveEnabled and local serving remain. → T025–T027
- **SC-005**: Write-guard proofs: outside-Subs refusal at required checkpoints, no overwrite, Subs creation when missing, non-discoverable staging, publication-attempt ceiling cleanup. → T010, T017–T020
- **SC-006**: Stale candidate selection rejected before any paid provider download; viewer prompted to search again. → T008, T016–T020
- **SC-007**: Unknown quota never displays as zero or inferred reset; known reset displays in local time without implying replenishment until fresh provider response. → T009, T015, T021, T024
- **SC-008**: Runtime home-cluster/BS Player receipts recorded PASS only when executed; otherwise explicitly `Could not run`. → T032

## Assumptions

All taste/policy assumptions below are Patron-approved in `ASSUMPTIONS.md` (not Quill inventions):

- Distinct-token Jaccard overlap with midpoint-away-from-zero rounding; empty union 0.
- Tie-breakers: ordinal release name then candidate ID.
- Empty PreferredLanguages → choose-language prompt, no auto-search.
- Candidate store defaults: five minutes TTL, 1000 entries; earliest-expiry FIFO eviction.
- Collision spelling without spaces: `<stem>-<code>(1).srt`.
- Session-safe activation; changed-language completion adds local track without replacing remote results.
- Copy including distinct search-429 vs download-429 strings (`design.md` / ASSUMPTIONS D1).
- Unknown quota → unavailable; elapsed reset alone is not replenishment proof.
- Active check only on actual active downloaded local track (D5).

Dependencies assumed available before implementation/runtime proofs: Player CC panel ticket; Subs discovery ticket; home-cluster ticket. Phase B verifies readiness and amends tasks only where authorized — does not rebuild prerequisites inside DEV-501.
