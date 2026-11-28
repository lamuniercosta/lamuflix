# Requirements Quality Checklist: Subtitle picker grouped search and batch download

**Purpose**: Unit tests for English — validate clarity, completeness, and consistency of DEV-552 requirements before Gate 1
**Created**: 2026-11-28
**Feature**: [spec.md](../spec.md) · [plan.md](../plan.md) · [tasks.md](../tasks.md) · [contracts/search-stream.md](../contracts/search-stream.md)
**Audience**: Spec/plan reviewer (Compass / Gate 1)
**Depth**: Standard (Size L)

## Scope & frozen boundaries

- [ ] CHK001 Spec Frozen scope matches ticket Wanted/Done when/Out of scope without adding or dropping deliverables
- [ ] CHK002 Out-of-scope list explicitly excludes automatic download, deletion, preview, scorer changes, new hashing, new dependencies/projects/layers/schema, LocalPlay/secrets/`Process.Start`
- [ ] CHK003 Q6 file envelope is named and forbids hand-edits to `openapi.json` / `types.ts`
- [ ] CHK004 DEV-551 consumer dependency (including preflight/topology pickup) and DEV-553 track-visibility ownership/pause rule are explicit
- [ ] CHK005 Closing bar (2 plan + 2 remediation; C/H/M + verified acceptance/security/contract failures) matches Q7

## Progressive search & grouping

- [ ] CHK006 Opening-tab search rules cover preferred list; picker owns empty-list → `defaultLanguage` fallback (exactly one auto request when non-null; zero when null); backend owns ordered `preferredLanguageCodes` field
- [ ] CHK007 Concurrent preferred-language searches and progressive per-provider fill-in are required and testable; documented cost is languages × usable providers with conditional hash-to-title second call and retained hash reads
- [ ] CHK008 Language grouping with counts and preference-then-added group order are unambiguous
- [ ] CHK009 Within-language ranking (match percent, download count, release-name tie-break) is preserved
- [ ] CHK010 Row fields (provider, release, match percent, download count, affirmative hash mark only) are complete
- [ ] CHK011 **Add a language** remains available for non-preferred languages

## Stream contract

- [ ] CHK012 Opt-in is `Accept: text/event-stream` on the existing GET with no new route and no provider query param; web uses fetch + in-repo parser + AbortController (no EventSource/reconnect)
- [ ] CHK013 Default JSON semantics/status codes remain unchanged; no-store preserved; `Vary: Accept` declared; both 200 content types; single SSE item carrier
- [ ] CHK014 Drafted wire discriminators `providerSettled` / `searchCompleted`; request-scoped provider snapshot (all-unusable → JSON 409; mixed emits every snapshot provider; terminal count = snapshot size; in-flight set changes belong to next request)
- [ ] CHK015 Close without terminal means incomplete; partial rows kept under availability snapshot; explicit retry only; abort propagates through fan-out/providers/HTTP and stops post-abort fallback
- [ ] CHK016 Pre-stream request-level domain errors, validation, and no-usable 409 never open a stream (DEV-551 semantics retained)
- [ ] CHK017 Named SSE DTOs required in OpenAPI → generated TypeScript; exact check asserts both named schemas, additive field types, and required reused `downloadAvailability` / `SubtitleDownloadAvailabilityDto`; consumer type-check follows wiring
- [ ] CHK033 Q8: required non-null `downloadAvailability` on every `providerSettled` (one stream-open snapshot repeated unchanged); apply to first visible/partial candidates; read-only shows candidates/reason, disables candidate and batch actions, zero POSTs; no new DTO/event/additional search; JSON and download route preserved

## Quota, batch, accessibility

- [ ] CHK018 One provider line with per-language outcomes/safe reasons is specified under overlapping searches
- [ ] CHK019 Last-observed quota, per-provider download-supersedes-search (late search may update outcomes/candidates not that quota), unknown-stays-unknown, and per-provider selected spend (no cross-provider remaining sum) are consistent with ASSUMPTIONS.md Q3
- [ ] CHK020 Selection may exceed last observed quota; route decides each attempt
- [ ] CHK021 Batch snapshot of distinct candidate IDs in visual order; sequential existing-route calls; one active batch per media; read-only disables batch actions
- [ ] CHK022 Row states waiting/done/failure including **expired, search again**; failures do not stop later rows; expired recovery is explicit language re-search + fresh candidate for a subsequent batch
- [ ] CHK023 Exactly one track reload for every settled batch in the current media context (all success, mixed failure, all failed); zero refreshes when media context superseded; selection keyed by candidate ID through reorder; one-active-batch; frozen membership; stale media isolation
- [ ] CHK024 Accessible checkbox labels and announced selection count are required

## Proof & gates

- [ ] CHK025 Web proofs map to SC-001–SC-004/SC-009/SC-011 and listed `brief.md` Web cases; US2 owns search outcomes; US3 owns selection-driven spend / over-quota / download-then-stale-search / expired recovery / refresh isolation / read-only batch guard; tracked test path is `web/src/features/playback/subtitlesTab.test.tsx`
- [ ] CHK026 Integration publication proof requires three files and three tracks in the named existing integration file only (not three HTTP successes; no companion option)
- [ ] CHK027 Browser e2e (two tracks via DEV-553) and two-provider screenshot are required
- [ ] CHK028 Contract proofs cover stream control with exact discriminators, snapshot/all-unusable/mixed/in-flight set change, terminal=snapshot size, gated abort, framing/flush, pre-stream domain/validation/409, legacy JSON, hash generic/OS cases, identical availability snapshot (read-only and enabled), both 200 media types, generated schema before web and consumer type-check after wiring; Phase 2 proof definitions (T007, may stay red) vs final green receipt (T006); T005 follows T003; T003–T006 need not wait for T007 green
- [ ] CHK029 Size L gate matrix matches Q7 / plan Gates: exit 1, Could not run, and unaccepted property exit 2 block; hosted gitleaks; two plan-review rounds (pre-Gate-1) + two code-remediation rounds (T025); explicit dispositions for every other finding; honest SKIPPED/N/A
- [ ] CHK030 Task order: proof definitions → T003 then T005 + generate:api schema proof before frontend consumers; required Accept opt-in + consumer type-check after wiring; picker owns default fallback and parser/read-only proofs in T011; T001 flips ADR after Gate 1
- [ ] CHK031 Q2 hash verification recorded in plan/contracts: nullable explicit-evidence (e.g. ExplicitHashMatch); adapter emits `moviehash` for nonblank hashes (recon:4–7); explicit `moviehash_match` is OpenSubtitles evidence; general public requirement remains explicit affirmative provider evidence; scoring may keep MatchesMovieHash hashSearch fallback
- [ ] CHK032 Taste items Q3/Q5 remain tagged `[assumed]` in ASSUMPTIONS.md and are not silently re-opened as inventable behaviour

## Notes

- Checklist generated by `/speckit-checklist` for Size L requirements quality (not runtime verification / not runtime PASS).
- Drafting pass 2026-11-28: items aligned to brief Q1–Q8, spec FR/SC, plan, tasks, and `contracts/search-stream.md`.
- Plan-challenge repair (final Phase 2 at `e9175fcb`): dispositions 1–9 applied — provider snapshot/preflight, abort propagation, topology cost, quota precedence, ExplicitHashMatch, expired recovery/refresh isolation, SSE transport/OpenAPI dual 200, ADR/sequencing/casing, Patron Q8 downloadAvailability.
- No interactive checklist-intent questions: Conductor ask fixed the artifact set from brief/recon/PRODUCT; depth Standard; audience Gate 1 reviewer; focus frozen picker/stream/batch requirements.
