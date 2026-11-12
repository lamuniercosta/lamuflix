# DEV-501 — Explicit subtitle search and download

Status: Phase A grill closed after nine questions; Patron confirmed shared understanding in Q8 and ratified closure in Q9. This is the decision source for Quill, not a spec, plan freeze, Gate 1 approval, implementation receipt, or runtime acceptance.

Plan-challenge closure (2026-11-12): **PLAN FROZEN** after catalog/D1–D5/Principle V rulings, bounded document repairs and Keel's independent read-only consistency analysis. The receipt is `PLAN_ADJUDICATION.md`; Gate 1 is not approved and runtime receipts remain OPEN. Source-inventory verification remains mandatory at pickup/before affected changes under D4, not claimed complete by this freeze.

## Authority and provenance

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-501-spec`, branch `feature/501-spec`.
- Facts: Maestri `recon-DEV-501`, lines 8–96; intake HEAD `9c1898ac2b5a0c2f05056ce738c3ae7e1bd33865`. No independent code investigation was performed by Keel.
- Decisions: Patron Q1–Q9, full answers in [CONCLUSIONS.md](CONCLUSIONS.md); taste details also in [ASSUMPTIONS.md](ASSUMPTIONS.md).
- ADR 0041 assigned by Conductor; draft at `docs/adr/0041-explicit-subtitle-acquisition.md`.
- Existing `harness.yml` modification predates this work and remains untouched.

## Loop discipline

Closing bar: all verified Critical, High, Medium and Low contract, safety and acceptance findings block closure. Unnecessary abstractions become follow-up issues. Review round cap: two; unresolved blockers are reported at the cap, never waived by exhaustion. Size L adjudication requires Sentry, Ledger and Compass evidence and explicit dispositions.

Frozen scope: language search and explicitly selected subtitle download in the player CC Subtitles tab; OpenSubtitles behind the existing provider seam; matching and ranking; Subs-only guarded atomic non-overwriting SRT writes; local-track activation/persistence; quota/reset, timeout and read-only UI states; complete prepare-only flow removal; DTO/OpenAPI/generated TypeScript contracts; tests and ticket-required runtime receipts. No SubDL, new dependency, player library, architectural layer, database schema, server delay/style persistence, or local-player execution change. Keep media-chrome. Anything else is a follow-up issue, not a finding in this round.

## Every grill answer

1. **Q1 — Loop and scope:** Patron approved the closing bar, two-round cap and exact ticket scope, citing recon:8–82, PRODUCT and pipeline. Round exhaustion does not satisfy acceptance.
2. **Q2 — Constitution:** Patron ruled the prepare-specific Principle VII wording update is consistent maintenance mandated by owner ruling 5, not a new structural escalation. Retain typed options, secret isolation and acquisition-vs-local-serving separation. Plan the constitution amendment/version procedure and explain it in ADR 0041; no owner checkbox arose from this ruling. This records Patron's interpretation, not a completed amendment.
3. **Q3 — Contract:** Patron approved the envelopes/statuses below and bounded in-memory, server-bound candidate IDs. Restart or expiry means stale selection: reject before a paid call and ask the viewer to search again. No persistence or new dependency.
4. **Q4 — Ranking/language:** Patron approved distinct-token Jaccard scoring, hash override, deterministic ties, bilingual language labels, first preferred language default, explicit choice when none, no paid automatic retry, and configurable five-minute TTL/1000-entry process cap.
5. **Q5 — IO/selection:** Patron approved server-derived names, containment checks before folder/file writes and paid download, collision spelling without spaces, payload validation, UI serialization and session-safe activation. Q7 supersedes Q5's direct-create/no-temp-cleanup mechanics.
6. **Q6 — UI:** Patron approved local tracks/Off above results, accessible release details, truthful nullable quota, local-time reset display, inline failure/read-only reasons and playback continuity; Palette supplies the design before plan challenge.
7. **Q7 — Atomicity correction:** Retain private guarded staging and non-replacing atomic publication; retry the next filename on collision. Never overwrite/delete existing or published library files. Cleanup only task-owned transient staging artifacts. This is Patron's reconciliation of the ticket's no-delete language with its express atomic-write reuse requirement, not an owner scope change.
8. **Q8 — Plan and acceptance:** Patron approved the ordering, file boundary, tests and gates below and confirmed shared understanding. Runtime receipts remain OPEN; grill closure proves none of them.

9. **Q9 — Precision and closure:** Patron ratified midpoint rounding away from zero, leading-dot `.srt` staging with explicit discovery exclusion tests, retention of shared SaveEnabled for read-only policy, and Q7 precedence; grill closed with shared understanding. Basis: recon:22–25,38–39,46–48,56–58,88 and PRODUCT.

## Vocabulary

- **Subtitle Candidate:** a remote search result; never an installed local track merely because it appears in search.
- **Candidate ID:** opaque process-local selection token bound server-side to movie, language and provider candidate; not a path, URL or raw provider file ID.
- **Local Subtitle Track:** a discovered subtitle, including a successfully committed downloaded SRT in Subs; selectable independently of acquisition availability.
- **Subtitle Match Percent:** moviehash 100, otherwise rounded distinct-token overlap as defined below; no extra confidence claim.
- **Subtitle Download Availability:** server-reported acquisition write capability/reason, separate from free search and existing-track playback.

These qualified terms refine this feature's language; D4 below authorizes their additive root glossary entries after Gate 1. Quill must use them consistently without rewriting existing exact-basename serving terms.

## Contract decisions

### Plan-challenge language ruling (Patron; supersedes Q4 ambiguity)

Source: `CONCLUSIONS.md`, "Plan Challenge Adjudication — Compass F1". Approve `GET /api/subtitles/languages` returning 200 `SubtitleLanguagesResponse(string? DefaultLanguage, IReadOnlyList<SubtitleLanguageOptionDto> Languages)` as `{defaultLanguage, languages:[{code, englishName, nativeName}]}`. A single in-process supported-language catalog in `LamuFlix.Core.Features.Subtitles` supplies canonical codes and bilingual names and validates search languages and configured preferences. Put configured preferences first in configured order, deduplicated; then remaining supported entries by ordinal English name and code. Default is the first preference, or null. Empty preferences are valid and must not produce acquisition 409; an explicit supported-language selection can search with otherwise valid provider settings.

The catalog endpoint uses private no-store, makes no provider call, and requires no provider key. The UI consumes generated DTOs, never a parallel handwritten catalog. A non-null default starts free search; null prompts without search. Add catalog, configuration-validation and UI tests to the existing language tasks and carry the contract through C# DTO → OpenAPI → generated TypeScript. Extend the planned API/Core file surface only for this approved catalog and DTO work. This ruling resolves the decisions behind Compass F1/F2; drafting propagation and verification are still required before closure.

`GET /api/movies/{id}/subtitles/search?language=` returns 200:

```text
{ candidates: [{ candidateId, language, releaseName, matchPercent, downloadCount }],
  quota: { remainingDownloads, resetAtUtc },
  downloadAvailability: { enabled, reason } }
