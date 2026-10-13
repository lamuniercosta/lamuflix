# DEV-327 — Brief (Phase 2 step 3.1, grill close)

Ticket: DEV-327 — Configure CI workflows for container build/push, Stryker mutation testing, and property tests
Size: M | ui: (none) | Parent: DEV-287
Phase A worktree: F:\Dev\LamuFlix.worktrees\feature-327-spec | Branch: feature/327-spec; Phase B worktree: F:/Dev/LamuFlix.worktrees/DEV-327 | Branch: feature/DEV-327 | Pickup HEAD: 42b7ec1d1ff99e1f652edb51db4577337e538843 (recon-DEV-327-3:7-12).
Grill base HEAD: cb55fc02c09bfa574459bccc71d8ab5547d8c6f8 (= merge-base with origin/main; recon-DEV-327 section 1)
Sources: ticket text (note DEV-327 lines 4, 15; corrected summary build-only per Q1, Rigger verified exit 0); recon-DEV-327 (sections 1–16); specs/PRODUCT.md; .specify/memory/constitution.md; charter section 2.3 (note lamuflix-team-charter); specs/DEV-327/CONCLUSIONS.md (Q1–Q9, commit 7548e50); specs/DEV-327/ASSUMPTIONS.md (3 items); note task-pipeline (Phase 2 step 3.1, section 2.2 caps).

## Closing bar

- The grill is closed. Patron reported 0 structural issues; there is no `blocked: structural` item and no owner checkbox for Gate 1 from this grill.
- This brief freezes scope, grill answers, and plan decisions for Quill. If it is not in this brief, it is not decided; a gap is `needs decision: <question>` to Keel, never a guess by Quill.
- Quill next drafts spec.md, plan.md, and tasks.md in one ask from this brief, recon-DEV-327, and specs/PRODUCT.md (Phase 2 step 3.2). Keel then runs /speckit-analyze read-only against this brief and sends one numbered fix list per round (step 3.3).
- Spec Kit is not started by this brief. No ADR (M ticket; ADR is L-only, task-pipeline Phase 2 step 5). No design.md (no ui: tag).
- Technical plan choices left to Keel by CONCLUSIONS.md (job layout, script vs direct `dotnet stryker`, project enumeration, caching, path filters) are decided under Plan decisions below; Quill encodes them, and does not reopen them without a `needs decision:` to Keel.

## Round caps

- Grill cap: 12 questions (task-pipeline section 2.2); beyond that, Keel's recommendation tagged [assumed] in ASSUMPTIONS.md. Grill closed within cap at 9 rulings (Q1–Q9); the 3 logged assumptions are in ASSUMPTIONS.md.
- Review cap: 2 rounds maximum; third-round findings go to user review with the PR.
- Remediation cap: 2 fix commits per round; a third means the finding or the plan was wrong.
- Blocked limit: 3 blocked reports parks the task.

## Frozen scope

In scope (implementation, Phase B):

- `.github/workflows/ci.yml` — edits only: three new checks added alongside the existing `build-test-format` and `web` jobs (Q4, Q7). No other workflow file is created or edited.
- A whole-project mode added to `scripts/run-mutation.ps1` for `LamuFlix.Core`, only as forced by Q3, with the existing diff-scoped behaviour and the 0/1/2 exit contract unchanged (Q4); `scripts/Test-RunMutation.ps1` extended to cover the new mode (Q4).
- Test additions inside the existing test projects only, and only if the Q5 baseline measurement shows they are needed to reach break 80 on `LamuFlix.Core` (see Plan decisions). No new test project.

In scope (this Phase A): specs/DEV-327/brief.md (this file), then Quill-drafted spec.md, plan.md, and tasks.md under specs/DEV-327/.

Out of scope (adding any of these is a ticket change under escalation (a), unless a fresh cited Patron ruling says otherwise):

