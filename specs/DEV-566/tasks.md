---
description: "Task list for DEV-566 subtitle download-host fail-closed guard"
---

# Tasks: Subtitle download-host fail-closed guard

**Input** — Design documents in `specs/DEV-566/`: spec, plan, brief, conclusions. Authority includes `ticket-text.txt`, notes `DEV-566` and `recon-DEV-566`, and `specs/PRODUCT.md`.

**Status** — **PLAN FROZEN**. Build tasks depend on the verified owner Gate 1 merge receipt. See [plan adjudication](PLAN_ADJUDICATION.md).

Work in the file boundary in plan.md and brief.md. Obtain a cited Patron ruling for other tracked rewrites or protected surfaces. Follow the dependencies and phase order below. `[P]` means parallelizable once its prerequisites are complete, in disjoint files. Sequence matches brief Approach 1–5 and Q8: options/validator and fake configuration → runtime latch work → cluster generator/assertions → live checker/workflow edits → automated verification → separate opt-in receipt.

## Phase 0: Gate 1

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares and opens the DEV-566 spec PR. Only the user merges. **Keel** records the verified owner merge receipt in `specs/DEV-566/CONCLUSIONS.md`. All Build tasks depend on this receipt.

## Phase 1: Read-only pickup

- [ ] T001 After T000, **Wisp** verifies the picked HEAD against plan/recon. Check options validation, both handlers’ `EnsureProviderConfigured` acquisition branches and 409 mapping, adapter link refusal, URI host policy, singleton registration, static readiness, home-cluster generator and test, and both implement-skill copies. Determine whether either skill copy is generated and identify its canonical source and regeneration path. Inventory loopback/fake fixtures configuring `DownloadAllowedHosts`. Report `needs recon:` for a missing required fact. Keep pickup read-only.

## Phase 2: User Story 1 - Fail-closed static download-host configuration (Priority: P1) 🎯 MVP

**Goal**: `DownloadLinkHost` validated against allowlist; invalid config → `409` on search/download with zero provider calls.

**Independent Test**: Validator + acquisition-settings unit tests and isolated endpoint composition/search-download tests prove `409`, no candidates, zero upstream calls.

### Tests for User Story 1

- [ ] T002 [P] [US1] After T001, extend `tests/LamuFlix.UnitTests/Options/SubtitleProviderOptionsValidatorTests.cs` for the default and explicit matching hosts, trim/case variants, valid host-only loopback values, missing/blank and nonmember expected hosts, and rejection of full URL, userinfo, port and trailing-dot values even when a matching allowlist entry is supplied. Verify the existing invalid-allowlist failure remains in force (FR-001 host shape, FR-002 membership).
- [ ] T003 [US1] After T001, extend `SearchSubtitlesQueryHandlerTests.cs` and `DownloadSubtitleCommandHandlerTests.cs` to verify each actual `EnsureProviderConfigured` guard handles `OptionsValidationException` using `SubtitleProviderNotConfiguredException` before any provider interaction. T006 exercises the real membership validator through the API. Complete these shared suites before T007.

### Implementation for User Story 1

- [ ] T004 [US1] After T002/T003, add the literal default `www.opensubtitles.com` for `DownloadLinkHost` in `SubtitleProviderOptions.cs`. Extend `SubtitleProviderOptionsValidator` (`IValidateOptions`) with a bare-host check and membership via a shared Core Options matcher reused by `SubtitleRequestUriPolicy` for existing trim and ordinal case-insensitive comparison. Preserve request-time validation and the existing allowlist validation (FR-001/002).
- [ ] T005 [US1] After T004, update loopback/fake OpenSubtitles option fixtures owned by the authorized unit/integration suites so each sets `DownloadLinkHost` explicitly to its allowlisted loopback host.
- [ ] T006 [US1] After T004/T005, extend `SubtitleAcquisitionOptionsCompositionTests.cs` and `SubtitleSearchDownloadEndpointTests.cs` for invalid expected-host membership: search and download both return `409 provider_not_configured`, search returns no candidates and total upstream calls are zero. Assert static readiness reports this configuration as invalid. Explicitly assert sidecar serving, the existing read-only-library precedence outcome, and the SaveEnabled=false search result with `downloadEnabled=false`. Use `SubtitleProviderHealthRegistrationTests.cs` or `SubtitleEndpointTests.cs` for additional assertions as needed.

**Checkpoint**: US1 independently testable. Runtime phase (US2) does not start until T005 and this checkpoint (T006) are complete.

