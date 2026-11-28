# Implementation Plan: Subtitle picker grouped search and batch download

**Branch**: `feature/552-spec` (worktree `F:/Dev/LamuFlix.worktrees/feature-552-spec`) | **Date**: 2026-11-28 | **Spec**: `specs/DEV-552/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q8); `CONCLUSIONS.md`; `ASSUMPTIONS.md` (Q3, Q5); Proposed ADR `docs/adr/0053-incremental-subtitle-provider-results.md`; Maestri notes `DEV-552`, `recon-DEV-552`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only). Plan-challenge dispositions items 1–9 at HEAD `e9175fcb`.

**Status**: Draft — Quill drafting only. Owner Gate 1, Build, and runtime acceptance remain pending. Quill does not declare PLAN FROZEN.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `contracts/search-stream.md`, `checklists/requirements.md`, `checklists/subtitle-picker-batch.md`, `DRAFTING_RECEIPT.md`. No `research.md` / `data-model.md` / `quickstart.md` — grill Q1–Q8 freeze inventable unknowns; no schema change; additive API contract under `contracts/`.

## Summary

Deliver the player Subtitles-tab picker that searches preferred languages concurrently with opt-in progressive per-provider SSE on the existing search GET (ADR 0053), surfaces additive `preferredLanguageCodes`, `hashMatch`, and stream `downloadAvailability` (Q8), groups candidates by language with provider rows and quota summaries, and downloads many selected candidates sequentially through the existing single-candidate route with one batch-end track reload for the current media context — under the Q7 closing bar and Q6 file envelope.

## Technical Context

**Language/Version**: C# / .NET 10 (`LamuFlix.Api`, `LamuFlix.Core`, `LamuFlix.Infrastructure`) + Vite/React/TypeScript under `web/` per `specs/PRODUCT.md` §1.

**Primary Dependencies**: Built-in ASP.NET Core 10 SSE (`TypedResults.ServerSentEvents`); existing OpenAPI → TypeScript `generate:api` chain; existing xUnit/web/browser test stacks. No new NuGet/npm dependency (Q1; `PRODUCT.md` §5 item 1).

**Storage**: No database or migration change (Q6).

**Testing**: Focused subtitle unit tests; `SubtitleSearchDownloadEndpointTests` stream/JSON/download coverage; tracked `web/src/features/playback/subtitlesTab.test.tsx` (+ hook behaviour via tab tests); browser e2e + screenshot; three-file/three-track publication proof. Never treat three HTTP 200s as publication proof (`brief.md` Test strategy).

**Target Platform**: Local API + web Compose/home-library player panel.

**Project Type**: Existing ports-and-adapters .NET solution + React player feature folder.

**Performance Goals**: Progressive UI bounded by per-provider deadlines already owned by DEV-551 fan-out. Documented call/I/O cost is concurrent preferred languages × usable providers, with the existing conditional hash-to-title fallback adding another provider call where that path applies (recon-DEV-552:4–7). Retain existing hash reader and one concurrent request per preferred language. No cross-request hash sharing, throttling, or changed search concurrency in this ticket. Documented topology does not claim measured performance (disposition item 2/3).

**Constraints**: Charter §2.3 care-list items deliberately ruled (Q1–Q8). No LocalPlay/secrets hardcoding/`Process.Start`. Honest gate labels; never lower thresholds; never merge. Size L requires Risk (Sentry), Standards (Ledger), Spec (Compass). Closing bar and caps: `brief.md` Q7 (unchanged).

**Scale/Scope**: Size L; picker UI + additive languages/candidate/availability fields + opt-in SSE on existing search + sequential client batch download. Parent chain via ticket; ADR 0053 claimed.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No structural owner checkbox (`PRODUCT.md` §5): Patron Q1–Q8; no ticket contradiction; no constitution departure. Ordinary owner Gate 1 remains the Spec Kit PR merge path. ADR 0053 acceptance is in the delivery task sequence after Gate 1; it is not a Gate 1 substitute.

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation | **PASS** — keep existing `ISubtitleProvider` fan-out; share server fan-out between JSON aggregate and SSE representation; name nullable explicit-evidence on the existing candidate result (for example `ExplicitHashMatch`) separate from scoring while preserving `MatchesMovieHash` scoring/ranking fallback; public `hashMatch` requires affirmative evidence (Q1, Q2, Q6; disposition item 3/5). No new port/layer. |
| II (Explicit Handlers) | Handlers + AddHandler | **PASS** — adapt existing language/search handlers and endpoint mapping; no mediator/repository layer. |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — no Movie mutation. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — request-scoped provider preflight; pre-stream request-level domain errors, validation, and 409 stay existing JSON ProblemDetails behaviour with retained DEV-551 semantics; download errors unchanged (Q4; disposition item 1). |
| VI (Observability) | Safe diagnostics | **PASS** — reuse DEV-551 safe closed outcome/reason codes; no upstream message/credential logging. |
| VII (Configuration / LocalPlay / Process.Start) | Options; secrets; LocalPlay gated | **PASS** — no LocalPlay/`Process.Start`/secrets hardcoding; preferences remain `Subtitles:PreferredLanguages` (Q6). |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — add **Hash Match** only if needed for the affirmative mark distinct from Subtitle Match Percent; keep existing Subtitle Candidate / Candidate ID / Local Subtitle Track meanings (`brief.md` Files). |
| IX (Test Pyramid) | Meaningful layers | **PASS** — unit + integration stream control + web + browser e2e + publication proof (`brief.md` Test strategy). |

### Stack / API / workflow checks

| Check | Plan response |
|---|---|
| Tech stack / API docs | **PASS** — additive public DTO fields and named SSE payload DTOs only (Q1–Q2, Q4, Q8); reuse `SubtitleDownloadAvailabilityDto` on settled events; regenerate openapi.json → types.ts; no new route; download untouched (Q1, Q6). Single `SseItem<T>` carrier; both 200 content types; `Vary: Accept`; preserve no-store (disposition item 5/7). |
| Static-analysis gates | Every changed `.cs` task completes only after Roslyn, cyclomatic complexity, and InspectCode pass on those files; full delivery matrix per `brief.md` Gate expectations / Q7. Keep `SKIPPED`, `N/A`, and `Could not run` distinct from PASS. |
| Web gates | Lint/test/build + OpenAPI/TypeScript drift; picker tests at `subtitlesTab.test.tsx` and browser suite for stated cases. |
| Mutation | Applicable production C# under `src/` expects mutation under harness policy; record PASSED / FAILED / SKIPPED (scope-empty) / N/A honestly. |

### Complexity Tracking

No constitution violations requiring justification. SSE is an alternate representation of the existing search operation; batching stays client-side on the existing download route.

## Approach

- **Spec Kit drafting → Gate 1**: Spec Kit artifacts drafted; Keel analyze / plan challenge follow the pipeline; Spec PR under ordinary Gate 1 must include ADR 0053 as **Proposed**; Build starts after Gate 1 merge receipt and pickup clearance. Quill does not claim freeze or Gate 1.
- **ADR**: Keel-owned ADR 0053 remains Proposed through Spec PR. After separate Gate 1, T001 flips status to Accepted before claiming stream behaviour merged; leave ADR 0052 intact as the multi-provider JSON/fan-out basis (disposition item 6/8).
- **Pickup (T002)**: Verify DEV-551 landed consumer contract (provider identity, `providers[]`, closed states), **preflight** (no-usable → 409), and fan-out/hash-read topology against pinned recon evidence. Verify DEV-553 track-refresh path for acceptance. Stop `needs recon:` / scoped Patron ruling if either gap blocks Done when (Q6; disposition items 1–2).
- **Provider-set snapshot (disposition item 1)**: Capture configured provider identities and initial eligibility before shared fan-out. Zero usable → JSON 409 before SSE. Opened stream emits one outcome per snapshot provider; terminal count equals snapshot size; configured-set changes belong to the next request.
- **Abort propagation (disposition item 2)**: Carry `HttpContext.RequestAborted` through shared fan-out, provider calls, and outbound HTTP; keep existing deadlines; stop post-abort title fallback. T007 gated-provider proof.
- **Q2 hash verification (draft-time fact; `recon-DEV-552` current lines 4–7)**: Handler reads file hash per language search; OpenSubtitles adapter emits `moviehash` for nonblank hashes. Explicit `moviehash_match` is OpenSubtitles’ evidence. Port scoring keeps `MatchesMovieHash` as `moviehash_match ?? hashSearch`. Name nullable explicit-evidence (for example `ExplicitHashMatch`) on the existing candidate result through `ISubtitleProvider`, separate from scoring. Public `hashMatch` true only on affirmative evidence; missing/false/no-signal/title-fallback → false. T004/T007 cover generic mapping and OpenSubtitles adapter cases without a new adapter or live provider proof (disposition item 3/5).
- **Quota precedence (disposition item 3/4)**: Per-provider download observation precedence. Late in-flight search may update outcomes/candidates but not that quota; a subsequent eligible search may update quota. T016: download → stale search → newer eligible search.
- **Expired recovery / refresh isolation (disposition item 4/6)**: Expired row → explicit language re-search + fresh candidate for a subsequent batch; original batch continues. Track refresh exactly once for current media context; zero for superseded context. T014–T016.
- **Q8 downloadAvailability (disposition item 9 / repair item 7)**: Required non-null `downloadAvailability` using existing `SubtitleDownloadAvailabilityDto` on `SubtitleProviderSettledEventDto`. One snapshot at successful stream open, repeated unchanged on every `providerSettled`. Apply to first visible candidates including partial rows after interruption. Read-only shows candidates/reason, disables candidate and batch actions, zero POSTs. No new DTO/event/additional search. Preserve JSON and download-route contracts.
- **SSE transport (disposition item 5/7)**: Built-in SSE writer with immediate per-event delivery; supported response-path buffering must not delay provider progress; preserve no-store; declare `Vary: Accept`; single SSE item carrier for both concrete DTOs; both 200 media types. Web: `fetch` + streaming body + in-repo parser + `AbortController`; no reconnect. T011: split chunks, ignored unknown events, close without terminal, first-event read-only. T006: both media types, named schemas, JSON compatibility, required reused availability type. T007: wire progress/framing, flushed first event, disconnect/abort, availability invariant.
- **Exact generated contract check (`brief.md` Task ordering 1)**: After backend regenerate, assert both named schemas (`SubtitleProviderSettledEventDto`, `SubtitleSearchStreamCompletedDto`), additive field types (`preferredLanguageCodes`, `hashMatch`), and required reused `downloadAvailability` / `SubtitleDownloadAvailabilityDto` in regenerated OpenAPI/`types.ts`. Backend generation/schema proof precedes web consumption. After the web consumer is wired, verify it type-checks against both generated payload types. Drafted wire event names (Quill) are `providerSettled` and `searchCompleted` (`contracts/search-stream.md`).
- **Task order (`brief.md` Task ordering; disposition item 6/8)**:
  1. Establish server contract (languages `preferredLanguageCodes`, candidate `hashMatch`, SSE DTOs + OpenAPI registration including Q8 availability) and wire tests / generated schemas **before** frontend consumers. Phase 2: T007 proof definitions first (may stay red); T003 then T005 for shared `SubtitleEndpoints.cs` edits (T004 may parallel T003); T006 final green + generation before frontend. T003–T006 may start once definitions exist and need not wait for T007 to pass. Picker owns empty-preference → `defaultLanguage` fallback (T008/T011).
  2. Keep current JSON and single-download behaviours covered while adding streaming.
  3. Build grouped search + provider summaries on the verified contract, then selection + sequential download state, quota freshness/spend, accessibility, expired recovery, and single batch-end track reload for current context (including all failed) with zero reload when superseded.
  4. Same-language publication proof, then e2e + screenshot with two providers enabled and real track flow.
  5. Size L gates and three-axis review under Q7 caps.
- **Do not**: add batch download route; auto-download/delete/preview; scorer/hashing projects; new dependencies/layers/schema; hand-edit openapi/types; touch LocalPlay/`Process.Start`/secrets; invent Gate 1 / plan freeze / delivery PASS from this draft.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-552/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md
├── tasks.md
├── contracts/search-stream.md
├── checklists/requirements.md
├── checklists/subtitle-picker-batch.md
└── DRAFTING_RECEIPT.md
docs/adr/0053-incremental-subtitle-provider-results.md  # Keel-owned; Proposed on Spec PR; T001 Accepts after Gate 1
```

