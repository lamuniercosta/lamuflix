# DEV-554 — Patron conclusions

Grill opened by Keel, 2026-11-28. Sources read: ticket (via recon-DEV-554), note DEV-554, `specs/PRODUCT.md`.

## Standing rulings (pre-grill)

1. **Multiple `.srt` entries in one archive** — take the ticket default: best name match to the release name; ties go to the largest entry.
   - Basis: ticket *Open points for the spec*, bullet 1 (default stated) and *Wanted* 4 ("the spec decides the rule").
2. **Download-count source** — whatever SubDL's own API docs name (header or field), checked on the day; an absent or unparseable count surfaces as `SubtitleQuota.Unknown`.
   - Basis: ticket *Open points for the spec*, bullet 2; *Wanted* 5.
3. **Zip reading uses the BCL `System.IO.Compression.ZipArchive`; no new NuGet package** (care item 1). A third-party archive library would need its own ruling.
   - Basis: `specs/PRODUCT.md` §5 care list 1; ticket *Out of scope* (SRT only, zip only), so the BCL covers the need.
4. **Archive reader lives in `src/LamuFlix.Infrastructure/Adapters/`** as a concrete class beside the adapters, not a new project or layer (care item 2).
   - Basis: ticket *Where it lands* row 1; `specs/PRODUCT.md` §2 (no speculative abstractions).
5. **Superseded by Q1.** The original ruling accepted the key in the query string.
6. **Scope is movies only**; TV search kinds are not declared.
   - Basis: ticket *Wanted* 2; *Out of scope* bullet 1.

## Grill answers

- **Q1 (transport). Accept Keel's recommendation.** Use the v2 API: `GET /api/v2/subtitles/search` (`type=movie` with `tmdb_id`) and `GET /api/v2/subtitles/{nId}/download` in its default zip format. The key goes in the `X-API-Key` header and only to the validated HTTPS `BaseUrl` origin, never in a URI. A redirected or separately hosted archive fetch carries no credential and must hit an allowed download host. No `unpack=1` and no `format=file`. URI/error/log/trace redaction stays as defence in depth. No key is ever committed. `Enabled=true` with a blank key means the provider is off.
  - Basis: the ticket says to check the HolyArrs hints against SubDL's own docs (*Problem*, last sub-bullet). recon-DEV-554:108 shows the v2 header auth and endpoints, so the header is within the ticket, not a change to it.
  - Zip-only keeps *Wanted* 4 (archive reader) and *Done when* bullet 2 meaningful. *Wanted* 1 still requires the search and download host options, which act as an allowlist.
  - `PRODUCT.md` §5 care item 5 (secrets): the ticket's *Wanted* 3 and *Done when* bullet 3 are met more strongly when the key never appears in a URI. *Wanted* 1 ("with no key it is off") covers the blank-key case.
- **Q2 (quota). Accept Keel's recommendation.** Download quota comes from a coherent `X-RateLimit-Limit`/`-Remaining`/`-Reset` snapshot on the v2 download response, and only where SubDL's docs identify those headers as download usage. Search-response counters are search usage and never fill download quota. Absent, malformed, partial or ambiguous values give `Unknown`. The reset time is never inferred, so there is no assumed midnight-UTC reset. There is no `/api/v2/me` call, polling or bootstrap in DEV-554. This refines standing ruling 2.
  - Basis: ticket *Wanted* 5 and *Open points* bullet 2 ("whatever SubDL's docs name; unknown counts show as unknown"); recon-DEV-554:110.
  - Adding a `/me` call would add an unrequested outbound call and change request volume, which the ticket does not ask for. If wanted later, it is a follow-up: `noted, no ticket`.