---

## Phase 3: User Story 2 - Runtime refused-host latch (Priority: P1)

**Goal**: Absolute HTTPS refused download host latches process-locally (first canonical host only); detecting `502` with zero file GETs; later same-process search/download `409` before provider calls; readiness unchanged; malformed/non-HTTPS refusal preserved without tripping the latch.

**Independent Test**: Provider + endpoint integration coverage for initial `502`, hostname-only state, zero file GETs, cross-request `409`, fresh-process recovery, malformed/non-HTTPS without latch trip, and isolated redirect regression for US2 acceptance scenarios; unit coverage for shared guard outcomes.

### Implementation for User Story 2

- [ ] T009 [US2] After T005/T006, add the sealed Core latch under `Features/Subtitles`. Use instance state, with `Interlocked.CompareExchange` or a lock, to keep the first successful write only. The latch holds a single canonical host and has no scoped or HTTP client dependencies. A fresh instance starts empty.
- [ ] T010 [US2] After T009, register the latch as singleton in `SubtitleProviderServiceCollectionExtensions.cs`. Inject it as a required dependency in both search and download handlers. Read it in each existing `EnsureProviderConfigured` acquisition guard, using `SubtitleProviderNotConfiguredException` for a set latch. Preserve existing short-circuits and read-only precedence. Assert the same instance resolves from the root provider twice and from a child scope in `SubtitleAcquisitionOptionsCompositionTests.cs`.
- [ ] T011 [US2] After T010, inject the singleton into `OpenSubtitlesSubtitleProvider`. Use the parsed host component (`Uri.IdnHost` normalized to lowercase for storage) and retain only a bounded, canonical host value. Parse defensively so malformed, hostless or non-HTTPS links keep their existing refusal and leave the latch unset. On an absolute HTTPS link that fails host allowlisting, set the latch before throwing and before any file GET. Complete the synchronous write once the refusal is observed even if the caller cancels. Emit one structured warning on the winning write with the canonical host and restart requirement. Preserve the detecting `502 provider_unavailable`, disabled redirects and existing client behavior. A 3xx from an allowed file host fails on the existing path and leaves the latch unset. Add no retry or hedging around the file GET.

### Tests for User Story 2

- [ ] T007 [US2] After T003 and T010, extend both handler suites so a set latch causes `SubtitleProviderNotConfiguredException` before any provider interaction. Use a fresh latch per test. In the latch unit suite, run contending writes to an initially empty latch, assert a single winning write and no exceptions, and prove subsequent writes cannot replace the winner. Keep T003 and T007 sequential in the handler suites.
- [ ] T008 [US2] After T011, extend `OpenSubtitlesSubtitleProviderTests.cs` to assert exactly one download-API call and zero file GETs on the detecting request, first canonical host only, the existing provider failure, and exactly one structured refusal warning with the host and restart requirement. Verify later refused links do not replace the winner or emit another warning. Assert malformed and non-HTTPS links leave the latch unset, including a malformed link whose host cannot be parsed. Exercise a permitted file link that returns a redirect to a disallowed host: one GET to the permitted host, none to the target, existing failure, latch empty. Use a fresh latch or service provider per independent scenario.
- [ ] T012 [US2] After T011, extend `SubtitleSearchDownloadEndpointTests.cs` for detecting `502 provider_unavailable` (one download-API call and zero file GETs) and subsequent same-process search and download both `409 provider_not_configured` with zero additional upstream calls. Verify readiness still reflects valid static options and remains unchanged after latching. Explicitly assert existing sidecar serving, read-only-library precedence and disabled-provider search behavior under a set latch. Verify recovery uses a fresh API host with corrected settings. Use a fresh `WebApplicationFactory` per independent scenario and keep the same host across requests within the latch scenario. Use `SubtitleProviderHealthRegistrationTests.cs` or `SubtitleEndpointTests.cs` as needed.

**Checkpoint**: US1 + US2 independently testable. Home-cluster work (US3) waits for completed runtime tests T007/T008/T012.

---

## Phase 4: User Story 3 - Home-cluster allowlist and expected host (Priority: P2)

**Goal**: Generated home-cluster settings allowlist API + `www.opensubtitles.com` and set `DownloadLinkHost`; script test asserts without deploying.

**Independent Test**: `scripts/home/Test-NewHomeCluster.ps1` proves hosts and expected host.

