# Feature Specification: Subtitle picker grouped search and batch download

**Feature Branch**: `feature/552-spec`

**Created**: 2026-11-28

**Status**: gate1: provisional

**Input**: Ticket DEV-552 (size L); grill-closed `brief.md` (Q1–Q8); `CONCLUSIONS.md`; `ASSUMPTIONS.md` (Q3, Q5); Proposed ADR `docs/adr/0053-incremental-subtitle-provider-results.md`; Maestri notes `DEV-552`, `recon-DEV-552`; `specs/PRODUCT.md`. Decisions come only from brief and its rulings. Plan-challenge dispositions items 1–9 at HEAD `e9175fcb`.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Frozen scope / Q6–Q7; `PRODUCT.md` §5). Ordinary owner Gate 1 remains the Spec Kit PR merge path. ADR 0053 stays Proposed on Spec PR until Accepted in the delivery task sequence after Gate 1 (T001).

## Clarifications

### Session 2026-11-28

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Progressive opt-in search stream (Q1), preferred-language codes and hash-match flag (Q2), provider line and quota across overlapping languages (Q3 taste `[assumed]`), stream completion and errors (Q4), batch snapshot/order/finish (Q5 taste `[assumed]`), authorized file scope (Q6), closing bar / frozen scope (Q7), and stream `downloadAvailability` (Q8) are settled in `brief.md` and `CONCLUSIONS.md`. OpenSubtitles movie-hash submission is a plan verification fact (Q2), not an inventable behaviour choice. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour. Any gap that cannot be closed from brief/CONCLUSIONS goes to `DRAFTING_RECEIPT.md` as `needs decision:`.

## What the User Will See *(mandatory)*

- **Where**: Browser player CC panel → Subtitles tab (`SubtitlesTab`)
- **What the user sees or does**: Opening the tab starts searches for every configured preferred language (or the catalog default when the preference list is empty). Results fill in by provider as each provider settles, grouped by language with counts. Each row shows provider, release name, match percent, download count, an affirmative hash-match mark when evidence exists, and a checkbox. A provider summary line shows each configured provider’s outcomes and known quota. Read-only acquisition still shows candidates and the read-only reason while candidate and batch download actions stay disabled. The owner ticks many rows and presses **Download selected (n)**; rows show waiting, done, or failure (including **expired, search again** with explicit language re-search for a later batch). Tracks reload once when the current-media batch settles. **Add a language** still searches a non-preferred language.
- **What it needs to show up**: Default Compose stack; at least one library movie with a primary video; subtitle acquisition configured with at least two enabled usable providers for multi-provider proof; `Subtitles:PreferredLanguages` (or a non-null catalog `defaultLanguage` when preferences are empty); DEV-551 consumer contract landed for provider identity and per-provider outcomes; DEV-553 track-list refresh path available for player visibility of new tracks

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** a library movie open in the browser player with at least two subtitle providers enabled, **When** the owner opens the Subtitles tab, waits for preferred-language results, ticks two candidates, and presses Download selected, **Then** the panel shows both rows completing and the player track list includes two new tracks after the single batch-end reload

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Progressive multi-language search grouped by language (Priority: P1)

Opening the Subtitles tab searches every configured preferred language concurrently across enabled usable providers. Results appear as each provider settles for each language. Candidates are grouped by language with counts; rows show provider identity and ranking fields. An empty preference list falls back to one automatic search of `defaultLanguage` when present. **Add a language** remains available for other supported languages. Streamed availability from the first settled event applies to the first visible candidates, including partial rows after interruption.

**Why this priority**: Wanted 1–3 and 9; without progressive grouped search the picker does not meet the owner ask.

**Independent Test**: With two providers and two preferred languages, the panel shows two language groups, provider identity on rows, and progressive fill-in without waiting for the slowest provider of every language before showing the first settled provider’s candidates.

**Acceptance Scenarios**:

1. **Given** two preferred languages and two enabled usable providers, **When** the owner opens the Subtitles tab, **Then** both languages are searched concurrently and candidates appear under language headings with counts as providers settle
2. **Given** an empty preferred-language list and a non-null catalog default language, **When** the owner opens the Subtitles tab, **Then** exactly one automatic search runs for that default language and **Add a language** remains available
3. **Given** search results from multiple providers for one language, **When** candidates are displayed, **Then** each row shows provider, release name, match percent, download count, and an affirmative hash-match mark only when provider evidence is affirmative
4. **Given** a language outside the preferred list, **When** the owner uses **Add a language** and searches, **Then** that language’s results appear as an additional group after preferred groups
5. **Given** a read-only `downloadAvailability` snapshot on the first `providerSettled` event, **When** candidates become visible (including after interruption with partial rows), **Then** candidates and the read-only reason are shown, candidate and batch download actions are disabled, and zero download POSTs are issued (Q8)

