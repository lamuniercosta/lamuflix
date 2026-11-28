# DEV-554 SubDL provider and shared archive reader

Phase 2 grill close. Size M. Worktree `F:/Dev/LamuFlix.worktrees/feature-554-spec`, branch `feature/554-spec`.

Sources are the `DEV-554` ticket as recorded in `recon-DEV-554:19–75`, the repository handoff in `recon-DEV-554:77–112`, `specs/PRODUCT.md` §§2–5, and Patron's recorded [conclusions](CONCLUSIONS.md). Provider documentation was verified in recon against the [SubDL developer docs](https://subdl.com/developers) and [legacy API docs](https://subdl.com/api-doc).

## Review loop

- **Closing bar.** A concrete in-scope Critical or High finding, or a demonstrated ticket acceptance, credential-safety or archive-safety violation at any severity, blocks. Record a disposition for every other finding as an advisory or follow-up under the charter.
- **Frozen scope.** One movie-only SubDL v2 provider and options, three search kinds (IMDb ID, TMDb ID, title and year), secure ZIP download, one shared bounded ZIP-to-SRT reader through the existing validation seam, documented download quota in the DEV-551 provider state, focused tests and sanitized fixtures, setup documentation, and the ticket's home-cluster acceptance receipt. TV, alternative archive formats, YIFY integration and unrelated improvements belong outside this scope. The scope ends here: anything else is a follow-up issue, not a finding in this round.
- **Round cap.** Two review rounds, up to two fix commits per round. Round one reviews the full diff. Round two reviews the remediation diff. Unresolved blockers at the cap go to the owner on the PR, with the closing bar still in force.

## Approach and file scope

| Surface | Allowed change |
| --- | --- |
| `src/LamuFlix.Infrastructure/Adapters/` | Add the SubDL adapter and the concrete reusable BCL `ZipArchive` reader. Make the minimum DEV-551 registration/composition change required to register SubDL. |
| `src/LamuFlix.Core/Options/` | Add typed `Subtitles:SubDl` options and validation for enablement, key, HTTPS base URL, search and download allowlists, and bounded archive settings. Use the existing options pattern. |
| `tests/LamuFlix.IntegrationTests/` and focused existing unit tests | Run the DEV-551 shared suite against SubDL, test options/composition and diagnostics, and exercise the archive reader and the normal SRT validation path. Use sanitized recorded WireMock fixtures in the DEV-549 location when available. |
| `README.md`, `docker/api.settings.example.json`, `tests/LamuFlix.UnitTests/SubtitleSetupDocumentationTests.cs` | Document settings, off-without-key behavior and approved credential channels. Keep the example key empty and cover setup documentation with the existing test. |

The reader returns selected SRT bytes. `DownloadSubtitleCommandHandler` retains its existing size check, validation and publication path. The DEV-551 provider contract and public API shape govern registration and state, with no new routes, DTO fields or status codes. No schema, web, package, project, layer, local playback or process execution changes are in scope. Any additional tracked file requires a fresh ruling.

The v2 adapter searches at `GET /api/v2/subtitles/search` and requests `GET /api/v2/subtitles/{nId}/download` in ZIP mode. It accepts the requested movie search kind and preserves the year for title-and-year searches under Q5. Send `X-API-Key` only to the validated HTTPS provider origin. Check download origins against the allowlist and omit credentials on redirected or separately hosted archive fetches. Keep diagnostics safe at the request and error boundaries. Enabled with a blank key is off.

Enforce the compressed-size cap before accepting the archive, and entry count plus actual expanded-byte limits while reading it. Treat entry names as data. Reject unsafe names anywhere in the archive, and select the SRT deterministically using Q3. Surface only documented download-usage quota through DEV-551. Missing or ambiguous evidence produces `Unknown`.

## Task order and pickup constraints