- [ ] T013 [US3] After completed runtime phase T007/T008/T012, update `scripts/home/New-HomeCluster.ps1` OpenSubtitles block: `DownloadAllowedHosts = @('api.opensubtitles.com', 'www.opensubtitles.com')` and `DownloadLinkHost = 'www.opensubtitles.com'`.
- [ ] T014 [US3] After T013, update and run `scripts/home/Test-NewHomeCluster.ps1`. Assert the generated allowlist contains both `api.opensubtitles.com` and `www.opensubtitles.com` and `DownloadLinkHost` exactly equals `www.opensubtitles.com`. Run without deploying.

**Checkpoint**: US3 independently testable via the home-cluster script test.

---

## Phase 5: User Story 4 - Developer live check and implement-skill gate (Priority: P2)

**Goal**: Live script (against shipped host settings) + both implement-skill copies with workflow receipt/pending reporting. Script and skill deliverables land here; **completed** DEV-566 live acceptance is T017 after automated verification — a pending-evidence statement fulfills workflow reporting only and leaves acceptance pending.

**Independent Test**: Script behaviour matches brief Q5 against shipped host settings; both skill copies match brief Q6 (including pending-evidence wording). Passing `-Download` receipt is verified in T017, not substituted by pending text.

- [ ] T015 [P] [US4] After T014, add `scripts/Test-LiveSubtitleProvider.ps1` with `-Help`, `Set-StrictMode`, `$ErrorActionPreference = 'Stop'` and exit 0 for the requested mode’s success, 1 for failure and 2 for unavailable credential or quota evidence. Require `-SettingsPath` pointing to local JSON generated by the updated home-cluster script. Read only the two required host settings. Read the key from developer user-secrets exclusively, with no key parameter. Default to free search. `-Download` submits at most one quota-charging request without retry, then checks HTTPS, expected-host equality and allowlist membership before a redirect-disabled HTTP 200 file GET. The receipt carries mode, HTTP statuses, validated host, named check outcomes and fixed failure/pending reasons only. Capture subprocess output and exceptions without forwarding raw responses, bodies, headers, keys or link URLs. Keep all use outside CI/tests and follow the repo’s comment convention.
- [ ] T016 [P] [US4] After T014, apply the same external-provider live-check trigger, OpenSubtitles script name, sanitized receipt rule and pending-evidence wording to `.claude/skills/implement/SKILL.md` and `.agents/skills/implement/SKILL.md`. Follow T001 provenance: edit the canonical source and regenerate any generated copy. Obtain a cited Patron ruling if the required source is outside the authorized file set. Preserve the passing `-Download` acceptance requirement.

**Checkpoint**: US4 script and skill deliverables present. Live acceptance remains open until T017 records a passing receipt (pending evidence keeps acceptance pending).

---

## Phase 6: Polish, gates, opt-in receipt, and post-merge operator

- [ ] T018 After T006/T012/T014/T015/T016, run isolated unit and integration regressions for the authorized subtitle suites; resolve in-scope failures; keep suites network-isolated.
- [ ] T019 After T018, run applicable Size M repository gates and record honest outcomes (`PASS` / `SKIPPED` / `N/A` / `Could not run` distinct; drafting claims none):
  - Roslyn analyzers, complexity including refactor threshold when required, InspectCode
  - **`dotnet format --verify-no-changes`** (constitution Pull Request Quality Gates — mandatory; name and run explicitly)
  - property tests, vulnerable packages
  - full tests with **`LamuFlix.ArchitectureTests` execution explicit** (may be via the full suite)
  - applicable mutation