```

`POST /api/movies/{id}/subtitles/download` accepts only `{candidateId}` and returns 201 `{track, quota}`. Reuse the existing local-track DTO (Patron names `SubtitleTrackDto`); do not create web duplicates. C# DTO → OpenAPI → generated TypeScript is mandatory. Neither API accepts a client path or URL. Both acquisition responses use `Cache-Control: private, no-store`.

Statuses: 400 invalid movie ID/language/body or stale/unknown/cross-movie candidate; 403 access-policy/containment refusal; 404 missing movie; 409 missing/invalid acquisition configuration or disabled/read-only POST; 429 provider quota exhaustion; 502 provider failure; 504 timeout. Errors expose safe reasons and known quota/reset metadata, never credentials. Read-only GET still searches with valid provider configuration and returns disabled download availability. POST refuses disabled writes before provider download.

Quota fields are nullable when provider values are unknown. Display only provider-supplied values; never infer zero, reset time, or replenishment. Acquisition configuration failure must not break host startup or local listing/content/playback; retain applicable typed validation and secret protections (Q2).

Extend existing provider/domain metadata as needed for release names and quota rather than exposing OpenSubtitles details through public routes. Use a bounded, thread-safe in-process candidate binding with injected TimeProvider; configurable defaults are five minutes and 1000 entries. Stale tokens after eviction, expiry or restart produce 400 without a paid download. No database-backed token store or new secret scheme is decided.

## Ranking and interaction

Moviehash matches score 100. Otherwise invariant-case-fold release name and video stem; split at non-Unicode-letter/digit boundaries, deduplicate tokens, compute `round(100 * intersection / union)`; empty union is 0. Descending match percent, descending download count, ordinal release name, then candidate ID settle ties. Use `MidpointRounding.AwayFromZero` consistently in implementation/tests (Q9).

Validate canonical language codes server-side against supported/configured language choices. Show English and native names, default to first configured preferred language; with no preference, prompt for explicit selection without issuing a search. Language change initiates free search, ignores stale responses and preserves playback/active track. Never auto-download. A paid request follows one explicit candidate selection; disable additional selections while it runs and never retry it automatically.

Local tracks, including Subs, precede remote results; Off and local switching remain usable in all acquisition states. Show truncated release name with full value on hover/focus, match percent, downloads and active check only for the actual active downloaded track. Successful POST refreshes the local list and activates the track only for the matching current movie/session. Completion after a language switch must not replace the newer remote results. Quota/reset/read-only/provider failures are inline and do not stop video. Unknown quota is shown as unavailable; known UTC reset is displayed in local time without implying restoration.

## Filesystem and removal

Derive movie folder/video stem from trusted server metadata and use validated canonical language code. Final sequence: `<movie folder>/Subs/<stem>-<code>.srt`, then `<stem>-<code>(1).srt`, `<stem>-<code>(2).srt`, etc. Create Subs if missing. Refuse resolved escapes and unsafe symlink/reparse paths before any write, including directory creation; repeat relevant protection at publication. Read-only checks happen before paid download; permission races fail safely and do not pretend to refund provider quota.

Validate SRT before staging; reject unsupported content without adding conversion dependencies. Preserve atomic staging and non-replacing publication. Collision retries reuse the staged payload and never overwrite the competing file. Staging remains private inside guarded Subs, hidden from discovery; use a leading-dot `.srt` staging name to satisfy the extension requirement. Ensure discovery excludes staging, and test it: Q7's description of current filtering/move/link internals is Patron-provided design context, not an independently established recon fact. Cleanup is limited to owned transient staging artifacts; existing/published media or subtitles are never deleted. Failure must not expose a partial subtitle as a local track.

Remove prepare route, handler/response, auto-pick/Film.srt behavior and settings/tests/references that exist only for prepare. Preserve shared provider client, moviehash, local list/content and safe atomic mechanisms. Prepare returns 404. Do not delete a shared setting by inference: Q3 uses SaveEnabled for read-only policy, so retain it as the shared read-only policy switch (Q9). Q7 governs any conflict with Q5.

## Planned file surface

Planning outputs owned here: this brief, CONCLUSIONS, ASSUMPTIONS and ADR 0041. Quill owns spec/plan/tasks; Palette owns design. Later implementation may modify:

- `src/LamuFlix.Api/Endpoints/SubtitleEndpoints.cs`, existing subtitle DTOs and `src/LamuFlix.Api/HandlerRegistration.cs`; introduce search/download request/response types in the existing feature shape.
- Existing Core subtitle feature directory, `ISubtitleProvider`, `SubtitleCandidate`, and sidecar port as necessary for explicit search/download, quota, scoring and token binding; no new architectural layer.
- `src/LamuFlix.Infrastructure/Adapters/OpenSubtitlesSubtitleProvider.cs`, `src/LamuFlix.Infrastructure/FileSystem/SubtitleSidecarStore.cs`, and existing discovery code only where required for safe downloaded/staged tracks.
- Remove `PrepareSubtitleCommandHandler.cs`, `SubtitlePrepareResponse.cs`, `PrepareSubtitleCommandHandlerTests.cs`, `SubtitlePrepareServeEndpointTests.cs`; update shared settings/registrations/tests only to remove prepare-only behavior while preserving other coverage.
- Existing subtitle endpoint/provider/store/concurrency tests and OpenAPI contract tests identified in recon:86–89; add tests beside them using repository conventions.
- `web/src/features/playback/BrowserPlayer.tsx`, `SubtitleSelector.tsx`, related subtitle UI/tests and CC integration after dependency pickup; `web/src/mocks/handlers.ts`.
- Regenerate `web/src/api/openapi.json` and `web/src/api/types.ts`, never hand-edit generated contracts.
- `.specify/memory/constitution.md` scoped prepare-reference maintenance and associated version procedure, and prepare-only `harness.yml` references where applicable. Do not overwrite the pre-existing harness modification.

Exact new filenames and dependent CC/discovery integration must be mapped by Quill using recon; absence of a needed authoritative fact requires `needs recon`, not guessed code facts. No production or test files were edited in this grill.

## Task ordering and gates

1. Quill drafts spec/plan/tasks from this brief and proposed ADR, including scoped constitution maintenance; obtain Palette design before plan challenge. Run read-only cross-artifact analysis, repair by bounded rounds, challenge/adjudicate, then request owner Gate 1. These are separate steps.
2. Before implementation, verify CC panel/Subs discovery dependency readiness and Phase B drift against main. Amend planned tasks where authorized; do not rebuild prerequisite tickets inside DEV-501.
3. Provider metadata/quota and bounded candidate binding; ranking/search query and explicit download command; guarded atomic store; endpoint/error/DTO integration and prepare-only removal. Build meaningful tests alongside each seam.
4. Generate OpenAPI/TypeScript, then use web-implement for the CC UI and state transitions after backend contract settles.
5. Run appropriate repository verification and three-axis review before PR handoff; two-round cap and closing bar remain binding. Never merge.
6. Perform required home-cluster acceptance after the home-cluster dependency lands. Preserve open status when execution cannot occur.

Required later validation: Roslyn, complexity, inspectcode, .NET tests, property tests, vulnerable-package scan, format, applicable mutation gate, and web gates. Use actual script interfaces and changed scope. Never lower thresholds or substitute a build for unavailable gates. `SKIPPED (scope-empty)`, opt-out `SKIP`, mutation `N/A`, and `Could not run` are distinct from PASS. Existing baseline skips verify no implementation behavior.

## Test strategy and runtime evidence

- Unit: hash and overlap/empty tokens/ties, language validation/defaults, candidate expiry/cap/restart/cross-movie rejection with TimeProvider, no automatic paid retries.
- Provider HTTP fixtures: metadata, selected download, quota/reset, missing data, failure and timeout. Use real driver/container behavior if a test actually depends on driver translation/serialization; no new database is part of this ticket.
- Real filesystem: containment and symlink/reparse escape refusal before directory/file creation; missing Subs creation; exact collision sequence, concurrent no-overwrite publication, private staging/discovery exclusion, partial-write failure and owned-staging-only cleanup.
- API: GET/POST envelopes/statuses, 201 track persists through list/content, read-only GET with POST refusal before paid call, stale token without paid call, unavailable config retains local serving, and prepare 404 with obsolete references removed.
- Vitest/RTL and browser: ordering, labels/details, language default/change, stale responses/session activation, explicit-only downloads, disabled read-only action, quota/reset/timeout/error messages with uninterrupted video and local selection.

Runtime acceptance remains **OPEN**: configured-key real-movie pt-BR search, explicit choice saves BS Player layout and plays the new subtitle, BS Player itself recognizes the file, and quota exhaustion/provider timeout/read-only states preserve video. Use copied real movie folders as ticket requires; machine-specific fixture locations are runtime inputs, not committed configuration. Home-cluster receipts follow its prerequisite ticket. Anything unexecuted is `Could not run`, never PASS; synthetic/HTTP fixture success does not replace these receipts.

## Plan-challenge decisions and bounded repair direction

Round 1 reports: Sentry (Risk), Ledger (Standards), Compass (Spec). Final dispositions and plan-freeze receipt belong in `PLAN_ADJUDICATION.md`. Patron approved D1–D5 in `CONCLUSIONS.md`; the operative directions below supersede the earlier pending-decision language. This records decisions before drafting repairs; it does not grant Gate 1.

- Acquisition success and mapped error responses on GET and POST must carry private no-store; explicitly test both. Invalid acquisition configuration produces 409 on either acquisition route when required provider configuration prevents execution; empty preferred languages alone are valid under the Patron catalog ruling.
- Make Subs protection checkpoints explicit: before directory creation, after creation and before staging, and immediately before each publication attempt. Tests must exercise changed-path refusal and non-overwrite; these checks do not claim to eliminate every operating-system path race.
- Retain Q9's explicit staging discovery exclusion tests; never claim existing filtering is verified from a Patron description.
- Complete prepare removal with an explicit repository-wide code/settings/generated-contract verification step, classifying historical documentation and negative 404 assertions separately from live prepare dependencies. D4 approves the delete/update/retain table below and deliberately assigns additional unverified live references to T026 verification before deletion; do not delete shared behavior by name alone.
- `harness.yml` references are established by recon:86, not hypothetical. Coordinate a minimal prepare-only edit with the owner of the existing dirty change; do not stash, reset or overwrite that change. Regenerate affected harness outputs through the repository installer; never edit rendered gate configuration or add an exemption merely to pass a gate.
- Sequence prepare removal after shared endpoint/registration edits, then regenerate final contracts and review/approve produced OpenAPI snapshots through the normal snapshot workflow. Do not hand-edit snapshots or run concurrent edits to the same files.
- Keep ADR 0041 Proposed during planning. D4 approves constitution 1.15.0 → 1.16.0 and ADR 0041 acceptance together after Gate 1, with ADR 0041 cited in the Sync Impact Report and Last Amended updated. Supersede only ADR 0038's prepare-route wording, preserving its Proposed status and provider/read-write separation. No new binder port means no Principle I port-list amendment; preserve Principle VI telemetry conventions when retiring prepare-only signals.
- Request-driven cancellation must not be reported as provider timeout or guessed quota consumption; add cancellation-versus-timeout proofs without inventing a new public status.
- Reject the assertion that UI serialization implies API idempotency. Q5/Q7 already allow concurrent no-overwrite publication; neither ticket nor Q1–Q9 promises content deduplication or one paid call across distinct explicit requests. No singleflight/AlreadyPresent contract is approved by this review.
- Reject a predicted complexity failure as evidence requiring new guard/pipeline abstractions. Use existing seams and measured complexity gates; do not invent types solely to satisfy a predicted score.

### D1 — Quota and errors

Quota is provider-observed data, nullable when absent; no claim that a failed call was charged/refunded without provider evidence. Convert provider relative durations/Retry-After with injected `timeProvider.GetUtcNow().Add(duration)`. POST completes local prechecks before a paid call: missing movie 404, invalid/stale binding 400, read-only policy 409 before missing/invalid provider configuration 409, containment 403. Unknown or positive last-known quota permits the explicit paid call after prechecks; known exhaustion refuses with download-quota 429. No elapsed-reset inference replenishes quota. GET search requires valid provider configuration (409 `provider_not_configured`), but read-only with valid configuration returns 200 and `{enabled:false, reason:"read_only_library"}`. The catalog endpoint is independent and key-free.

Non-2xx acquisition responses use RFC 7807 ProblemDetails with `extensions["reason"]` and optional `extensions["quota"]={remainingDownloads,resetAtUtc}` when known. Approved reasons: `invalid_request`, `stale_candidate`, `access_denied`, `movie_not_found`, `read_only_library`, `provider_not_configured`, `search_rate_limited`, `download_quota_exhausted`, `provider_unavailable`, `provider_timeout`. No credentials, paths or raw provider text. GET 429 means free-search rate limiting, never proof of exhausted paid quota; copy: “Subtitle search is temporarily rate-limited. Try again in a moment.” POST 429 means download quota; copy: “Download quota reached. Resets at {localTime}.” or “Download quota reached.” when reset is unknown; disable Get. Local serving and playback continue.

### Principle V alignment — Patron final status clarification

Source: `CONCLUSIONS.md`, Cross-Artifact Alignment — Principle V. Preserve Principle V unchanged: actual FluentValidation `ValidationException` from the validation decorator returns **422** `invalid_request`; **400** is malformed HTTP/route/query/JSON binding or domain/token precheck rejection outside that exception. This clarification supersedes Q3/D1/D2 shorthand that categorized all invalid inputs as 400. Error mapping remains in the single centralized `IExceptionHandler` using `AddProblemDetails()` and includes `traceId`; never globally remap ValidationException or return raw exception text from endpoints. Add explicit GET/POST 422 contracts and handler/API tests and generate web contracts accordingly. Parameterless catalog is unaffected unless a validator applies. No constitution departure or owner checkbox is introduced.

### D2 — Binding and canonical codes

Use concrete `SubtitleCandidateBinder` and `SubtitleMatchScorer` in `LamuFlix.Core.Features.Subtitles`; binder DI lifetime is process-local Singleton, thread-safe, no additional interface/port/layer/distributed store or mutable static initialization. On existing `SubtitlesOptions`, `CandidateTokenTtl` defaults to `00:05:00` and validates > zero, `CandidateStoreMaxEntries` defaults to 1000 and validates >= 1; preserve acquisition-startup-safe validation. Mint opaque 32-hex-character/128-bit tokens under lock using injected seeded Random and TimeProvider per Patron D2; do not relabel this as cryptographically secure randomness. Purge `expiresAtUtc <= now` on insertion, then if full evict earliest expiry with FIFO tie-break; new search issuance succeeds. Expired/evicted/restart-lost/cross-movie selections return 400 `stale_candidate` before paid calls. Process-local state is intentionally not shared across replicas; route to the issuing process or treat an unavailable binding as stale and require free re-search, without claiming affinity is implemented here.

`SupportedSubtitleLanguages` is the single Core catalog. Trim query/preference code whitespace, match with `StringComparison.OrdinalIgnoreCase`, emit its canonical Code in DTOs, bindings, provider requests and filenames. Empty/unknown search codes return 400 `invalid_request`. Empty preferred-language list remains valid; preference entries must resolve to supported codes. Do not hand-maintain web catalog duplicates.

### D3 — Resource bounds and cancellation

Enforce the approved `MaxSubtitleDownloadBytes` default 2 MiB (`2 * 1024 * 1024`) while reading the provider body, not after unbounded buffering; invalid/non-SRT or oversized payload returns 502 `provider_unavailable` before staging, with no invented quota charge/refund. Express the approved cap and `MaxSubtitlePublicationAttempts` default 10,000 as typed configuration values on existing subtitle options with positive validation, not gate thresholds or unexplained inline literals. These property names carry the approved configurable bounds in the existing options surface; no dependency or layer is added.

Reuse one private leading-dot `.srt` staging file across next-free non-replacing publication attempts; count the unsuffixed slot as the first attempt, then `(1)` onwards. Recheck containment before create, after creation/before stage and before every publication attempt. Stop on request cancellation; at the approved 10,000-attempt default return 409 `read_only_library` and clean up only owned staging in finally. Propagate caller cancellation through search/body read/staging/publication; caller cancellation is not provider timeout or a quota narrative. Only upstream timeout while caller request remains active maps to 504 `provider_timeout`.

### D4 — Authorized file and governance surface

| Disposition | Planned surface |
|---|---|
| Delete confirmed prepare-only artifacts | `PrepareSubtitleCommandHandler.cs`, `SubtitlePrepareResponse.cs`, `PrepareSubtitleCommandHandlerTests.cs`, `SubtitlePrepareServeEndpointTests.cs` (older recon:86; revalidate current existence before changes) |
| Update shared references only | `SubtitleEndpoints.cs`, `HandlerRegistration.cs`, `harness.yml` preserving dirty edits, OpenAPI contract tests/snapshots, regenerated `web/src/api/openapi.json` / `types.ts`, `web/src/mocks/handlers.ts`; additional live references require the T026 sweep before deletion |
| Retain shared mechanisms | `ISubtitleProvider`, `OpenSubtitlesSubtitleProvider`, `SubtitleSidecarStore`, `SubtitleSidecarDiscovery`, `ListSubtitleTracksQueryHandler`, `GetSubtitleSidecarQueryHandler`, `SubtitlesOptions` including SaveEnabled and PreferredLanguages, `SubtitleProviderOptions` |
| Add scoped glossary terms | `CONTEXT.md`: Subtitle Candidate, Candidate ID, Local Subtitle Track, Subtitle Match Percent, Subtitle Download Availability; do not rewrite existing Movie, Library or exact-basename sidecar serving entries |
| Post-Gate-1 maintenance | scoped Principle VII 1.15.0 → 1.16.0, ADR 0041 acceptance together, ADR 0038 prepare-route relationship only; key-free catalog and valid empty preferences explicit |

Current recon:113–130 establishes only the cited inspected facts and explicitly leaves other inventories unverified. Older recon and reviewer file lists are candidate locations, not proof of current completeness. T003/T004/T006/T025/T026 must verify current paths, shared usage, exact installer/snapshot workflow and live references before modifying them; missing facts then stop as needs recon. D4 deliberately permits these implementation-pickup checks without treating them as new undecided product scope. No pre-emptive deletion or broad historical-document rewrite is authorized.

### D5 — Active indicator

Active check belongs only to the actually active downloaded track in LocalTrackList. Remove result-row `[x]/[ ]` and CandidateRow active-check wording; candidates have only explicit Get actions. No release-name/file-name heuristic matching, candidate-to-track persistence or added identity mapping. This is Patron's cited reconciliation of ticket result selection with its downloaded-local-track flow.

### Final drafting and test directions

Use qualified glossary names above while retaining established DTO/identifier names. Scorer/ranking property tests are applicable: bounds, hash override, deterministic ordering and overlap symmetry/empty behavior. Use existing FsCheck/Shouldly/NSubstitute/WireMock conventions and real filesystem fixtures, with exact current paths verified at pickup; no guessed SurvivorTests suffix mandate. Pin new `SubtitleSearchDownloadEndpointTests.cs` and functionality-named scorer/binder tests in the existing test projects; do not invent a missing existing test as fact. T026 must discover the applicable installer and snapshot approval interfaces from the current checkout before invocation, not assume `scripts/install.ps1` exists. No third challenge round is introduced by these repairs; gate/runtime/merge boundaries remain separate.