1. At Phase B pickup, confirm DEV-551 has landed and locate its provider registry, quota state and shared suite. Verify current v2 title-and-year mapping and capture the applicable documentation beside the contract fixture evidence. Use the Q5 stop condition if the year mapping is unavailable. Refresh the allowed file list against the landed seams before code work.
2. Implement and verify the standalone archive reader and its bounds and ranking invariants. Establish the sanitized recorded fixture location before writing fixtures. DEV-549 is preferred for placement. If it is unavailable at fixture time, obtain the Conductor's location confirmation through the Q4 process.
3. Bind and validate options, then implement guarded search, archive download and quota mapping. Run the shared contract suite with the recorded fake. Wire the adapter into DEV-551 and verify disabled configuration, per-provider state and the existing SRT publication path through normal composition.
4. Update the setup docs and sample config and run the documentation test. Run the size-M backend gates and review on the implementation diff. Record the home-cluster receipt on the delivery PR and verify the rebased head through the normal ship gates.

## Test and acceptance evidence

- **Provider contract.** Cover IMDb ID, TMDb ID with movie type and title plus year. Verify same-title/different-year disambiguation, ZIP download, and coherent and unknown download quota cases, including search counters staying separate. Cover provider enablement and blank credentials.
- **Archive safety.** Cover compressed, expanded and entry cap breaches, including the zip-bomb and too-many-entries cases, `../x.srt` and the other unsafe path forms, no SRT, malformed ZIP, multi-entry ranking and invalid selected SRT through the existing checks. Assert distinct safe failure reasons. Use the existing FsCheck for limit boundaries and selection independence from entry order when the ranking criteria decide the outcome.
- **Credential safety.** Exercise a credential sentinel and capture logs across all levels, traces and surfaced errors. Include upstream failure and redirected download. Assert no leak and no credential on the redirected or separately hosted fetch.
- **Gates.** Apply `task-pipeline` Phase 3 step 5 and Phase 5 to the delivery diff. Expect Roslyn, cyclomatic complexity, InspectCode, property tests, vulnerable packages, format and `dotnet test`. Run mutation for the production C# scope at the harness threshold and preserve its actual verdict. Record web gates as `SKIPPED (scope-empty)` if the delivery diff has no web files. Require Risk, Standards and Spec review reports and use exact-head gate results at ship.
- **Runtime.** Attach a real home-cluster movie search and one successful download to the delivery PR. Record movie, languages, candidate count and successful download, with credentials scrubbed. Automated CI uses WireMock.

## Standing rulings and grill exchanges

The standing archive choice and placement remain in force: `System.IO.Compression.ZipArchive`, no new package, a concrete reader beside the Infrastructure adapters and no new layer. Q3 refines the standing multi-SRT rule. Q2 refines the quota rule. Q1 replaces the initial query-string authentication ruling. The movie-only standing ruling is unchanged. Patron recorded all seven answers in `CONCLUSIONS.md`.

### Q1

> [from Keel] DEV-554 Q1 — Which documented SubDL transport should the spec use now that recon-DEV-554:108–110 supersedes the HolyArrs hints? Recommend the current v2 search and ZIP download endpoints with X-API-Key on the validated HTTPS provider origin, movies and the three ticket search kinds only, no unpack/file mode, and no credential on a redirected or separately hosted archive fetch. This explicitly revisits the standing query-string ruling: the ticket asks us to verify the hints and keep the key out of diagnostics, and the v2 header avoids credentials in URIs. Keep defensive redaction for errors, logs and traces, forbid committing keys, and make Enabled=true with a missing key effectively disabled. Please rule with cited basis and confirm that I should append the exchange to specs/DEV-554/CONCLUSIONS.md.

Patron approved v2 search and ZIP download using `X-API-Key` only at the validated HTTPS `BaseUrl` origin. Redirected or separately hosted archive fetches use allowed download hosts and carry no credential. Preserve defensive redaction, keep credentials out of git, and treat enabled with a blank key as off. Use the three movie search kinds, with `type=movie` for TMDb. Use the default ZIP mode. This supersedes the standing query-string ruling. Basis is ticket Problem and Wanted 1, 3 and 4, `recon-DEV-554:108` and `specs/PRODUCT.md` §5 item 5.

### Q2