- **Q3 (shared zip-reader contract). Accept Keel's recommendation.**
  - **Limits:** each caller passes validated limits. SubDL defaults are 8 MiB compressed, 32 MiB total expanded and 128 entries, and the existing SRT payload cap still applies at publication. Every entry counts toward the entry cap. Actual decompressed bytes are bounded across all entries, including ignored extensions, rather than trusting the archive headers.
  - **Rejections:** reject malformed archives, any limit breach, no `.srt` entry, and unsafe names (rooted, drive-qualified, or `..` traversal with `/` or `\`). Each has a distinct reason that is safe to show. Nothing is extracted to disk.
  - **Choosing the `.srt` entry:** first an exact normalized stem match to the candidate release name, then the most shared distinct release-name tokens, then the largest actual size, then ordinal entry-name order, then archive order. The selected bytes pass the existing handler SRT checks.
  - Basis: ticket *Wanted* 4 (caps, paths never used as file paths, SRT checks reused) and *Done when* bullet 2. The ranking follows the ticket default in *Open points* bullet 1 and standing ruling 1; the token tier and the final tie-breaks only make "best name match" and its ties deterministic. The SRT seam is at recon-DEV-554:87.
  - Rejecting the whole archive for a traversal name satisfies *Done when* bullet 2 (`../x.srt` rejected) and stays within the ticket. The cap values are spec detail, not a care-list item.
- **Q4 (pickup and integration boundary). Accept Keel's recommendation.**
  - **Pickup:** DEV-551 is a hard prerequisite. At pickup, confirm that its provider registry, per-provider quota state and shared contract suite have landed, then build on those seams. The recorded WireMock fixtures, cleaned of keys, go where DEV-549 puts fakes. If DEV-549 has not landed by fixture time, confirm the location with the Conductor before writing any fixture. There is no parallel fake framework.
  - **File scope (care item 6):** the work may touch:
    - the new files in `Infrastructure/Adapters/` and `Core/Options/`;
    - only the DEV-551 registration/composition code needed to wire the provider in;
    - the existing integration and unit tests that cover what changes;
    - `README.md`, `docker/api.settings.example.json` and `SubtitleSetupDocumentationTests`, as *Wanted* 6–7 require.
    Any other tracked file needs a fresh ruling.
  - **Out of bounds:** the handler's SRT seam and the public API shape stay as they are, so care item 4 is not triggered. There are no schema, web, new project/layer/dependency, `Features:LocalPlay` or `Process.Start` changes (care items 1, 2, 3 and 5).
  - Basis: ticket *Needs* ("DEV-551. Run after DEV-549 if possible"); *Wanted* 5–7; *Where it lands* table; recon-DEV-554:83–98 and :111 (neither DEV-551 nor DEV-549 artifacts are tracked at HEAD); `PRODUCT.md` §5 care list.
- **Q5 (title-and-year search). Accept Keel's recommendation.**
  - Title and year form one search kind.
  - At pickup, check the current v2 docs and the sanitized fixtures for how the year maps:
    - if v2 documents a year request filter, use it;
    - if not, v2 must document a result-year field, and results are filtered to the requested year before any candidate is returned.
  - The year is never silently dropped, and no candidate with a different year is returned. A contract case covers the same title in a different year.
  - If v2 documents neither, pause the transport work and send a `needs recon` request through the Conductor, then come back for a Patron ruling. The legacy endpoint is not approved here.
  - Basis: ticket *Wanted* 2 (title and year as a search kind) and *Done when* bullet 1 (contract suite passes); recon-DEV-554:108–109 (v2 documents title search; the year filter is documented only in the legacy docs); Q1 ruling (v2 transport).
- **Q6 (delivery evidence). Accept Keel's recommendation.**
  - **Contract suite:** the DEV-551 shared contract suite runs against sanitized, recorded WireMock responses. It covers all three search kinds, the year case, the zip download, and download quota both known and unknown. WireMock is the CI path.
  - **Options:** tests cover enabled with a key and enabled with a blank key (provider off).
  - **Key leakage:** a sentinel key is checked at every log level and on traces and errors, including an upstream failure and a redirected download.
  - **Archive:** tests cover each of the three caps (compressed, expanded, entry count), traversal names, a missing `.srt`, a corrupt archive, the ranking, and rejection by the existing SRT checks.
  - **Property tests** use the repo's existing FsCheck, with no new dependency. They show the choice does not depend on entry order whenever the deciding ranks differ, and they probe each limit at its boundary.
  - **Gates:** the normal size-M backend gates run at build and rebase.
  - **Delivery PR:** it must include a real search on the home cluster with one download, stating the movie, the languages, the candidate count and that the download succeeded, with credentials removed.
  - Basis: ticket *Wanted* 1–7 and *Done when* bullets 1–4; the `task-pipeline` note, Phase 3 step 5 and Phase 5; care item 1 (no new dependency).
- **Q7 (closing bar, frozen scope, round cap). Accept Keel's recommendation; the grill can close.**
  - **Closing bar:** a concrete in-scope Critical or High finding blocks. So does any demonstrated breach of the ticket's acceptance, credential safety or archive safety, whatever its severity. Every other finding is recorded and dispositioned as advisory or follow-up.
  - **Frozen scope:**
    - the movie-only SubDL v2 provider and its options;
    - the secure zip reader, returning through the existing SRT seam;
    - download quota through DEV-551;
    - focused tests and fixtures;
    - setup docs;
    - the home-cluster receipt.
    Anything else becomes a follow-up, not a finding in this round.
  - **Round cap:** two review rounds, with up to two fix commits per round. Round 1 reviews the full diff; round 2 reviews only the fix diff. At the cap, any unresolved blocker goes to the owner on the PR, and the acceptance bar stays in force.
  - Basis: ticket *Wanted*, *Done when* and *Out of scope*; `task-pipeline` standing constraints; `agent-pipeline` Loop Discipline. This is a standing ruling, cited once for this chain.
- **Step 3.3 (Principles VII/VIII and SubDL options). Amend VII narrowly, behind an unchecked owner checkbox. VIII is met with vendor-neutral Core names, so it needs no exception.**
  - **Why VII must be amended:** VII lists the options records as a closed set (constitution.md:782-807). Its subtitle exception covers only `Subtitles:OpenSubtitles` (:788-801). The ticket requires typed `Subtitles:SubDl` options (`Enabled`, `ApiKey`, `BaseUrl`, hosts), and with no key the provider is off (recon-DEV-554:37, :63-64). Nothing in the current text allows either. That is a constitution departure, which charter §2.3(b) sends to the owner. Patron judges it necessary because the ticket text is authoritative. The spec PR therefore carries `blocked: structural — approve the VII amendment adding the SubDL acquisition options exception?` as an unchecked checkbox, and Gate 1 stays closed until the owner ticks it.
  - **What the amendment says (SubDL only; nothing else in VII changes):**
    - The vendor-neutral options record is added to the VII list. It binds `Subtitles:SubDl` as typed options with a validator.
    - SubDL is effectively disabled when `Enabled` is false or the key is blank. In that state it makes no outbound call, it is not an error, and it never fails startup.
    - When enabled but invalid (for example a non-HTTPS `BaseUrl`, a missing host, or a missing or non-positive cap):
      - startup does not fail;
      - SubDL is unusable and makes zero outbound calls;
      - the per-provider state (DEV-551) reports it as misconfigured, with the key redacted.
    - Settings are validated before any outbound use.
    - The archive caps (archive bytes, unpacked total, entry count) are required typed values. Code supplies no defaults, because VII forbids guessed numeric defaults (:800-801). A missing cap makes SubDL invalid. The values go in the example and home config, and the spec states them for the owner to approve.
    - These stay unchanged:
      - OpenSubtitles and other providers;
      - `SaveEnabled` and read-only precedence;
      - existing-track serving;
      - `GET /api/subtitles/languages`;
      - secret isolation.
  - **How the 409 works with several providers (spell this out in the amendment, because it changes behaviour today's text describes):**
    - **Search** returns 409 only when no provider has effective, valid settings. SubDL being off or invalid while another provider is valid is not a 409: the search returns the valid providers' results and shows SubDL's state.
    - **Download** returns 409 when the chosen candidate's provider has missing or invalid settings. The read-only library policy is checked before that configuration failure (:795-796).
    - **What the owner is approving:** today a blank OpenSubtitles key gives 409. After this, the same blank key with a valid SubDL gives SubDL results. The checkbox text must say so.
  - **VIII naming:** Core types are named for what they do, not for the vendor. For example `ArchiveSubtitleProviderOptions` and its validator; the spec picks the final names. The ticket's "SubDL options and validator" (recon:64) names a purpose, not identifiers. The vendor name may appear only:
    - on the Infrastructure adapter (for example `SubDlSubtitleProvider`);
    - in the `Subtitles:SubDl` configuration-key string (:855-857).
    No VIII exception is requested.
  - **Order of steps:**
    1. On the spec PR: the constitution edit, with the version bump and Sync Impact Report required by its amendment procedure, plus the unchecked checkbox.
    2. The owner ticks the checkbox and merges.
    3. T000 runs before any build task and checks, with recorded evidence:
       - the owner checkbox is ticked on the merged spec PR (`gh pr view`);
       - `origin/main:.specify/memory/constitution.md` contains the amended VII clause and the new version line;
       - DEV-551's provider seam and per-provider state are on main. They are absent at the spec HEAD (recon:111), and the 409 rule depends on them;
       - the cap values the owner approved.
       If any check fails, T000 fails and no build starts.
  - **If the owner rejects:**
    - The constitution edit is reverted on the spec branch.
    - The ticket cannot be delivered as written, so DEV-554 goes back to the owner to re-scope under §2.3(a), and the build does not start.
    - Patron offers no workaround. Nesting SubDL under `SubtitleProviderOptions` would still break the `Subtitles:OpenSubtitles` path clause.
  - Basis: constitution VII :782-807, VIII :852-857; recon-DEV-554:37, :63-64, :111; charter §2.3(b) and care item 4. No owner approval is presumed.


## Step 6 plan challenge adjudication

Reviewed `feature/554-spec` at `508f8e48c98d12465d9f418a9c5e86b6b9a61e89`, including Quill's working-tree draft. Evidence is the DEV-554 and recon-DEV-554 notes, the three named axis reports and the DEV-554 planning artifacts. Sentry reported seven findings, Ledger nine, and Compass passed with zero. Ledger's headline severity subtotal accounts for eight entries. The individual headings identify two High, five Medium, and two Low, and all nine are dispositioned below.

| Finding | Disposition and basis |
| --- | --- |
| Sentry F1 (High) | **Blocking draft fix.** FR-004 and the contract's archive-fetch row require allowed destinations and credential isolation. Enforce those guarantees before each request with bounded, manually checked redirect handling. See fix 3. |
| Sentry F2 (Medium) | **Accepted draft fix.** FR-002–FR-004 and Q1 constrain the validated origin, route and search inputs. Add explicit path-segment validation and query encoding in fix 3. The provider-specific identifier shape is pickup evidence if documented. |
| Sentry F3 (Medium) | **Accepted draft fix.** Q3 and FR-006 require a compressed cap before ZIP acceptance. Limit actual stream bytes before opening ZIP. Prove over-cap rejection with a controlled stream independent of HTTP framing. See fix 4. |
| Sentry F4 (Medium) | **Accepted draft fix.** FR-001 and FR-009 require production HTTPS and WireMock coverage. At T001 record an HTTPS fixture or isolated test-only transport that preserves production validation. Recon lines 85–96 identify existing patterns to check, without establishing how the test transport is configured. See fix 5. |
| Sentry F5 (Medium) | **Accepted draft fix.** Q1/Q3 and FR-005/FR-006 require safe redaction and rejection reasons. Use stable reasons and structured safe fields, with regression evidence for discarded external strings. See fix 4. |
| Sentry F6 (Medium) | **Accepted in part.** Require stateless per-call limits, cancellation and disposal across success and rejection. Reader lifetime can follow landed composition because these guarantees do not depend on a transient registration. See fix 4. |
| Sentry F7 (Low) | **Accepted draft fix.** Q2 and FR-008 already prescribe `Unknown` for partial or malformed snapshots. Specify safe parsing and inject `TimeProvider` where time arithmetic is needed. Confirm the documented reset representation at pickup before mapping. See fix 6. |
| Ledger S-1 (High) | **Blocking draft fix.** Step 3.3 explicitly changes the search 409 when OpenSubtitles is blank and SubDL is valid. T000 currently says “unchanged OpenSubtitles.” Reconcile it with the existing singular 409 clause, options list and checklist. Cite DEV-501 / ADR 0041 in the Sync Impact Report. Any separate edit to the ADR requires a cited ruling under Q4. See fix 1. |
| Ledger S-2 (High) | **Blocking draft fix.** Principle VI covers telemetry names. Principle VIII and Step 3.3 require vendor-neutral Core identifiers. Record how runtime provider identity reaches state and telemetry, including test/support types, and enforce the constitution stop for any proposed exception. See fix 2. |
| Ledger S-3 (Medium) | **Accepted in part.** Reuse landed host matching, URI redaction, logging and SRT validation where compatible. Recon lines 83–98 verify the current production patterns and handler seam. Challenger-listed helper names and logging file layout are candidates for T001 verification. See fixes 2 and 5. |
| Ledger S-4 (Medium) | **Accepted draft fix.** FR-001 and Step 3.3 require typed, validated-before-use optional configuration. Carry the registration, validation, test isolation and redirect obligations into the contract. See fixes 3, 5 and 6. |
| Ledger S-5 (Medium) | **Accepted draft fix.** Step 3.3 authorizes the 409 behavior. Q4 limits edits by file. T001 must identify the exact landed paths and obtain cited Patron approval for edits beyond the Q4 registration/composition allowance before T008. Exact landed paths remain pickup evidence under recon line 111. See fix 2. |
| Ledger S-6 (Medium) | **Accepted draft fix.** The plan and T002 currently leave two possible homes for standalone reader tests. Put them and their FsCheck properties in the existing unit project under Q4/Q6. Keep state per invocation and order the work by the reader call shape. See fix 7. |
| Ledger S-7 (Medium) | **Accepted draft fix.** The repository mutation gate enforces both share and span. T014/T017 preserve the actual verdict and require correcting test attribution at a failed share/span gate. See fix 7. |
| Ledger S-8 (Low) | **Advisory.** Keep the approved vendor-neutral options names and the sole `Subtitles:SubDl` binding. Q4 and Step 3.3 already bound this ticket, and DEV-556 is outside its scope. |
| Ledger S-9 (Low) | **Advisory.** Stable IDs and explicit T017-before-T016 dependency are sufficient. T004 verifies the reader/test handoff as an independent checkpoint. Task conventions already cover the explanatory-comment rule. |
| Compass (0) | **PASS.** Preserve the ticket coverage, Q5 stop, Q2 fallback, 409/composition split, health safety and ship-before-live-receipt ordering in the repair. |

### Quill fix list

1. **Principle VII** (Ledger S-1). In `spec.md` FR-015 / Gate 1, `plan.md` VII and `tasks.md` T000, specify that the Spec Kit PR adds the typed options entry, replaces the existing singular 409 clause with the Step 3.3 provider-set search and chosen-provider download rules, and updates the corresponding checklist (Ledger cites constitution lines 782–784, 795–801 and 946–948). Preserve OpenSubtitles binding and validation, `SaveEnabled`, read-only precedence, existing-track serving, languages and secret isolation. State that search aggregation changes for blank OpenSubtitles with valid SubDL. Carry DEV-501 / ADR 0041 impact in the Sync Impact Report, the version bump, the existing unchecked owner checkbox and the T000/rejection checks.
2. **Pickup, edit permissions and Principle VIII** (Ledger S-2/S-3/S-5). In `plan.md` VI/VIII and pickup/structure, `tasks.md` T001/T008 and the contract's Public API section, require the exact landed paths and lines for 409, health and registration. T001 lists which 409 files require edits and which are outside Q4. Record cited Patron approval in `CONCLUSIONS.md` before T008 edits an out-of-envelope file. Trace the provider ID as runtime data into DEV-551 state and telemetry using existing vendor-neutral names, including Core test/support types. A proposed vendor-named Core constant requires a constitution ruling, with charter §2.3(b) governing any necessary departure. Record compatible landed host, URI-redaction, logging and existing SRT seams, with pickup verification for helper names and logging layout.
3. **Guarded transport** (Sentry F1/F2, Ledger S-4). In `spec.md` FR-003/004, `plan.md` transport and verification, `tasks.md` T001/T005/T007/T011 and the external contract, inventory client configuration at pickup and disable automatic redirect following. Keep search on its validated origin. For ZIP redirects resolve and validate each target as HTTPS and allowlisted before requesting it, with a finite hop limit and immediate rejection at the first invalid hop. Send the key only on an eligible initial request to the validated provider origin. Every redirected or separately hosted ZIP fetch is credential-free. Build request paths from a validated single-segment `nId` (honor the documented shape if pickup verifies one) and encode query values structurally. T005 covers allowed multi-hop, disallowed target with zero outbound request to it, hop exhaustion, rejected invalid IDs and title metacharacters. T011 verifies the sentinel and absence of credentials on all followed hops after the existing Phase 3 checkpoint.
4. **Byte bounds, diagnostics and resource ownership** (Sentry F3/F5/F6). In `spec.md` FR-005/006, the plan, the reader/transport contract and `tasks.md` T002/T003/T005/T007/T011, name the bounded actual-byte read before ZIP open. A length header can only provide an early rejection. Keep limits and counters per invocation, propagate cancellation through HTTP and compressed/expanded reads, and dispose response, stream and ZIP resources according to ownership on all exits. Use fixed failure reasons and structured safe fields. Omit raw entry names, URLs, identifiers and quota values from diagnostics. Cover over-cap actual data without a reliable length, distinct rejection, independent concurrent calls, cancellation/disposal on rejection and absence of untrusted marker text from reasons, logs, traces and errors. Keep the existing SRT publication seam.
5. **Typed options and test transport** (Sentry F4, Ledger S-3/S-4). In `plan.md`, `tasks.md` T001/T006/T007 and the external contract, reuse the landed typed subtitle options and HttpClient patterns where compatible. Keep this optional provider out of startup validation, apply the typed validator before outbound use and use typed section binding. T001 records an HTTPS WireMock configuration or an isolated test-only transport seam. T005/T006 must prove production configuration rejects non-HTTPS with zero outbound calls. Use the landed structured logging convention and required movie ID context where available.
6. **Quota parsing** (Sentry F7, Ledger S-4). In the contract, plan and `tasks.md` T001/T005/T007 require documented attribution and reset representation before mapping. Reject malformed, negative, overflowing or incoherent values to `Unknown` without throwing. Use injected `TimeProvider` for any relative reset calculation. Add header regression cases, keeping search counters separate and zero `/api/v2/me` calls.
7. **Test placement and gates** (Ledger S-6/S-7). In the plan and `tasks.md`, place standalone ZIP tests and FsCheck properties in `tests/LamuFlix.UnitTests/ZipSubtitleArchiveReaderTests.cs` with repo naming and property gate discovery conventions. Keep normal-composition SRT evidence in T009 and WireMock/health/composition in integration. Remove the T002/T003 parallel marker and examples, order T003 before T002, and keep T004 as verification. Preserve caller-supplied caps and avoid mutable static state. T014 and T017 record the actual mutation verdict and require correcting deficient tests on share/span failure while retaining configured thresholds.

**Phase 2 status:** All findings adjudicated. Quill's repair and Keel's reread are required before plan freeze and step 7. Gate 1 remains closed pending the Spec Kit PR with the Principle VII amendment and unchecked owner checkbox, then owner approval and merge.

## Step 6 freeze receipt

**PLAN FROZEN (2026-11-28).** Keel froze the Phase A spec, plan, and tasks from the independently reread working-tree package on `feature/554-spec`, based at `508f8e48c98d12465d9f418a9c5e86b6b9a61e89`. This receipt supersedes the drafting status labels.

Read-only `/speckit-analyze` cleared the repaired package. The 18 tasks cover all 15 functional requirements and 7 success criteria. The seven adjudicated repairs are reflected in the package, including T005/T007 and the transport contract for the HTTP compressed-byte bound before buffering. The private plan and tasks match the worktree copies byte for byte.

Ready for Phase 2 step 7 commit and draft PR preparation. Include the narrow Principle VII amendment, constitution version bump, Sync Impact Report, and unchecked owner checkbox in the draft PR. Gate 1 remains closed until the owner ticks the checkbox and merges. T000 still requires the recorded merge, amended constitution on main, landed DEV-551 seams, and approved cap values before Build.

## Gate 1 ruling (2026-11-28, HEAD 45ff03c1)

**gate1: provisional, not approve. The constitution amendment must land on `feature/554-spec` before Rigger marks PR #523 ready.**
- The owner ticked the VII checkbox on PR #523. That approves the Step 3.3 amendment as worded in the PR's Patron recommendation, including the caps of 8 MiB compressed, 32 MiB expanded and 128 entries. The owner's word is final, so this is not re-opened.
- `git diff origin/main...HEAD` touches only `specs/DEV-554/`. `.specify/memory/constitution.md` is unchanged. Step 3.3 order item 1 and T000 require the edit on this spec PR: add the options entry at :782-784, replace the 409 clause at :795-801, update the checklist at :946-948, bump the version, and add a Sync Impact Report citing DEV-501 and ADR 0041. The owner approval and T000 force this edit, so it is in scope. Quill drafts it with `/speckit-constitution`, Keel rereads it, then Rigger marks the PR ready and sets YouTrack Gate 1. The owner merges.
- In the same commit, fix the stale `tasks.md:9` status ("awaiting Keel reread") to match the freeze receipt. Analyze is clean, the plan challenge is adjudicated and the User-Visible Outcome items are ticked, so `spec.md` Status is now `gate1: provisional`.