### Source Code (authorized envelope — Q6)

```text
web/src/features/playback/SubtitlesTab.tsx
web/src/features/playback/useSubtitleAcquisition.ts
web/src/features/playback/subtitleCopy.ts
web/src/features/playback/subtitlesTab.test.tsx
web/e2e/   # existing browser suite extensions for stated cases
src/LamuFlix.Api/Endpoints/SubtitleEndpoints.cs
# existing subtitle language query/DTO, search handler, candidate/response models/DTOs
# named stream DTOs beside existing endpoint DTOs (reuse SubtitleDownloadAvailabilityDto)
# focused ISubtitleProvider result mappings/adapters for explicit hash evidence + OpenAPI registration
tests/LamuFlix.UnitTests/Features/Subtitles/
tests/LamuFlix.IntegrationTests/SubtitleSearchDownloadEndpointTests.cs
web/src/api/openapi.json   # regenerate only
web/src/api/types.ts       # regenerate only
CONTEXT.md                 # only if Hash Match glossary term required
```

**Structure Decision**: Extend existing subtitle endpoint/handler/DTO and player picker files; no new project or top-level folder.

## Phase 0 — Research outcomes (frozen by grill; hash fact recorded)

| Topic | Decision | Basis |
| --- | --- | --- |
| Progressive results | Opt-in SSE on existing GET | Q1; ADR 0053 |
| Metadata | `preferredLanguageCodes` + non-null `hashMatch` | Q2 |
| Empty preferences | Search `defaultLanguage` when present; else none | Q2 amendment |
| Stream end | Explicit terminal + settled count equals request snapshot size; wire `providerSettled` / `searchCompleted` | Q4; disposition item 1 |
| Batching | Client sequential existing route | Q5; ticket Open points default |
| Hash evidence | Nullable explicit-evidence on candidate result; public `hashMatch` true only on affirmative evidence; scoring keeps `MatchesMovieHash` fallback | Q2; recon-DEV-552:4–7; disposition item 3/5 |
| Availability | Required reused `downloadAvailability` snapshot on every `providerSettled` | Q8; disposition item 9 |
| SSE DTO names | `SubtitleProviderSettledEventDto`, `SubtitleSearchStreamCompletedDto` (`settledProviderCount`); drafted wire names `providerSettled` / `searchCompleted` | brief Approach #3 drafting (Q1 freezes opt-in SSE + named DTOs) |
| Call topology | Concurrent languages × usable providers; conditional hash-to-title second call; retained hash reads | disposition item 2/3; recon:4–7 |