> [from Keel] DEV-554 Q2 — How should SubDL populate DEV-551 download quota? Recommend mapping a coherent X-RateLimit-Limit/Remaining/Reset snapshot from the v2 download response only where the official documentation identifies that response as download usage. Search response counters are search usage. Leave download quota Unknown on absent, malformed, or ambiguous values, with no inferred midnight reset and no extra /me polling or bootstrap call in DEV-554. This follows the ticket Wanted 5/Open points and recon-DEV-554:110 and keeps request volume unchanged. Please give a cited ruling. I will append the full exchange myself unless you record it.

Patron approved download quota from a coherent, documented download-response `X-RateLimit-Limit`, `X-RateLimit-Remaining` and `X-RateLimit-Reset` snapshot. Search counters are search usage. Absent, malformed, partial or ambiguous snapshots map to `Unknown`. Derive reset only from the documented response and make zero `/api/v2/me` calls. Basis is ticket Wanted 5 and Open points bullet 2, and `recon-DEV-554:110`.

### Q3

> [from Keel] DEV-554 Q3 — What is the shared ZIP-reader contract? Recommend validated limits passed by each caller, with SubDL defaults of 8 MiB compressed, 32 MiB total expanded, and 128 entries, plus the existing effective SRT payload cap at publication. Count every entry and bound actual decompressed bytes across all files, including ignored extensions. Reject malformed, over-cap, missing-SRT and unsafe names (rooted/drive-qualified or parent traversal with either separator) with distinct safe reasons, and never extract to disk. Rank supported names by exact normalized stem match to the candidate release name, then by shared distinct release-name tokens, then by largest actual size, with stable ordinal entry-name order then archive order for remaining ties. Apply the existing handler SRT checks to the selected bytes. Basis: ticket Wanted 4, Done when bullet 2, Open points bullet 1, recon-DEV-554:87. Please rule on this contract.

Patron approved caller-passed validated limits, with SubDL defaults of 8 MiB compressed, 32 MiB expanded in aggregate and 128 entries. Count all entries and actual expanded bytes, including ignored formats. Reject malformed ZIPs, cap breaches, missing SRT and unsafe names with distinct safe reasons. Rooted and drive-qualified names and parent traversal using either separator invalidate the archive. Never extract to disk. Rank exact normalized stem match first, then shared distinct tokens with the release name, largest actual byte size, ordinal entry name and archive order. The existing handler still caps and validates selected SRT bytes. Basis is ticket Wanted 4, Done when bullet 2, Open points bullet 1 and `recon-DEV-554:87`.

### Q4

> [from Keel] DEV-554 Q4 — What is the pickup and integration boundary given recon-DEV-554:83–98,111? Recommend making DEV-551 a hard implementation prerequisite (verify landed registry, quota state, shared contract suite at pickup, then use those seams). Prefer landed DEV-549 placement for sanitized recorded WireMock fixtures. If DEV-549 is unavailable at fixture time, reconfirm the applicable fixture location with the Conductor before writing fixtures; no parallel fake framework. Allow only the new Infrastructure/Adapters and Core/Options files, the DEV-551 registration/composition touch needed to wire the provider, focused existing integration/unit tests and the README/settings example/setup-doc test edits required by Wanted 6–7. Preserve the handler SRT seam and current public API shape; no schema, web, new project/layer/dependency, Features:LocalPlay, or Process.Start changes. Please rule including charter 2.3 file scope and public API.

Patron made DEV-551 a hard pickup prerequisite. Use its actual registry, per-provider quota state and shared suite. Put sanitized recorded fixtures in the DEV-549 location when landed. Otherwise, confirm the applicable location with the Conductor before fixture work. Use one fake framework. Scope is new Adapters and Options files, the minimal DEV-551 registration touch, focused tests and the documented setup files. Any other tracked file needs a fresh ruling. Reuse the current SRT handler and public API. Schema, web, new project/layer/dependency, `Features:LocalPlay` and `Process.Start` are out of scope. Basis is ticket Needs, Wanted 5–7 and Where it lands, `recon-DEV-554:83–98,111` and `specs/PRODUCT.md` §5.

### Q5

