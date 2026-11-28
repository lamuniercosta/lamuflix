---
description: "Task list for DEV-552 subtitle picker grouped search and batch download"
---

# Tasks: Subtitle picker grouped search and batch download

**Input**: Design documents from `specs/DEV-552/`

**Prerequisites**: plan.md, spec.md, contracts/search-stream.md, brief.md Q1–Q8

**Tests**: Required by ticket Done when and `brief.md` Test strategy — include test tasks.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1…US5 maps to spec.md user stories
- Include exact file paths in descriptions

## Phase 0: Gate 1 (Spec Kit)

**Purpose**: Owner Gate 1 on Spec Kit PR — blocks Build

**Pre-Gate-1 rule (Q7)**: The two-round plan challenge (Keel analyze / plan challenge) is a pre-Gate-1 sequencing rule. It is not a delivery task under T025. Q7 caps remain unchanged: two plan-review rounds and two code-remediation rounds maximum. Spec PR must include ADR 0053 as Proposed.

- [ ] T000 Spec Kit PR merged / Gate 1 cleared for DEV-552 (ordinary owner merge path). Quill/Build must not start production edits before this receipt.

---

## Phase 1: Delivery prerequisites

**Purpose**: ADR accept + pickup verification before production edits

- [ ] T001 After T000: Flip ADR `docs/adr/0053-incremental-subtitle-provider-results.md` status from Proposed to Accepted in the delivery sequence (Keel-owned ADR text; status transition only). Retain before-deliverable-merge expectation for stream behaviour (disposition item 6/8).
- [ ] T002 After T000: Pickup recon — verify DEV-551 landed consumer contract (provider identity, `providers[]`, closed states/reasons), DEV-551 **preflight** (no-usable → JSON 409 before stream), and landed fan-out/hash-read topology (concurrent languages × usable providers; conditional hash-to-title second call; retained hash reads per recon-DEV-552:4–7). Verify DEV-553 track-refresh path against Done when. Stop `needs recon:` or scoped Patron ruling if either gap blocks acceptance (Q6; disposition items 1–2).

**Checkpoint**: Gate 1 + ADR Accept + dependency verification complete

---

## Phase 2: Foundational server contract (blocks UI consumers)

**Purpose**: Additive languages/candidate/availability fields, named SSE DTOs, OpenAPI registration, and failing-then-passing contract tests before frontend wiring (`brief.md` Task ordering items 1–2; disposition items 5–7)

**⚠️ CRITICAL**: No picker consumer work until this phase’s generate:api / schema-proof receipt exists. Split: (a) T007 **proof definitions** may be written first and stay red; (b) T003–T006 may start once those definitions exist and **do not wait for T007 to pass**; (c) **T005 follows T003** for shared `SubtitleEndpoints.cs` edits (T004 may stay parallel with T003); (d) T006 is the **final green receipt** that drives the T007 suite green and records backend schema proof before web.