---

### User Story 2 - Provider summaries and quota across overlapping searches (Priority: P1)

While preferred languages search concurrently, the panel keeps one summary line per configured provider. Outcomes stay visible per language (including failures and safe reasons). Quota shows the last observed remaining value or unknown; download observations supersede in-flight search observations **per provider**. A late search may still update outcomes and candidates but must not overwrite a download quota observation. Selected spend is counted per metered provider without summing remaining balances across providers. Selection may exceed the last observed quota; the download route decides each attempt.

**Why this priority**: Wanted 5–6; overlapping language searches must not hide partial failures or invent reserved quota.

**Independent Test**: Force one provider ok and one failed across two languages; confirm both outcomes remain visible and search-quota display follows last-observation / unknown rules without requiring selection or download. Scenarios that depend on downloads or selection (scenario 2 download superseding; scenario 3 selected spend) are proved under US3.

**Acceptance Scenarios**:

1. **Given** overlapping language searches, **When** providers settle with mixed success and failure, **Then** the provider line retains per-language outcomes and safe reasons beside successes
2. **Given** a known quota observation from search, **When** a later download returns a different remaining count, **Then** the panel shows the download observation and unknown stays unknown when never observed (proved with US3 download observations)
3. **Given** selected rows that would spend more than the last observed remaining quota, **When** the owner reviews the download control, **Then** selected spend is shown per metered provider and every selected row remains eligible to submit (proved under US3 selection)
4. **Given** a download quota observation for a provider, **When** a late in-flight search event arrives afterward, **Then** outcomes/candidates may update but the quota display keeps the download observation until a subsequent eligible search may update it (proved under US3 T016)

---

### User Story 3 - Multi-select batch download on the existing route (Priority: P1)

The owner ticks any number of rows. **Download selected (n)** snapshots distinct checked candidate IDs in current visual order and submits them one at a time through the existing single-candidate download route. One active batch per media item. Each row shows waiting, done, or failure with reason, including expired candidates marked **expired, search again**. Recovery of an expired row is an explicit re-search of that language and selection of a fresh candidate for a **subsequent** batch; the original batch keeps its snapshot and continues through later rows. A failure or expiry does not stop later rows. Tracks reload exactly once when a batch settles for the **current** picker media context — all success, mixed failure, or all failed — and perform **zero** track refreshes when the media context has been superseded. Selection is keyed by opaque candidate ID so incremental reordering preserves checks. Responses from a previous media item are ignored. DEV-553 owns player track visibility proof for new tracks. Read-only availability disables batch download actions with zero POSTs (Q8).

**Why this priority**: Wanted 4, 7–8 and Done when; batch download is the core interaction change.

**Independent Test**: Tick three rows and press once; observe three sequential download calls in visual order, row-level failure isolation, and a single track reload at batch end for every settled current-context outcome including all failed.

**Acceptance Scenarios**:

1. **Given** three checked candidates in visual order, **When** the owner presses Download selected, **Then** three download requests run sequentially in that order through the existing single-candidate route
2. **Given** a batch where the second download fails or is expired, **When** the batch continues, **Then** that row shows its reason (including **expired, search again** when expired) and later rows still run
3. **Given** an expired row after the original batch continues, **When** the owner explicitly re-searches that language and selects a fresh candidate, **Then** a subsequent batch can download the new candidate (never by resubmitting the expired ID)
4. **Given** a settled batch for the current media context that is all success, mixed failure, or all failed, **When** the batch settles, **Then** the track list reloads exactly once
5. **Given** a batch whose picker media context has been superseded, **When** that stale batch settles, **Then** it performs zero track refreshes and late responses are ignored
6. **Given** results still arriving and reordering within a language, **When** the owner has checked rows by candidate ID, **Then** checks remain on the same candidates after reorder
7. **Given** the owner changes media during an in-flight search or batch, **When** late events or responses arrive for the previous media, **Then** they are ignored

---

### User Story 4 - Opt-in incremental search stream contract (Priority: P2)