- Registry push, registry login, any new secret, and any `docker/*` GitHub Action (Q1, Q8). The container job is build-only validation.
- Creating any Dockerfile, `docker/`, Nginx config, or `.dockerignore` — DEV-21 delivered four files (`docker/Dockerfile.api`, `docker/Dockerfile.web`, `docker/nginx.conf`, `.dockerignore`); DEV-327 builds only the two Dockerfiles already present. The worker Dockerfile was permanently dropped, not deferred (Patron Q2a, note DEV-327:55-56; recon-DEV-327-3:25-33).
- Any edit to `harness.yml`, `stryker-config.json`, `scripts/run-property-tests.ps1`, or `.github/workflows/gitleaks.yml` without a cited Patron ruling first (Q4). In particular: no threshold change anywhere; break stays 80 from `harness.yml` `gates.mutation.threshold` / `stryker-config.json` (recon sections 9–10, both already 80).
- Wiring the other harness gates (analyzers, complexity, InspectCode, vulnerable packages) or the OpenAPI drift check (constitution line 337) into CI (Q7) — noted, no ticket.
- Any NuGet/npm dependency, new project or top-level folder, database schema change, or public API shape (route, DTO, status code) — none is needed; recon sections 11 and 15 confirm none is touched.
- Anything touching `Features:LocalPlay`, secrets, or `Process.Start` (recon section 15: 0 hits in the inspect set; the new jobs introduce none).

## Grill answers (every ruling, cited)

The 9 original Patron rulings from specs/DEV-327/CONCLUSIONS.md (commit 7548e50) remain in effect except Q2, superseded at Phase B pickup by Patron Q2a (note DEV-327:55-56). CONCLUSIONS.md:18-22 records Q2a; Q2 remains preserved there as historical and superseded. This brief uses the recorded Q2a.

- Q1. Container job is build-only validation: no registry push, no registry login, no new secret. Ticket summary scopes "container validation job (Dockerfiles build cleanly)" and the AC names no push (note DEV-327:15); no registry or deploy target exists in PRODUCT.md (Epic 8 "by pick", line 16) or the constitution (Containers row, line 336). The title word "push" was a summary error; Rigger has corrected the YouTrack summary (verified exit 0, note DEV-327). Push stays `noted, no ticket` until the owner asks for a registry.
- Q2a (supersedes Q2). Container validation builds only `docker/Dockerfile.api` and `docker/Dockerfile.web` from the repo root; "build cleanly" means `docker build` exit 0 per file. The owner permanently dropped `docker/Dockerfile.worker` in DEV-21, PR #136 option c (`specs/DEV-21/tasks.md:5`, quoted in recon-DEV-327-3:29). No restoration wait or owner checkbox: the ticket names no Dockerfile set; the three-file list was Patron Q2 (Patron receipt, note DEV-327:55-56). Both selected Dockerfiles are on main at pickup HEAD (recon-DEV-327-3:26-33); Patron rules the DEV-21 gate satisfied and T009 ungated. DEV-21's non-root/runtimeconfig checks remain DEV-21 evidence, not CI. Recon line 32's escalation classification is superseded by this deliberate Patron ruling, not adopted as a decision.
- Q3. Stryker scope is whole-project mutation of `LamuFlix.Core` on every CI run — not changed-files-only — with break threshold = `harness.yml` `gates.mutation.threshold` (80); below 80 fails CI. Basis: ticket "Stryker on LamuFlix.Core threshold >= 80" names a project, not a diff; constitution line 290 "break threshold 80 on `LamuFlix.Core`"; PRODUCT.md:12 "Stryker >= 80%". Never use `since` (standing ruling); the standing FAIL criteria (0 tested, Ignored-by-since, all-Timeout, and the other evaluation failures at run-mutation.ps1:335–368, recon section 7) apply in CI too. Mutating test projects exclude `*.ArchitectureTests` and `*.IntegrationTests` (run-mutation.ps1:142); constitution line 290 names `LamuFlix.UnitTests`.
- Q4. File scope (charter section 2.3 item 6): `.github/workflows/ci.yml` edits are in scope (ticket: "Extend GitHub Actions CI"). A whole-project mode added to `scripts/run-mutation.ps1` is in scope as forced by Q3, provided the existing diff-scoped behaviour and 0/1/2 exit contract are unchanged and `scripts/Test-RunMutation.ps1` covers the new mode. Keel may instead call `dotnet stryker` directly with the tracked `stryker-config.json` — decided under Plan decisions. Any edit to `harness.yml`, `stryker-config.json`, `scripts/run-property-tests.ps1`, or `gitleaks.yml` needs a cited Patron ruling first.
- Q5. Keel measures the `LamuFlix.Core` baseline mutation score in the plan. Raising tests to reach 80 is in scope (forced by AC "all quality gates pass in CI"); if closing the gap exceeds size:M, Keel returns it to Patron before tasks are cut.
- Q6. Property tests: CI runs `scripts/run-property-tests.ps1` once per test project that holds `Category=Property` tests — at HEAD `LamuFlix.UnitTests` and `LamuFlix.IntegrationTests` (recon section 14). Exit 0 is required from each invocation; exit 2 on any of those projects fails CI, because it would mean tagged tests vanished or were all skipped. Ticket names the script verbatim; its exit contract is at recon section 6 (0 = at least one test ran and passed; 2 = harness off, no tagged tests, or all skipped; the script already rejects solutions and takes one `-Project` at a time).
- Q7. "All quality gates pass in CI" means: the existing `build-test-format` and `web` jobs plus the three new checks (container build, Stryker Core, property tests) are green on the DEV-327 PR. Wiring the other harness gates or the OpenAPI drift check into CI is out of scope — noted, no ticket; adding them would add to what the ticket delivers (escalation (a)).
- Q8. New GitHub Actions (charter section 2.3 item 1 by analogy): `actions/upload-artifact@v4` is approved (matches the existing `@v4` pinning in ci.yml, recon section 15). Container builds use the runner's `docker build` CLI; any other third-party action (for example `docker/*`) needs a cited Patron ruling first.
- Q9. The Stryker HTML report is uploaded as a workflow artifact on every run, including a failing run (`if: always()`), so a red gate is diagnosable. AC: "Stryker report uploaded". Artifact name and retention are taste — see ASSUMPTIONS.md.