- [ ] T007 Write contract/integration **proof definitions** in `tests/LamuFlix.IntegrationTests/SubtitleSearchDownloadEndpointTests.cs` (and focused unit tests under `tests/LamuFlix.UnitTests/Features/Subtitles/` as needed): control two provider completions so first `providerSettled` event+candidates are read before slower provider; request-scoped provider snapshot — all-unusable JSON 409 (no stream), mixed snapshot emits every snapshot provider, in-flight configured-set change excluded, terminal `searchCompleted` / `settledProviderCount` equals snapshot size; gated abort cancels outstanding provider/HTTP work and stops post-abort title fallback; exact framing, flushed first event, disconnect; pre-stream request-level domain errors, validation, and no-usable 409 with retained DEV-551 semantics; legacy JSON compatibility including `hashMatch`; unmodified single-candidate download; catalog ordered `preferredLanguageCodes` field; hash evidence — generic mapping false default, OpenSubtitles explicit true/false/absent on hash search plus title fallback (recon-DEV-552:4–7; no new adapter); identical `downloadAvailability` snapshot on every `providerSettled` in read-only and enabled modes (Q8); generated stream payload schema present (SC-008; `brief.md` Test strategy). Picker-owned empty-preference → `defaultLanguage` fallback is proved in T011, not here. Definitions may remain red while T003–T006 run; final green is T006’s receipt.
- [ ] T003 [P] After T007 proof definitions exist (need not be green): Add ordered `preferredLanguageCodes` to existing languages query/DTO path used by `src/LamuFlix.Api/Endpoints/SubtitleEndpoints.cs` / languages response DTOs (FR-006; Q2; `contracts/search-stream.md`). Shared-file note: any T003 edits to `SubtitleEndpoints.cs` complete before T005.
- [ ] T004 [P] After T007 proof definitions exist (need not be green): Add non-null `hashMatch` on candidate DTO via focused mapping from a **nullable explicit-evidence** value on the existing candidate result through `ISubtitleProvider` (for example `ExplicitHashMatch`); keep `MatchesMovieHash` and scoring fallback intact; public false for missing/false evidence, providers with no defined affirmative signal, and title fallback; cover generic result mapping and OpenSubtitles adapter cases without inventing another adapter or a live provider proof (FR-003; Q2; Q6; recon-DEV-552:4–7; disposition item 3/5)
- [ ] T005 After T003 (and after T007 proof definitions exist; need not be green): Name and implement SSE payload DTOs `SubtitleProviderSettledEventDto` and `SubtitleSearchStreamCompletedDto` (`settledProviderCount`) beside existing endpoint DTOs; include required non-null `downloadAvailability` reusing existing `SubtitleDownloadAvailabilityDto` — one snapshot at successful stream open, repeated unchanged on every `providerSettled` (Q8); emit exact wire discriminators `providerSettled` and `searchCompleted`; wire opt-in `Accept: text/event-stream` on existing search GET via built-in ASP.NET Core 10 SSE sharing the DEV-551 fan-out; capture request-scoped provider snapshot before fan-out; zero usable → JSON 409 before SSE; propagate `RequestAborted` through fan-out/providers/outbound HTTP and stop post-abort fallback; single `SseItem<T>` carrier for both concrete DTOs; immediate per-event delivery (supported response-path buffering must not delay provider progress); preserve search no-store and declare `Vary: Accept`; both 200 content-type metadata; preserve default JSON semantics/status codes; terminal event required (FR-002/004/005/016/018; Q1/Q4/Q8; ADR 0053; `SubtitleEndpoints.cs`; disposition items 1–2, 5/7)
- [ ] T006 After T003–T005: Register SSE DTO schemas for OpenAPI emission if generator omits them; regenerate `web/src/api/openapi.json` and `web/src/api/types.ts` via existing workflow; never hand-edit; assert both named schemas (`SubtitleProviderSettledEventDto`, `SubtitleSearchStreamCompletedDto`), additive field types (`preferredLanguageCodes`, `hashMatch`), both 200 media types on the negotiated GET, JSON compatibility apart from additive `hashMatch`, and required reused `SubtitleDownloadAvailabilityDto` / `downloadAvailability` in regenerated OpenAPI/`types.ts` (exact generated contract check from `brief.md` Task ordering 1; Q8). Backend schema proof precedes web consumption (FR-016/018; Q1/Q6/Q8). **Final green receipt**: drive the T007 suite green.

**Checkpoint**: Server contract + generate:api schema proof + stream proofs green (T006 receipt); JSON and download regressions green; no web consumer yet

---

## Phase 3: User Story 1 - Progressive grouped search (Priority: P1) 🎯 MVP UI

**Goal**: Preferred-language concurrent searches with progressive fill-in, language groups, provider rows, hash mark, availability/read-only, Add a language, explicit retry
**Independent Test**: SC-001/SC-011; US1 scenarios