The existing one-language search GET gains an opt-in `Accept: text/event-stream` representation that emits one complete provider outcome plus its candidates as each provider in the request-scoped configured-provider snapshot settles, then an explicit terminal completion event whose settled provider count equals that snapshot size. Default JSON response semantics and status codes stay unchanged. Pre-stream validation and no-usable-provider 409 remain JSON before any stream opens. Disconnect cancels outstanding provider work through fan-out, provider calls, and outbound HTTP, and stops post-abort title fallback. Interrupted streams keep partial rows and require explicit retry; a close without the terminal event means incomplete for that language. Each `providerSettled` event carries the same required `downloadAvailability` snapshot taken at stream open (Q8).

**Why this priority**: Wanted 1 delivery mechanism (Q1/Q4/Q8); enables progressive UI without changing the download route.

**Independent Test**: Control two provider completions so the first event is observed before the slower provider finishes; assert terminal settled count equals snapshot size, interrupted-without-terminal incomplete behaviour, pre-stream JSON errors, and identical availability on every settled event.

**Acceptance Scenarios**:

1. **Given** an Accept opt-in stream search with two providers in the request snapshot, **When** the first provider settles before the second, **Then** the client receives a `providerSettled` event with that provider’s outcome, candidates, and the stream’s `downloadAvailability` snapshot before the second settles, then a `searchCompleted` terminal event whose settled provider count equals the snapshot size
2. **Given** request-level domain, validation, or no-usable-provider conditions, **When** search is requested with the stream Accept, **Then** the response is the existing DEV-551 JSON error (including 409) and no stream opens
3. **Given** a mixed snapshot containing initially usable and unusable providers, **When** the stream runs, **Then** every snapshot provider emits one outcome and the terminal count matches the snapshot; providers added after the snapshot are excluded
4. **Given** an opened stream that closes without the `searchCompleted` terminal event, **When** the client updates that language, **Then** already-received rows remain (still governed by the availability snapshot), the language is incomplete, and only an explicit retry restarts search
5. **Given** a client disconnect mid-stream, **When** the server observes abort, **Then** outstanding provider calls and outbound HTTP for that request are cancelled and no post-abort title fallback runs
6. **Given** enabled and read-only streams, **When** multiple `providerSettled` events arrive, **Then** every event repeats the identical `downloadAvailability` snapshot taken at stream open (Q8)

---

### User Story 5 - Accessibility for selection and batch control (Priority: P3)

Every checkbox has an accessible label. The download control announces the selection count. Existing keyboard and screen-reader use of the panel is preserved.

**Why this priority**: Wanted 10; required for Done when accessibility bar without blocking the P1 interaction slices.

**Independent Test**: Automated web tests assert labeled checkboxes and an announced selection count on the batch control.

**Acceptance Scenarios**:

1. **Given** visible candidate rows, **When** assistive technology reads a checkbox, **Then** each checkbox has an accessible label
2. **Given** one or more checked rows, **When** the download control is presented, **Then** the selection count is announced

---

### Edge Cases

