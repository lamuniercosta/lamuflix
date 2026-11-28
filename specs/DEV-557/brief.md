# DEV-557 grill brief

Patron closed the Phase 2 grill after 7 questions (rulings with cited basis in `specs/DEV-557/CONCLUSIONS.md`, commit `95024d0b`). DEV-557 is Size M with no `ui` tag, so there is no design step. Evidence is pinned to `feature/557-spec` @ `e1649d533018ee7311144c8c3953895c8cfcb0e2` (`recon-DEV-557`). Authority is the DEV-557 ticket text (*Wanted* 1–4, *Done when*, *Out of scope*, *Where it lands*). No taste item came up, so there are no `ASSUMPTIONS.md` entries. No owner checkbox: neither §2.3 escalation applies.

## Frozen scope

- Add one abstract xUnit metadata-provider contract class. Each adapter runs it through the shipped DI composition (`MetadataProviderServiceCollectionExtensions.cs:43-59`): real typed `HttpClient`, real resilience pipeline and quota handler, against WireMock. Cases (ticket *Wanted* 1): found by IMDb id, found by title and year, not found, 401 as a key problem, 429 as `RateLimited`, 5xx as `ProviderUnavailable`, timeout, unreachable server, `Retry-After` inside and past the budget, breaker opening, cancellation, and telemetry (one span and one duration per call; no key, title or IMDb id in any tag).
- OMDb and TMDb both run it through thin derived fixture classes.
- Twin cases, meaning cases present in both families across lookup, resilience and telemetry, leave the adapter classes. Single-adapter cases stay: TMDb cancel-during-request, `MetadataProviderTmdbSignalTests`, `SecondaryMetadataProviderTrailerTests`, `SecondaryMetadataProviderMappingTests`, and the composition and chain-composition tests. If a twin carries an adapter-specific assertion the contract cannot express, only that residual assertion stays in its adapter class (Q4).
- No run against `ChainedMetadataProvider` (Q2). No new provider, and no change to chain selection (*Out of scope*).
- Production adapter code stays unchanged, with one exception: if the 401 outcomes differ, the suite asserts the OMDb behaviour, TMDb is brought to it only if "reported as a key problem" requires it, and the PR body states the difference (Q3).

## Shared harness with DEV-551 (Q1)