- [ ] T008 [US1] Extend `web/src/features/playback/useSubtitleAcquisition.ts` so picker searches use `fetch` with the streaming response body, an in-repo parser, and `AbortController` (no `EventSource`, no reconnect); **require** `Accept: text/event-stream`; on success consume wire events `providerSettled` and `searchCompleted`; apply required generated `downloadAvailability` from the first settled event to the first visible candidates, including partial rows after interruption; handle documented pre-stream JSON errors (request-level domain, validation, no-usable 409) without treating them as stream sessions; type-check against both generated payload types when wired; start concurrent searches per `preferredLanguageCodes` with **picker-owned** empty-preference fallback to one automatic `defaultLanguage` search when non-null, else zero automatic searches; retain partial rows; track per-language completeness/terminal; explicit language retry that preserves partial rows, ignores superseded retry events, and never auto-retries; cancel/ignore on media change; wire **Add a language** search for non-preferred languages (FR-001/002/014/018/019; Q2/Q4/Q8)
- [ ] T009 [US1] Update `web/src/features/playback/SubtitlesTab.tsx` to group candidates by language with counts; order groups preference-then-added; within-language ranking unchanged; show provider, release, match percent, download count, affirmative hash mark; surface incomplete/explicit-retry and **Add a language**; honour picker-owned empty-preferences fallback; under read-only availability show candidates and reason while disabling candidate download actions (FR-007/008/018)
- [ ] T010 [P] [US1] Add picker copy in `web/src/features/playback/subtitleCopy.ts` for group headings, hash mark, incomplete/retry, Add a language, read-only reason as needed (taste within Wanted; no new behaviour)
- [ ] T011 [US1] Extend `web/src/features/playback/subtitlesTab.test.tsx` for two providers × two languages grouping, progressive identity/counts/ranking, overlapping first-provider progress; **both** empty-preference fallback branches — exactly one automatic request when `defaultLanguage` is non-null, and zero automatic requests when default is null; Add a language; explicit retry (partial rows kept; superseded retry events ignored; no auto retry); interrupted-stream recovery; stale media isolation; required Accept opt-in and pre-stream JSON error handling; fetch/parser proofs — split chunks, ignored unknown events, close without terminal; first-event read-only rows visible with reason, disabled download actions, and zero POST (SC-001/SC-011; Q8; `brief.md` Web tests). Backend catalog ordered-preference field remains T007/T003.

**Checkpoint**: Grouped progressive search usable without batch download

---

## Phase 4: User Story 2 - Provider summaries and quota (Priority: P1)

**Goal**: One provider line with per-language search outcomes; last-observed quota from search
**Independent Test**: SC-004 search-outcome/quota cases (US2 scenario 1 and search half of scenario 2). US2 scenarios that depend on downloads/selection (download superseding search; selected spend / over-quota; download-then-stale-search) are covered by US3 T014/T016.

- [ ] T012 [US2] In `SubtitlesTab.tsx` / `useSubtitleAcquisition.ts`, retain per-configured-provider **search** outcomes across overlapping language searches; show safe reasons; last observed quota from search with unknown staying unknown; prepare per-provider observation ordering so download precedence can supersede without late search overwriting quota (FR-013 search half; Q3; ASSUMPTIONS.md). Selection-driven spend and download-superseding observations are owned by US3 (T014/T016), not this slice.
- [ ] T013 [US2] Extend `web/src/features/playback/subtitlesTab.test.tsx` for provider failure naming, known/unknown quota from search, and quota freshness under overlapping searches (SC-004 search outcomes). Do not require selection/batch UI here.

**Checkpoint**: Provider/quota search summaries correct under overlap without depending on US3 selection

---

## Phase 5: User Story 3 - Batch download (Priority: P1)

**Goal**: Multi-select sequential downloads on the existing route; row states; expired recovery; one track reload for current context only
**Independent Test**: SC-002/003/005/006; US3 scenarios