- Empty preferred list with null `defaultLanguage`: no automatic search; **Add a language** remains available (Q2)
- Stream interruption after partial provider events: keep rows under the availability snapshot; mark language incomplete; no automatic retry (Q4/Q8)
- Media change while searches or a batch are in flight: cancel media-scoped searches; ignore superseded events and batch responses; zero track refreshes for superseded-context batches (Q4/Q5; disposition item 4/6)
- Expired candidate in a batch: mark **expired, search again**; continue later rows; recover via explicit language re-search and a fresh candidate in a subsequent batch (Q5; disposition item 4/6)
- Selection larger than last observed quota: still submit every selected row; route decides each attempt (Q3)
- Late search after download quota observation: outcomes/candidates may update; quota keeps download precedence until a subsequent eligible search (Q3; disposition item 3/4)
- Hash mark without affirmative provider evidence: show false / no affirmative mark; never infer from match percent; generic providers default false (Q2; disposition item 3/5)
- Zero usable providers at preflight: JSON 409 before SSE; mixed snapshot still emits every snapshot provider (Q4; disposition item 1)
- DEV-553 track-flow gap for acceptance: pause for a scoped Patron ruling; no opt-out of three-file/three-track or two-track e2e proofs (Q6)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Opening the Subtitles tab MUST start one search per configured preferred language concurrently; an empty preference list MUST fall back to one automatic search of catalog `defaultLanguage` when non-null, otherwise none; **Add a language** MUST remain available (`brief.md` Frozen scope; Q2)
- **FR-002**: Search MUST use the existing one-language server fan-out across enabled usable providers; progressive delivery MUST use opt-in `Accept: text/event-stream` on the existing GET without a new route or provider query parameter (Q1; ADR 0053). Documented call cost is concurrent preferred languages × usable providers, with conditional hash-to-title adding another provider call where that path applies; retain existing hash-read topology and one concurrent request per preferred language; no cross-request optimization or changed search concurrency in this ticket (disposition item 2/3)
- **FR-003**: Default JSON search response semantics and status codes MUST remain unchanged for non-stream clients; additive `hashMatch` (non-null boolean) MUST appear on JSON candidates and stream candidates; true only on explicit affirmative provider evidence carried as a nullable explicit-evidence value on the existing candidate result through `ISubtitleProvider` (for example `ExplicitHashMatch`); keep `MatchesMovieHash` and scoring fallback intact; missing/false evidence, providers with no defined affirmative signal, and title fallback MUST be false (Q1–Q2; disposition item 3/5; recon-DEV-552:4–7)
- **FR-004**: For a validated request the server MUST snapshot configured provider identities and initial eligibility before fan-out; the stream MUST emit one complete snapshot-provider outcome plus its candidates as each provider settles (including immediate off/unusable) under wire event `providerSettled`, then an explicit terminal completion event `searchCompleted` whose settled provider count equals the snapshot size; configured-set changes after the snapshot belong to the next request; close without terminal means incomplete for that language (Q4; disposition item 1)
- **FR-005**: Pre-stream request-level domain errors, validation failures, and no-usable-provider 409 MUST stay JSON before any stream opens with retained DEV-551 error semantics; zero usable providers MUST return that 409 before SSE headers or bytes; server MUST propagate request abort through shared fan-out, provider calls, and outbound HTTP (existing deadlines kept) and MUST stop post-abort title fallback (Q4; disposition items 1–2)
- **FR-006**: Languages response MUST add ordered canonical `preferredLanguageCodes` (Q2)
- **FR-007**: Candidates MUST group by language with counts; group order MUST be configured preference order, then manually added languages in add order; within a language, order MUST remain match percent, then download count, then existing release-name tie-break (Frozen scope; Q5)
- **FR-008**: Each row MUST show provider, release name, match percent, download count, and an affirmative hash-match mark only when evidence is affirmative (Wanted 3; Q2)
- **FR-009**: Selection MUST key by opaque candidate ID so incoming results and reordering preserve checked rows (Frozen scope; Q5)
- **FR-010**: **Download selected (n)** MUST snapshot distinct checked candidate IDs in current visual order and submit them sequentially through the existing single-candidate download route; one active batch per media item (Q5)
- **FR-011**: Each batch row MUST show waiting, done, or failure with reason, including **expired, search again**; failures/expiries MUST leave subsequent attempts eligible; expired recovery MUST be explicit language re-search plus a fresh candidate for a subsequent batch while the original batch continues (Wanted 4, 7; Q5; disposition item 4/6)
- **FR-012**: Tracks MUST reload exactly once when a batch settles for the current picker media context for every settled batch — all success, mixed failure, or all failed — and MUST perform zero track refreshes when the media context has been superseded; DEV-553 owns player track visibility (Wanted 8; Q5–Q6; disposition item 4/6)
- **FR-013**: Provider summary MUST show one line per configured provider with per-language outcomes and safe reasons; quota MUST show last observed remaining or unknown with **per-provider** download precedence over in-flight search observations (late search may update outcomes/candidates, not that quota, until a subsequent eligible search); selected spend MUST be per metered provider without summing remaining balances across providers; selection above last observation remains allowed (Q3; disposition item 3/4)
- **FR-014**: Interrupted streams MUST keep partial rows, mark incomplete, and require explicit retry; media change MUST cancel stale searches and ignore superseded events/responses (Q4–Q5)
- **FR-015**: Checkboxes MUST have accessible labels; the download control MUST announce selection count (Wanted 10)
- **FR-016**: Named C# SSE payload DTOs (provider-settled and terminal) MUST appear in OpenAPI and be consumed via generated TypeScript; regenerate `openapi.json`/`types.ts` via the existing workflow; never hand-edit generated files; negotiated GET MUST preserve search no-store handling, declare `Vary: Accept`, use a single server-side SSE item carrier for both concrete DTOs, and declare both 200 content types (Q1; Q6; disposition item 5/7)
- **FR-017**: Scope MUST exclude automatic download, deletion, subtitle preview, scorer changes, new hashing work, new dependencies/projects/layers/schema, and LocalPlay/secrets/`Process.Start` boundaries (`brief.md` Frozen scope; Q6)
- **FR-018**: Each `providerSettled` event MUST carry required non-null `downloadAvailability` reusing existing `SubtitleDownloadAvailabilityDto`; one snapshot is taken when the successful stream opens and is repeated unchanged on every settled event; the picker MUST apply that snapshot to the first visible candidates including partial rows after interruption; read-only MUST show candidates and reason, disable candidate and batch download actions, and issue zero download POSTs; no new DTO/event/additional search; JSON and download-route contracts preserved (Q8; disposition item 9 / repair item 7; recon-DEV-552:11–14)
- **FR-019**: Web stream consumption MUST use `fetch` with the streaming response body, an in-repo parser, and `AbortController`, with no `EventSource` and no reconnect (disposition item 5/7)