## Phase 1 — Contracts

See `contracts/search-stream.md`. Confirm OpenAPI emission of named SSE DTOs and required reused availability type; register explicitly if needed (Q1/Q8). Exact generated contract check: both named schemas + additive field types + `downloadAvailability` after regenerate; consumer type-check after wiring.

## Verification mapping

| Proof | Owner tasks (see tasks.md) |
| --- | --- |
| Stream first-event-before-second, snapshot outcomes, terminal=snapshot size, all-unusable/mixed/in-flight set change, interrupt, gated abort (no post-abort fallback), framing/flush, pre-stream JSON (domain/validation/409), legacy JSON, download unmodified, hash generic/OS cases, catalog ordered preference field, identical availability snapshot, generated schema | T007 proof definitions → T003 then T005 / T004 → T006 final green |
| Both 200 media types, named schemas, JSON compatibility, required reused availability type | T006 |
| Consumer type-check; fetch parser + AbortController; required Accept; apply availability; pre-stream JSON handling | After wiring (T008) |
| Empty-preference fallback; split chunks; unknown events ignored; close without terminal; first-event read-only / zero POST | T011 |
| Two providers × two languages grouping, selection identity, three sequential downloads, failure/expiry isolation + expired recovery, interrupted-stream recovery, stale media isolation (zero refresh), a11y | Web tasks |
| Search outcomes / known-unknown quota freshness (US2 independent proof) | T012/T013 (US2) |
| Selection-driven spend, over-quota submission, download-then-stale-search-then-eligible-search, all-failed reload, one-active-batch, frozen membership, media-change batch isolation / zero superseded refresh, batch guard under read-only | T014/T016 (US3) |
| Three distinct published files + three tracks | T017 in named integration file |
| Two-track e2e via DEV-553 + two-provider screenshot | Browser/screenshot tasks |
| Size L gates + three axes under Q7 | Polish / review tasks |

## Gates

Apply standing Size L pipeline at implementation time (`brief.md` Gate expectations). Record standing exit-2 exceptions by real verdict. Exit 1, `Could not run`, and an unaccepted property-test exit 2 block. Final hosted checks include gitleaks. Intake baseline at `6bd8be67` is a separate receipt (DEV-552:9). Closing bar (Q7, unchanged): two plan-review rounds (pre-Gate-1 plan challenge) and two code-remediation rounds (delivery, T025 after T024) maximum; Critical/High/Medium block; verified acceptance/security/contract failures block at any severity; disposition every other finding explicitly.