- [ ] T014 [US3] Create checkbox selection keyed by candidate ID; **Download selected (n)** snapshots distinct IDs in visual order; one active batch per media; sequential existing-route posts; row waiting/done/failure including **expired, search again**; continue after failure/expiry; expired recovery via explicit language re-search and fresh candidate for a subsequent batch (original batch continues); ignore stale media responses; under read-only availability disable batch download actions and issue zero POSTs; show selection-driven spend per metered provider; allow over-quota submission (route decides); apply per-provider download observations superseding in-flight search observations (late search may update outcomes/candidates, not that quota) (FR-009/010/011/013/014/018; Q3/Q5/Q8)
- [ ] T015 [US3] Reload tracks exactly once when the batch settles for the **current** picker media context for **every** settled batch — all success, mixed failure, or all failed; perform **zero** track refreshes when the media context has been superseded; integrate existing DEV-553 track flow; pause for Patron ruling if flow cannot meet acceptance (FR-012; Q6; disposition item 4/6)
- [ ] T016 [US3] Extend `web/src/features/playback/subtitlesTab.test.tsx` for three sequential downloads in visual order, failure then continue, expiry then continue, expired recovery via explicit re-search + fresh candidate in a subsequent batch, retained selection through reorder / frozen membership, single final track refresh including the all-failed case for current context, zero track refresh when media context superseded, one-active-batch guard, media-change batch-response isolation, selection-driven spend, over-quota submission, download-then-stale-search-then-newer-eligible-search quota precedence, and read-only batch guard / zero POSTs (SC-002/003/004/011; FR-012/013/018)
- [ ] T017 [US3] Integration publication proof in `tests/LamuFlix.IntegrationTests/SubtitleSearchDownloadEndpointTests.cs` only (named existing file; no companion): three separate existing-route downloads for one language yield three distinct published files and three tracks — assert files and track list, not merely three HTTP successes (SC-005)

**Checkpoint**: Batch download + publication proof complete

---

## Phase 6: User Story 4 - Stream contract hardening already in Phase 2

**Goal**: US4 acceptance is delivered by T007 then T003–T006; this phase is the regression hold while UI consumes the stream
**Independent Test**: SC-008 retained green after UI wiring

- [ ] T018 [US4] After UI wiring (T008–T017): Re-run stream/JSON/download contract suite from T007 and require green (SC-008 regression)

**Checkpoint**: Stream contract still green with consumers attached

---

## Phase 7: User Story 5 - Accessibility (Priority: P3)

**Goal**: Labeled checkboxes; announced selection count
**Independent Test**: SC-009

- [ ] T019 [US5] After T014: Ensure accessible checkbox labels and announced selection count on the batch control in `SubtitlesTab.tsx` / `subtitleCopy.ts` (FR-015; Wanted 10)
- [ ] T020 [US5] Extend `web/src/features/playback/subtitlesTab.test.tsx` for accessible labels and announced count (SC-009)

**Checkpoint**: Accessibility bar covered

---

## Phase 8: Browser e2e, screenshot, glossary

**Purpose**: Done when browser/screenshot receipts; optional CONTEXT term

- [ ] T021 Browser e2e in existing suite: search, check two rows, submit once, verify two new tracks in the player through DEV-553 (SC-006; SMOKE-001)
- [ ] T022 [P] Capture PR screenshot of home-library panel with at least two providers on (SC-007)
- [ ] T023 [P] If spec domain vocabulary requires it, add **Hash Match** to `CONTEXT.md` without rewriting existing subtitle terms (Principle VIII; `brief.md` Files)

---

## Phase 9: Exact-head gates and Size L review

**Purpose**: Q7 closing bar