- [ ] T017 [US4] After T014 and completed T018/T019, run developer `-Download` with the local generated settings JSON asserted by the home-cluster test as the settings source. Record the sanitized passing receipt in the brief or PR. Record a specific pending reason if the check, credentials or quota are unavailable. Free search or pending reporting leaves SC-005 open.
- [ ] T020 After T019 and the T017 attempt, run Size M three-axis review (Risk/Standards/Spec) against the frozen delivery diff with brief Q1 closing bar and two-round cap. Review every live-check output/error path for raw URLs, secret values, forwarded subprocess output and response data. Verify the documented receipt field allowlist and inspect the sanitized receipt when present. Escalate unresolved in-scope findings at the cap to Bernstein. Pending live evidence leaves acceptance open.
- [ ] T021 After owner merge of the **delivery PR** (after Gate 1 and implementation — distinct from the Gate 1 spec-PR merge in T000 and from T017), operators regenerate the home-cluster secret and redeploy with updated settings; record that operator receipt separately from the pre-merge live receipt.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Gate 1 (Phase 0)**: Blocks Build pickup and implementation.
- **Pickup (Phase 1)**: Depends on T000; blocks production edits.
- **US1 (Phase 2) — brief approach 1**: Depends on T001; options/validator and fake configuration first (MVP). Completes at T005 + checkpoint T006 before runtime starts.
- **US2 (Phase 3) — brief approach 2**: Depends on T005 and T006; then T009 → T010 → T011; T007 after T003 + T010; T008 after T011 (wired adapter/DI); T012 after T011. Runtime phase completes at T007/T008/T012.
- **US3 (Phase 4) — brief approach 3**: Depends on completed runtime phase (T007/T008/T012); then T013 → T014. Not authorized immediately after T001.
- **US4 script/skills (Phase 5) — brief approach 4**: T015 and T016 both after T014; may run in parallel with each other after T014. Not authorized immediately after T001. Live receipt is **not** in this phase.
- **Polish (Phase 6) — brief approach 5**: T018 → T019 → T017 (opt-in receipt or pending-evidence record after automated verification) → T020 (review after T019 and that T017 attempt) → T021 post-merge operator work on the delivery PR.

### User Story Dependencies

- **US1**: No dependency on US2–US4 code; must complete options/validator/fakes (through T005) and US1 checkpoint T006 before runtime latch wiring (T009).
- **US2**: Needs US1 fakes + checkpoint (T005/T006) before T009; T008 waits for T011 so provider integration exercises the wired latch collaborator; acquisition/handler unit latch tests (T007) after T003 and T010.
- **US3**: After completed runtime phase (T007/T008/T012); required before T015/T016/T017 shipped-settings claims. Not parallel with US1/US2 from T001.
- **US4**: T015 and T016 both after T014 (parallel with each other); **passing** live receipt (T017) only after T014 + T018 + T019. Pending-evidence wording is workflow reporting, not completed acceptance.

### Parallel Opportunities

- T002 after T001 (disjoint validator-test file). **T003 is not `[P]` with T007** (shared acquisition/handler suites).
- After T011: T007 (once T003 + T010 are done) may run alongside T008 and T012 on disjoint files. **T008 does not overlap T010/T011** and does not start when only the latch type exists.
- T015/T016 in parallel **after T014** only.
- No Home Cluster (T013) or live skill/script (T015/T016) authorization immediately after T001.
- No parallel that places T017 before T014/T018/T019, or T020 before T019 and the T017 attempt (passing or pending record).

---

## Parallel Example: User Story 1

```text
Task: "Extend SubtitleProviderOptionsValidatorTests for DownloadLinkHost cases (incl. full-URL rejection)"
Task: "After T002/T003 land, implement DownloadLinkHost option + validator"
```

T003 and T007 must not run as concurrent edits on the acquisition/handler suites.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. T000 → T001
2. Complete Phase 2 (US1): options/validator + fake configuration + static regressions (through T005/T006)
3. **STOP and VALIDATE**: invalid allowlist → `409` on search/download, zero provider calls

### Incremental Delivery (brief approach 1–5 / Q8)

1. US1 → options/validator and fake configuration (through T005/T006)
2. US2 → runtime refusal state, adapter/DI wiring (T009–T011), then unit/provider/endpoint tests (T007/T008/T012)
3. US3 → cluster generator/assertions (`New-HomeCluster.ps1` / `Test-NewHomeCluster.ps1`) after runtime complete
4. US4 → live checker + both implement-skill copies after T014 (T015/T016 parallel; workflow reporting)
5. Automated verification (T018/T019, including `dotnet format --verify-no-changes` and ArchitectureTests) → separate opt-in live receipt attempt (T017: passing or pending evidence; pending leaves acceptance open) → review (T020 after T019 and that T017 attempt) → post-merge operator receipt on delivery-PR merge (T021)

---

## Notes

- `[P]` tasks = different files, no incomplete dependencies; T003/T007 deliberately sequential on shared suites; T008 waits for T011
- `[Story]` label maps to spec user stories
- No new NuGet/npm dependency, port, schema, route, DTO, or status code
- Do not invoke the live script from CI or automated tests
- Pending-evidence statements satisfy workflow reporting only; completed acceptance requires a passing `-Download` receipt
- Quill does not mark implementation tasks done from this drafting pass and claims no gate results