> [from Keel] DEV-554 Q5 — How should the spec keep the required title-and-year search precise? Recommend recording the movie title and year as a single search kind. At pickup, check current v2 docs and sanitized fixtures for the exact year mapping: use a documented year request filter when supported, otherwise require a documented result-year field and filter to the requested year before returning candidates. Never silently drop the year or return a conflicting one. Require a same-title/different-year contract case. If neither documented mapping exists, pause the transport implementation for recon and a Patron ruling rather than weakening the ticket; no new legacy endpoint is approved here. Basis: ticket Wanted 2 and Done when bullet 1; recon-DEV-554:108–109 (v2 title evidence, explicit year in legacy docs). Please rule.

Patron confirmed title and year are one search kind. Verify the current v2 year mapping at pickup. Prefer a documented request-year filter. A documented result-year field can support filtering before publication. Year must participate and a conflicting year cannot be returned. Test two movies with the same title and different years. If v2 documents neither mapping, stop transport work for recon through the Conductor and a Patron ruling. The legacy API has no approval under this ruling. Basis is ticket Wanted 2 and Done when bullet 1, `recon-DEV-554:108–109` and Q1.

### Q6

> [from Keel] DEV-554 Q6 — What verification evidence must the delivery PR carry? Recommend the DEV-551 shared contract against sanitized recorded WireMock responses (all three search kinds, year disambiguation, ZIP download, known and unknown download quota); enabled/blank-key option coverage; sentinel leakage tests at every log level and on traces and errors, including an upstream failure and redirected download; archive coverage for compressed/expanded/entry caps, traversal, missing SRT, corruption, ranking and existing SRT rejection; exercise order-independent choice whenever decisive ranks differ and limit boundaries with property tests. Run the normal size-M backend gates at build/rebase. Keep the ticket-required home-cluster real movie search and one download as delivery-PR acceptance with movie, languages and candidate count plus successful download evidence, scrubbed of credentials. WireMock is the CI path. Basis: ticket Wanted 1–7 and Done when 1–4, task-pipeline Phase 3 step 5 and Phase 5. Please rule.

Patron approved the WireMock contract cases, no-leak sentinel coverage, archive regressions, existing FsCheck for selection and limit invariants, and normal size-M gates. CI uses the fake. The delivery PR needs a real home-cluster movie search and one successful download with languages and candidate count and scrubbed credentials. Basis is ticket Wanted 1–7 and Done when 1–4, and `task-pipeline` Phases 3 and 5.

### Q7

> [from Keel] DEV-554 Q7 — Can we close the grill on this review contract? Recommend that a concrete in-scope Critical/High finding or any demonstrated ticket acceptance, credential-safety or archive-safety violation blocks regardless of severity. Record and disposition every other finding as advisory/follow-up per the charter. Freeze scope at the movie-only SubDL v2 provider and options, secure ZIP return through the existing SRT seam, download quota in DEV-551, focused tests/fixtures, setup docs and the required home-cluster receipt; anything else is a follow-up issue, not a finding in this round. Cap review at two rounds and two fix commits per round. Round one checks the full diff, round two the remediation diff. Surface unresolved blockers to the owner in the PR at the cap and keep the acceptance bar in force. Basis: ticket Wanted/Done when/Out of scope, task-pipeline standing constraints, agent-pipeline Loop Discipline. Please confirm the closing bar, frozen scope, round cap and grill close.

Patron confirmed the closing bar, frozen scope and two-round cap stated above. In-scope Critical/High findings with concrete scenarios and demonstrated acceptance, credential-safety or archive-safety violations block. Everything else is advisory or follow-up. Round one covers the full diff and round two covers the fix diff, with up to two fix commits per round. Surface unresolved blockers to the owner on the PR at the cap and keep the bar in force. Patron confirmed grill close. Basis is ticket Wanted, Done when and Out of scope, `task-pipeline` and `.cursor/rules/agent-pipeline.mdc` Loop Discipline.

## Step 3.3 governance ruling

Patron recorded this ruling in `CONCLUSIONS.md` at `508f8e48`. It supersedes Q3 only on where the three SubDL cap values are set and supplements Q4 on configuration, Gate 1, and existing-route status behavior. Basis is Constitution VII and VIII, charter §2.3(b) and care item 4, and `recon-DEV-554:37,63–64,111`.