- [ ] T024 After all in-scope receipts (T007/T011/T013/T016/T017/T018/T020/T021/T022 and any refactor): Exact-head Roslyn, cyclomatic complexity (incl. refactor threshold), InspectCode, property tests, vulnerable packages, format, full `dotnet test`, mutation when applicable, web lint/test/build + contract drift. Size L ship-review split and rebased-head verification at exact reviewed head. Exit 1, `Could not run`, and an unaccepted property-test exit 2 block. Keep SKIPPED/N/A distinct from PASS. Final hosted checks include gitleaks (SC-010; Q7; plan Gates)
- [ ] T025 After T024 (delivery review): Risk (Sentry), Standards (Ledger), Spec (Compass) with explicit dispositions for every finding; apply the **two code-remediation** round cap here (Q7). The two-round **plan challenge** is the pre-Gate-1 rule in Phase 0, not this task. Critical/High/Medium block; verified acceptance/security/contract failures block at any severity; disposition every other finding explicitly; unresolved blockers to Bernstein (Q7 caps unchanged)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0**: Blocks all delivery work
- **Phase 1**: ADR Accept + pickup deps before Build
- **Phase 2**: T007 proof definitions (may stay red) → T003 then T005 (shared endpoint file) with T004 parallel to T003 → T006 schema proof + final green receipt; blocks UI consumers
- **Phases 3–5**: UI stories on verified contract; US1 → US2 → US3 natural order on shared picker files (limited parallelism). US3 creates the selection surface.
- **Phase 6**: Regression after consumers
- **Phase 7**: A11y after T014 selection/batch control exists
- **Phase 8**: E2e/screenshot after batch + track reload
- **Phase 9**: After all in-scope receipts (T025 delivery code-remediation cap; plan challenge already closed pre-Gate-1)

### User Story Dependencies

- **US4 (server)**: Phase 2 foundational — before US1 consumer
- **US1**: After Phase 2
- **US2**: After US1 search state exists (same picker files); independent proof covers search outcomes/quota only — does not depend on US3 selection/spend; download/selection-dependent US2 scenarios proved under US3
- **US3**: Creates the selection surface; uses US2 search-outcome display; owns selection-driven spend, over-quota submission, download-superseding observations, expired recovery, current-vs-superseded refresh, read-only batch guard, and every-settled-batch reload for current context
- **US5**: After T014 (selection/batch control)

### Parallel Opportunities

- T003 and T004 can proceed in parallel after T007 proof definitions exist (definitions need not be green)
- T005 must follow T003 for shared `SubtitleEndpoints.cs` edits
- T010 copy can proceed beside T009 once strings are known
- T022 screenshot and T023 CONTEXT are `[P]` after panel exists
- Do **not** mark same-file `SubtitlesTab.tsx` / `useSubtitleAcquisition.ts` edits `[P]` across US1–US3
- T019 is sequential after T014 (no parallel marker)

### Parallel Example: Additive metadata

```text
Task: "Add preferredLanguageCodes on languages DTO path"
Task: "Map hashMatch from nullable ExplicitHashMatch evidence"
```

---

## Implementation Strategy

### MVP First

1. Gate 1 + ADR Accept + DEV-551/DEV-553 pickup verify (plan challenge already closed pre-Gate-1)
2. T007 proof definitions → T003 then T005 (T004 parallel to T003) → T006 schema proof + final green
3. Grouped progressive search UI (US1) with required Accept opt-in, fetch parser, and Q8 availability
4. STOP and validate SC-001/SC-008/SC-011

### Incremental Delivery

1. Provider/quota search summaries (US2)
2. Batch download + publication proof (US3 creates selection; expired recovery; refresh isolation)
3. A11y (after T014) + e2e + screenshot
4. Exact-head gates + Size L review (Q7)

### Notes

- Decisions only from brief/CONCLUSIONS/ASSUMPTIONS; gaps → `needs decision:` in `DRAFTING_RECEIPT.md`
- No new download route; no new dependency; regenerate OpenAPI/types only
- Q3/Q5 taste remains in `ASSUMPTIONS.md`
- Closing bar / caps: `brief.md` Q7 only (unchanged)
- Task IDs T000–T025 preserved