Assumptions (ASSUMPTIONS.md, taste, [assumed]):

- A1. Stryker artifact name `stryker-report-core`; GitHub default retention, no override (Q9).
- A2. New CI checks use the same triggers as ci.yml (push to main, pull_request to main); path filters are Keel's (decided under Plan decisions).
- A3. Container job builds images without tagging for any registry; local tags only (Q1).

Structural escalations: none. Patron reported 0 structural issues; no ruling contradicts or changes the ticket text (the Q1 title word "push" was corrected as a summary error that does not change what the ticket delivers, recorded by Rigger) and no constitution departure is proposed.

## Plan decisions

Approach:

- Job layout: three new, separate jobs in `.github/workflows/ci.yml`, in addition to the untouched `build-test-format` and `web` jobs — one per new check — so each check fails, passes, and is diagnosed independently and the Stryker artifact belongs to exactly one job. All three run on `ubuntu-latest` with the same triggers as the existing jobs (push to main, pull_request to main; A2) and no path filters: a path filter could silently skip a required check on the DEV-327 PR and make Q7's "green" unverifiable, and Stryker Core / property tests must run on every CI run (Q3, Q6). Each .NET job follows the existing ci.yml shape (actions/checkout@v4, actions/setup-dotnet@v4 with `global-json-file: global.json`, recon section 4); no caching beyond what the existing jobs already use is introduced, and no job gets a registry login, a secret beyond the default `GITHUB_TOKEN`, or a third-party action other than the approved `actions/upload-artifact@v4` (Q8).
- Container job (`container-build`, runner `docker build` CLI only, Q8): after `actions/checkout@v4`, use exactly two explicit build steps named for their files: `docker build -f docker/Dockerfile.api .` and `docker build -f docker/Dockerfile.web .`, from repo root; each must exit 0 (Q2a). No runtime discovery or worker build step. A missing selected file or nonzero build fails its named step and the job; neither is skipped or turned into a dependency wait. Images carry local tags only (A3), no registry tag, login, push, or `docker/*` action (Q1, Q8). No non-root/runtimeconfig/image-content checks. DEV-21 gate is satisfied at pickup; T009 needs the T006 workflow, not a further DEV-21 merge, rebase, restoration, or owner answer.
- Stryker job — mechanism (Q4 choice): extend `scripts/run-mutation.ps1` with an explicit whole-project mode for `LamuFlix.Core` and invoke that mode from CI, rather than calling `dotnet stryker` directly. Rationale: the script already owns the harness threshold read (gates.mutation.threshold, default 80), the eligible-test-project rule excluding `*.ArchitectureTests` and `*.IntegrationTests`, the temp-config generation (test-runner mtp, coverage off, `since` disabled, break = threshold), the report read, and the standing FAIL evaluation (0 tested, 0 killed, score below threshold, and the rest at recon section 7) that Q3 requires in CI; a direct `dotnet stryker` call would bypass those guardrails and re-implement them in YAML. The new mode mutates the whole of `src/LamuFlix.Core/LamuFlix.Core.csproj` on every run (Q3), never enables `since`, and reuses the tracked `stryker-config.json` reporters (progress, html, json; recon section 9).
- Stryker job — pass bar and artifact: the run passes only on script exit 0, which requires score at or above break 80 whole-Core (Q3; recon sections 7 and 9: score = (killed + timeout) / tested × 100). Exit 1 fails the job; exit 2 also fails the job in CI — whole-Core scope is never scope-empty, so a 2 means the gate did not run, and a gate that could not run has not passed. The HTML report from the Stryker output tree is uploaded with `actions/upload-artifact@v4` under the name `stryker-report-core` with `if: always()` and default retention (Q9, A1), so a failing run is still diagnosable.
- Property-test job: invokes `pwsh scripts/run-property-tests.ps1 -Project <project>` once per enumerated project — `tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj`, `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` (the two projects holding `Category=Property` tests at HEAD, recon section 14) — because the script accepts exactly one project per invocation (recon section 6). The project list is enumerated explicitly in the workflow, not discovered by glob or grep at runtime: explicit enumeration is what makes Q6 enforceable, since a project whose tagged tests vanished must surface as that project's exit 2 and fail CI instead of silently dropping out of a discovered list. Every invocation must exit 0; exit 1 or exit 2 on any one of them fails the job (Q6). `scripts/run-property-tests.ps1` itself is not edited (Q4).
- Q5 baseline measurement (plan stage, before tasks are cut): the plan records a measured whole-Core Stryker baseline score for `LamuFlix.Core` (one whole-project-mode run, or the new mode's first run, with the raw score in plan.md). If the baseline is at or above 80, no test-raising work is planned. If it is below 80, the plan sizes the test-raising work needed to close the gap; that work is in scope (Q5), but if it exceeds size:M, Keel returns to Patron before tasks are cut — Quill does not silently cut an oversized task list. The baseline value is a measurement for Gauge/Keel to produce, not a recon fact; recon-DEV-327 deliberately contains no score (recon section 16).

Files touched:

- Spec phase (Phase A): specs/DEV-327/spec.md, specs/DEV-327/plan.md, specs/DEV-327/tasks.md (Quill, from this brief); this brief is the only Keel-written artifact. No ADR, no design.md.
- Build phase (Phase B, for plan/tasks to enumerate): `.github/workflows/ci.yml`, `scripts/run-mutation.ps1`, `scripts/Test-RunMutation.ps1`, and — only under the Q5 condition above — test files inside the existing `LamuFlix.Test` / `LamuFlix.UnitTests` projects. Nothing else. Any further file is `needs decision:` to Keel before it is added; edits to `harness.yml`, `stryker-config.json`, `scripts/run-property-tests.ps1`, or `gitleaks.yml` additionally need a cited Patron ruling (Q4).

Test strategy:

- `scripts/Test-RunMutation.ps1` is extended to cover the new whole-project mode (Q4): the new mode classifies and invokes Stryker for the whole named project, and — as regression coverage — the existing diff-scoped behaviour and the 0/1/2 exit contract are asserted unchanged. This script coverage is required, not optional; the mode ships only with it.
- Q5 test-raising, if triggered, follows the repo's existing test conventions in the affected project (xUnit, `Method_Condition_Expected` naming) and is proven by the whole-Core score reaching at least 80, not by test count.
- No existing test references ci.yml, any Dockerfile, Stryker, or either gate script (recon section 14: 0 hits for every pattern), and no new test project is created to test YAML. The workflow changes are verified by the CI run itself on the DEV-327 PR (Q7) plus local execution of the two gate scripts in their CI modes; that run output is the evidence, recorded in the task note.
- Property-test coverage already exists (35 `Category=Property` Trait lines across 14 files in the same two enumerated projects at Phase B pickup, recon-DEV-327-3:39; the Phase A count of 33 is historical); this ticket wires it into CI and adds no property tests unless the Q5 Stryker gap separately forces Core test-raising that happens to add them.

Gate expectations (Phase B, run by Gauge per task-pipeline Phase 3 step 5):

- The DEV-327 PR is green only when all five ci.yml jobs pass: `build-test-format`, `web`, container build, Stryker Core (exit 0 at break 80, HTML artifact uploaded on every run), and property tests (exit 0 for each of the two enumerated projects) (Q6, Q7, Q9; derivative Q10 confirmation: CONCLUSIONS.md:59-62, note DEV-327:212).
- Local harness gates keep their existing contracts and are not redefined by this ticket: exit 0 = pass; exit 1 = fail and blocks; exit 2 = SKIPPED (scope-empty), non-blocking and never reported as PASS; a gate that could not run has not passed. `scripts/run-mutation.ps1` in its existing diff-scoped mode keeps reporting SKIPPED (scope-empty) when no production C# under src/ changed — the new whole-project mode does not change that behaviour (Q4). A property-test exit 2 blocks unless the task note records the pipeline opt-out; in CI it always fails the job (Q6).
- Never lower a threshold (break stays 80), never edit a generated file, and never substitute plain `dotnet build` or a direct `dotnet stryker` call for a gate script. Stryker in CI is minutes-expensive; it runs as its own job so its duration never blocks the fast jobs' signal.

Task-ordering constraints (for Quill to encode in tasks.md):

1. DEV-21 gate satisfied at Phase B pickup (Q2a; recon-DEV-327-3:26-33). T009 is ungated with respect to DEV-21: T006 → T009, with both named builds exit 0 and green CI evidence recorded in the task note. T008 and T009 may proceed in parallel after T006; no restoration wait, additional DEV-21 merge/rebase condition, or owner checkbox. The Q5 baseline/gap prerequisite on T006 and the all-five-jobs Q7 closing bar remain in force.
2. The whole-project mode in `scripts/run-mutation.ps1`, together with its `scripts/Test-RunMutation.ps1` coverage, lands before the ci.yml Stryker job that invokes it; the workflow never references a mode that does not exist on the same branch.
3. The Q5 baseline is measured as soon as the whole-project mode exists and before any test-raising tasks are cut; a below-80 baseline that exceeds size:M returns to Patron at that point, before tasks are finalised.
4. The three ci.yml jobs are added in one workflow edit; the Stryker job's artifact-upload step (`if: always()`, `actions/upload-artifact@v4`, name `stryker-report-core`) is part of that same edit, not a follow-up.
5. No task may add a registry push or login, a secret, a `docker/*` action, a Dockerfile, a threshold change, or an edit to `harness.yml`, `stryker-config.json`, `scripts/run-property-tests.ps1`, or `gitleaks.yml` (frozen scope). Such a task is a brief violation for Keel to strike at /speckit-analyze.

## Next owner and action

- Next owner: Quill — bounded Phase B pickup alignment of spec.md, plan.md, and tasks.md to Q2a and the technical decisions above, using recon-DEV-327-3 for current facts; retain the Q5 measurement/gap prerequisite. Exact alignment anchors are listed below. CONCLUSIONS.md remains Patron's recording action, not Quill's.
- Then Keel: /speckit-analyze read-only against this brief, checking the two-file container contract, ungated T009 ordering, whole-project mode contract, enumerated property-test projects, and frozen file set.
- This is pickup alignment after merged spec PR #138, not a new Gate 1 owner question. Nobody merges; the user merges every PR. No implementation, gates, commit, or push is performed by this brief update.

## Phase B pickup alignment — Patron Q2a (2026-10-13)

Authority: Conductor's bounded ask and Patron receipt at note DEV-327:55-56. Factual basis: recon-DEV-327-3:7-12, 25-33. The original Phase A absence premise and three-file dependency are historical; Q2a controls the current plan. This update changes only brief.md; Quill owns the following alignment in the other three artifacts. Line anchors refer to unchanged pickup HEAD 42b7ec1d1ff99e1f652edb51db4577337e538843:

1. `spec.md:15,19,23` (US1 description, independent test, acceptance scenario 1) and `spec.md:76` (FR-002): exactly api + web, repo-root commands as above, exit 0 per file, cite Q2a; remove worker and any future merge/rebase prerequisite. `spec.md:24` still requires a failed build to fail the job and name its file. Align `spec.md:64` (edge case) to failure for a missing selected file, not a DEV-21 wait; `spec.md:110` (assumption) records the satisfied pickup prerequisite, no owner checkbox or restoration wait. Keep FR-001's three jobs (`spec.md:75`) and SC-001's five-job green bar (`spec.md:100`); two Dockerfiles does not mean two new jobs.
2. `plan.md:9` (summary) and `plan.md:73` (container design): gate satisfied, exactly two explicit named steps after checkout, same CLI/local-tag/no-login/no-push restrictions, fail on missing selected file or nonzero build. Cite Q2a and recon-DEV-327-3; no worker, restoration wait, or further DEV-21 merge/rebase condition.
3. `tasks.md:38` (T006): enumerate exactly the two selected Dockerfiles and the commands/named-step failure contract. `tasks.md:45,47` (Phase 4 heading/purpose), `tasks.md:51` (T009), `tasks.md:54` (checkpoint), `tasks.md:60-61` (dependencies/parallelism), and `tasks.md:66` (MVP): remove DEV-21-gated/WAITS/once-merged sequencing; T006 → T009, record green container-build with both builds exit 0, T008 + T009 parallel after T006. All five jobs are still required by Q7; no reduced closing bar. Q5's prerequisite on Phase 3/T006 stays intact.
4. Update source metadata to distinguish Phase A from pickup: `spec.md:3,9`, `plan.md:3,5`, `tasks.md:3` use feature/DEV-327 where current branch is stated and include this brief's Q2a plus recon-DEV-327-3. CONCLUSIONS.md:18-22 records Q2a; Q2 is preserved as historical and superseded, not authority for a worker/restoration requirement. `spec.md:94`'s Phase A count of 33 should be labelled historical or aligned to current 35 Trait lines across 14 files; the same three holding projects remain valid (recon-DEV-327-3:39). No other requirement or task scope is reopened.

## Bounded Constitution citation decision — C1 (2026-10-13)

Keel decision: cite the following applicable Constitution principles in each evaluated plan gate, as required by `.specify/memory/constitution.md:528-529`. This records citation coverage, not a Constitution departure or an implementation gate result.

- TimeProvider / seeded Random: Principle VII (Configuration Isolation and Deterministic Time, lines 241-257) for injected time; seeded Random remains the existing harness rule.
- Threshold / generated files / gate-script substitution: Principle IX (Test Pyramid with Real Infrastructure, lines 282-309), specifically whole-Core break 80 at line 290; generated-file and gate-script constraints remain the existing harness rules recorded in Gate expectations.
- Assertion/mocking libraries and real infrastructure: Principle IX (lines 284-301). No dependency/project/folder/schema/API change: Principles I (architecture and allowed Core references, lines 93-120) and V (request/error contracts, lines 196-213), with Technology Stack Constraints and FR-013 retaining the broader frozen-scope restrictions.
- LocalPlay / secrets / Process.Start: Principle VII (lines 243-255).
- Overall no-departure assessment: Principles I, V, VII, and IX for the evaluated gates above; no new gate or scope change.