### Key Entities

- **Preferred Language Codes**: Ordered canonical supported language codes from configuration that drive automatic opening searches
- **Subtitle Candidate (picker row)**: Remote result shown with provider identity, release name, match percent, download count, hash-match flag, and opaque Candidate ID
- **Provider Outcome (per language)**: Settled configured-provider state with safe reason and optional observed quota for one language search
- **Provider-set Snapshot**: Request-scoped configured provider identities and initial eligibility captured before fan-out; terminal settled count equals its size
- **Download Availability Snapshot**: Stream-open `SubtitleDownloadAvailabilityDto` repeated unchanged on every `providerSettled` event
- **Search Stream Session**: Opt-in incremental search for one movie and one language, ending in terminal completion or incomplete interruption
- **Download Batch**: Frozen snapshot of distinct candidate IDs for one media item, executed sequentially on the existing download route

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Web tests show results from two providers and two languages in two language groups with provider identity on each row
- **SC-002**: Web tests show ticking three rows and one Download selected press sends three download calls in visual order
- **SC-003**: Web tests show a failed download marks its row and the next download still runs; an expired candidate marks its row and others download; expired recovery uses explicit re-search and a fresh candidate in a later batch; superseded-context batches perform zero track refreshes
- **SC-004**: Web tests show the quota line when known and name a failed provider in the provider line; download-then-stale-search keeps download quota while allowing later eligible search updates
- **SC-005**: Integration test shows three same-language downloads via the existing route publish three distinct files and three tracks (actual published files and track list — not three HTTP 200s alone)
- **SC-006**: Browser e2e searches, ticks two rows, downloads once, and shows two new tracks in the player through DEV-553
- **SC-007**: PR includes a screenshot of the home-library panel with at least two providers on
- **SC-008**: Contract/endpoint tests cover first-provider-before-second streaming with exact `providerSettled` / `searchCompleted` discriminators, request-scoped snapshot outcomes, terminal settled count equals snapshot size, all-unusable/mixed/in-flight set-change cases, interrupted/cancelled search with gated abort (no post-abort fallback), pre-stream request-level domain/validation/409 JSON, legacy JSON compatibility, unmodified single-candidate download, preference order/fallback, hash evidence cases (generic false default, explicit true, explicit false, absent on hash search, title fallback; OpenSubtitles code-path), identical `downloadAvailability` snapshot on every settled event in read-only and enabled modes, both 200 content types, and generated stream payload schemas including required reused availability type plus post-wiring consumer types
- **SC-009**: Accessibility web coverage proves labeled checkboxes and announced selection count
- **SC-010**: Size L gates, three review axes, and ticket receipts meet the Q7 closing bar with honest exit-2 labels
- **SC-011**: Web tests prove first-event read-only rows visible with reason, disabled candidate/batch download actions, and zero POSTs (`subtitlesTab.test.tsx`)

## Assumptions

- Q3 and Q5 taste details logged `[assumed]` in `ASSUMPTIONS.md` and confirmed in `CONCLUSIONS.md`
- DEV-551 supplies provider identity and per-provider search states on the consumer search contract before Build consumes them (`Needs`); pickup verifies the landed contract and preflight/topology (T002)
- DEV-553 owns player track visibility; if the existing track-refresh flow cannot support acceptance, Build pauses for a scoped Patron ruling (Q6)
- OpenSubtitles adapter emits `moviehash` for nonblank hashes; explicit `moviehash_match` is OpenSubtitles’ evidence; the explicit-vs-fallback split is verified in the plan (`recon-DEV-552` current lines 4–7; Q2). Public `hashMatch` requires explicit affirmative provider evidence and stays false without it.
- No new NuGet/npm dependency; built-in ASP.NET Core 10 SSE only (Q1)
- Generated OpenAPI/TypeScript artifacts change only by regeneration (Q6)
- Q8 reuses existing `SubtitleDownloadAvailabilityDto`; no new DTO/event/additional search