- DEV-557 consumes the harness that DEV-551 lands and defines no rival harness. `spec.md` names only the contract cases and adapter fixtures, never harness type names. The DEV-551 plan (`d115a12e`) freezes no generic type names, and DEV-551 is at Gate 1 (spec PR #520), not implemented.
- **Phase B pickup is blocked until the DEV-551 implementation merges to main.** The Phase B drift check re-runs `/speckit-analyze` against main, re-verifies the harness type names there, and repins `plan.md` and `tasks.md` to them.
- If the merged harness is subtitle-bound, extracting its provider-agnostic part (WireMock server lifetime, DI host build, telemetry capture) into a base both suites use is in scope (*Wanted* 4 forced edit, §2.3 item 6 carve-out). DEV-551's subtitle assertions are not touched.

## Charter §2.3 (Q6)

None triggered. There is no new dependency (WireMock.Net and xUnit are already in IntegrationTests), no new project or layer (the work lands in existing `tests/LamuFlix.IntegrationTests/`), and no schema, public API, `Features:LocalPlay`, secrets or `Process.Start` change. Deletes and rewrites are limited to twin removal in the 12 named family files, plus the Q1 harness clause. Any other edit needs a new Patron ruling. WireMock keys and tokens are fake literals only.

## Plan decisions

- **Approach:** abstract generic contract base over `IMetadataProvider`, with per-adapter abstract hooks (DI registration and options, WireMock stubs for found/not-found/error responses per provider wire format, expected 401 outcome). Then two derived classes (OMDb, TMDb), then removal of the twins from the existing families.
- **Contract case set (analyze decision, Keel, 2026-11-28):** the *Wanted* 1 list is the minimum, not the ceiling. Every twin in the inventory maps to a contract case. A twin with no matching case (known now: resilience "transient server error then success", which recovers rather than ending in `ProviderUnavailable`) gets a contract case added. It is not kept as a residual, because a residual that exists in both families would fail *Done when* 2. Residuals are only adapter-specific assertions inside a twin (Q4). To keep ordering step 3 intact, a draft twin inventory is taken before the contract class is written, and the contract covers it. If the T008 final inventory finds another unmatched twin, the case is added and both fixtures are green again before any removal. Basis: ticket *Wanted* 3 and *Done when* 2, Q4. This does not change the ticket.
- **Files touched:** new contract class and two fixture classes in `tests/LamuFlix.IntegrationTests/` (file names fixed at plan time against the merged DEV-551 harness). Edits are limited to the 12 recon files: `MetadataProvider{Lookup,Resilience,Telemetry}Tests.cs` and `SecondaryMetadataProvider{Lookup,Resilience,Telemetry}Tests.cs` for twin removal. The six stay files (`MetadataProviderTmdbSignalTests.cs`, `SecondaryMetadataProviderTrailerTests.cs`, `SecondaryMetadataProviderMappingTests.cs`, and the three composition and chain-composition files) are read-only; if pickup or the T008 inventory finds a twin in one of them, Build stops for a cited Patron ruling before editing it (plan challenge P2, Q4/Q6). The DEV-551 harness file is touched only under the Q1 clause. `OmdbMetadataProvider.cs` is read-only (it is the Q3 reference); only `TmdbMetadataProvider.cs` may be touched, and only under the Q3 401 alignment (plan challenge P1). Expected Q3 outcome is "none": recon shows ordinary 401 already returns `Failed(InvalidResponse)` with `wasUnauthorized` in both adapters (OMDb `:452-455`, TMDb `:537-540`).
- **Spec prerequisite:** `spec.md` records each adapter's current 401 outcome with file:line (Q3). Quill sends `needs recon:` for this to the Conductor and does not guess.
- **Test strategy:** integration tests only, WireMock with the real DI pipeline, injected time where the harness provides it. Before any removal, a twin inventory maps every removed test to its contract case (removed-test → contract-case table in `tasks.md` and the PR body). This proves *Done when* 2 and lets reviewers check that no assertion was lost.
- **Measurement (Q5):** Stryker on `OmdbMetadataProvider.cs` and `TmdbMetadataProvider.cs` with identical config, before on the base and after on the head. The score must not drop per file, and receipts are recorded. The line count covers the 12 recon files (5,072 at `e1649d53`) plus every new DEV-557 contract and fixture file and any harness lines DEV-557 adds; lines DEV-551 merged are excluded. After must be below before.
- **Gate expectations:** Roslyn, cyclomatic complexity (≤ 15, refactor gate 6), and InspectCode on changed files; `dotnet test` green; `dotnet format --verify-no-changes`. Baseline: Roslyn 0, complexity 0, InspectCode 2 (`MethodHasAsyncOverloadWithCancellation` at `SecondaryMetadataProviderTelemetryTests.cs:98,102`). If that file is in the diff at all, fix both warnings (gate-forced, Q7); otherwise leave them as `noted, no ticket`.
- **Task ordering constraints:**
  1. Phase B is blocked on the DEV-551 implementation merge, then the drift check repins the harness names.
  2. Stryker "before" receipt and line-count baseline on the Phase B base, before any edit.
  3. 401 outcome per adapter confirmed (from spec), then the contract base plus OMDb and TMDb fixtures, all green, before any removal.
  4. Twin inventory, then remove the twins family by family, with tests green after each.
  5. Q7 InspectCode fix if `SecondaryMetadataProviderTelemetryTests.cs` changed.
  6. Stryker "after" plus line-count receipts, then the full gates on the exact head.

## Plan challenge decisions (Keel, 2026-11-28, step 6)

Adjudication of Sentry F1-F8, Ledger M1-M5 and L1-L5, and Compass (clean) is in `CONCLUSIONS.md`. Decisions that change or sharpen the plan:

- **P1 (Ledger M1):** OMDb production file read-only; Q3 permits bringing TMDb to OMDb only.
- **P2 (Ledger M2):** the six stay files are read-only; a twin found there stops Build for a cited Patron ruling.
- **P3 (Ledger M3, Sentry F6):** T001 lists every DEV-551 file the Q1 extraction would touch, with its HEAD blob hash and line count. The extraction is structural only, with no assertion edits. Touching any file beyond the DEV-551 harness base file(s), such as rewiring a derived subtitle suite's base class or constructor, stops Build for a cited Patron ruling. T012 separates DEV-551-merged lines from DEV-557-added lines in that file by diff stat.
- **P4 (Sentry F1):** T001 records a harness capability receipt: delay injection, the unreachable-server mechanism, a fresh host per case or a proven reset, and time control (or real clock with existing budgets cited). A capability the harness lacks is built inside DEV-557's own contract and fixture files. Adding it to a DEV-551 file goes beyond the Q1 extraction and needs a cited Patron ruling.
- **P5 (Sentry F2):** each contract case gets a fresh DI host and WireMock state, or a reset that T004 cites as proven. Breaker and quota state must never carry over between cases. Timing budgets come from the shipped options, with no invented SLA. T002 and T012 record the Stryker `additional-timeout` value.
- **P6 (Sentry F3, F5):** the T001 draft twin inventory covers all three families (lookup, resilience, telemetry) as test-name lists. The T008 loop is the safety net, not the main path. T008 and the review mark "cancel during backoff wait" (twin) apart from TMDb cancel-during-request (stay).
- **P7 (Sentry F4):** the OMDb ordinary-401 stub must not match the request-limit (quota) body; T004 and T005 include a negative guard for that. The quota case stays OMDb-only and outside the shared assertion.
- **P8 (Ledger M4, Sentry F7):** the Q5 receipts come from a direct Stryker run with explicit `--mutate` on the two adapter files and identical config, and are labelled receipts, not a gate PASS. T013 reports the `scripts/run-mutation.ps1` verdict honestly (PASSED, FAILED, SKIPPED or N/A) and pastes the kill-share and file-span lines the tool prints.
- **P9 (Ledger M5):** Keel makes the Phase B repin. It is committed on the Phase B Build branch as `DEV-557 - Repin harness names to DEV-551 main`, changes only names and paths (no scope, FR or SC edits), and is recorded in `CONCLUSIONS.md`. T001 is read-only apart from that commit.
- **P10 (Ledger L1):** the line-count comparator is the T002 measurement on the Phase B base. 5,072 at `e1649d53` is kept only as historical context.
- **P11 (Ledger L2, L3):** the twin inventory is committed at `specs/DEV-557/twin-inventory.md` on the Build branch before any deletion, so reviewers can check it before the removals start. Every commit starts `DEV-557 - `.

## Closing bar and round cap

Done when both adapters pass the contract suite; every case that was in both families exists only in the suite (inventory receipt); the Stryker score per adapter file does not drop (paired receipts); the line count drops under the Q5 measure; and all gates are green on the exact head. Review uses all three Size M axis reports (Sentry, Ledger, Compass), and a missing axis blocks. Cap: two review and remediation rounds. Any unresolved Critical or High finding blocks, as does any finding that fails a *Done when* item or a required gate. Residual blockers go to Bernstein at the cap. Out-of-scope findings become follow-up tickets only for Critical, High or broken behaviour; otherwise they are recorded `noted, no ticket`.

## Grill record (summary; full rulings in CONCLUSIONS.md)

- Q1 harness: consume DEV-551's harness, no rival; Phase B blocked until DEV-551 merges; drift check repins names; subtitle-bound extraction is in scope.
- Q2 chain: no.
- Q3 401: spec cites both with file:line; if they differ, OMDb behaviour wins and the PR says so.
- Q4 twins: twins across lookup, resilience and telemetry move; single-adapter cases and residual assertions stay.
- Q5 Stryker and lines: identical settings, before on base and after on head, no drop per file; lines = 12 files + new DEV-557 files, excluding DEV-551 harness lines.
- Q6 §2.3: none triggered; fake keys only.
- Q7 InspectCode: fix :98 and :102 if the file is in the diff, else leave.