- Amend Principle VII narrowly for `Subtitles:SubDl`. Bind a vendor-neutral typed Core record and validator. `Enabled=false` or a blank key means disabled without error, host startup failure, or outbound calls. Enabled, keyed settings that fail validation also permit host startup, make no outbound calls, and report a redacted misconfigured state through DEV-551. Validate before any outbound use.
- The compressed-byte, expanded-byte, and entry-count caps are required positive typed values. Missing values mean misconfigured. Use the Q3 values of 8 MiB, 32 MiB, and 128 in the example and home configuration, subject to owner approval. The shared reader takes caller-supplied limits and code supplies no fallback values.
- Search returns 409 only if no provider has effective, valid settings. With a valid SubDL configuration and a blank OpenSubtitles key, search returns SubDL results. If SubDL is disabled or misconfigured and another provider has valid settings, search returns the valid providers' results and shows SubDL's state. Download returns 409 when the chosen candidate's provider has missing or invalid settings, after applying read-only library precedence. Preserve other provider validation, `SaveEnabled`, existing-track serving, the key-free languages route, and secret isolation.
- Use vendor-neutral names for the Core options and validator under VIII. Keep the vendor name on the Infrastructure adapter and in the `Subtitles:SubDl` configuration key.
- The spec PR carries the narrow amendment, version bump, Sync Impact Report and the unchecked checkbox below. T000 verifies the owner tick and merged spec PR, the amended VII clause and version on `origin/main`, DEV-551's landed provider seam and per-provider state, and the owner-approved cap values. All checks must pass before Build. If the owner rejects the amendment, revert it on the spec branch and return to the owner to re-scope the ticket under §2.3(a).

- [ ] blocked: structural — approve the VII amendment adding the SubDL acquisition options exception? With the approved caps and valid SubDL settings, search returns SubDL results even when the OpenSubtitles key is blank. Search returns 409 only when no provider has effective, valid settings.


## Step 6 plan challenge refinements

The [step 6 adjudication and Quill fix list](CONCLUSIONS.md#step-6-plan-challenge-adjudication) govern the next draft. The Q7 closing bar and scope still apply. Step 3.3 preserves OpenSubtitles configuration and validation while changing 409 aggregation on the existing surfaces. The Principle VII draft must amend the options list, the 409 clause and its checklist, with the DEV-501 / ADR 0041 relationship recorded in the Sync Impact Report. Gate 1 requires the same unchecked owner decision and merged amendment.

- T001 records the landed transport, reuse and provider identity seams, the test-only HTTPS strategy and the exact 409 files. Patron must cite approval for any 409 edit outside Q4's registration/composition allowance before T008 touches that file. Provider identity in state and telemetry is runtime data under the existing vendor-neutral Core rules.
- Q1 transport now explicitly requires bounded manual redirects, HTTPS and download allowlist validation at every applicable hop, credential-free redirected and separately hosted archive fetches, and structured URI construction. A test-only transport may support WireMock while the production HTTPS validator remains strict.
- Q3 compressed caps apply to actual bytes read before ZIP open. Keep per-invocation state, propagate cancellation and dispose resources on rejection. Safe diagnostics use fixed reasons and approved structured values. Q2 parsing must fail to `Unknown` for malformed or incoherent snapshots, with injected `TimeProvider` for any relative reset calculation.
- Standalone reader and FsCheck coverage belongs in the existing unit test project. T003 establishes the reader call shape before T002 consumes it, and T004 remains the phase checkpoint. WireMock, health and normal-composition evidence remain integration tests. T014 and T017 preserve the configured mutation share/span limits.

## Step 6 freeze

The Phase A spec, plan, and tasks are frozen. The [freeze receipt](CONCLUSIONS.md#step-6-freeze-receipt) records the read-only analysis and mirror verification. Phase 2 step 7 may prepare the commit and draft PR. Gate 1 remains closed until the owner ticks the Principle VII checkbox and merges the Spec Kit PR.
