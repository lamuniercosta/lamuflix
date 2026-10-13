# DEV-327 — Patron grill conclusions (Phase A)

Sources: ticket text (note DEV-327 lines 4, 15), recon-DEV-327, `specs/PRODUCT.md`, `.specify/memory/constitution.md`, charter §2.3.
Technical plan choices (job layout, script vs direct `dotnet stryker`, project enumeration, caching, path filters) are Keel's.

## Q1. Container job: build only, or build and push?
**Ruling: build-only validation. No registry push, no registry login, no new secret.**
- Ticket summary scopes it as "container validation job (Dockerfiles build cleanly)"; the AC names no push (note DEV-327:15).
- No registry or deploy target exists in `specs/PRODUCT.md` (Epic 8 is "by pick", line 16) or the constitution (Containers row, line 336); a push needs credentials (§2.3 item 5) with no source authorising them.
- The title word "push" is a summary error, not scope: Rigger corrects the summary (see YouTrack requests). Push is `noted, no ticket` until the owner asks for a registry.

## Q2. Which Dockerfiles, and what if they are absent?
**Ruling: the job builds the three DEV-21 Dockerfiles (`docker/Dockerfile.api`, `docker/Dockerfile.worker`, `docker/Dockerfile.web`) from repo root. DEV-327 implementation of the container job depends on DEV-21 implementation being merged first.**
- recon-DEV-327 §12: no Dockerfile, `docker/` or `.dockerignore` exists at origin/main `cb55fc0`; DEV-21 tasks.md T003/T005/T008 create them, and DEV-21 tasks.md:5 bars CI changes from DEV-21, so CI wiring belongs here.
- Bar is "build cleanly" = `docker build` exit 0 per file (ticket text). DEV-21's non-root / runtimeconfig checks stay DEV-21 evidence, not CI.
- Sequencing is Bernstein's; Rigger records the dependency as a comment.

## Q3. Stryker scope: whole `LamuFlix.Core` or changed files only?
**Ruling: whole-project mutation of `LamuFlix.Core` on every CI run; break threshold = `harness.yml` `gates.mutation.threshold` (80); below 80 fails CI.**
- Ticket: "Stryker on LamuFlix.Core threshold >= 80" names a project, not a diff; constitution line 290 states "break threshold 80 on `LamuFlix.Core`"; PRODUCT.md:12 "Stryker >= 80%".
- Standing ruling (charter, Known Tooling Limitations): never use `since`; the FAIL criteria there (0 tested, Ignored-by-since, all-Timeout, etc.) apply in CI too.
- Mutating test projects exclude `*.ArchitectureTests` and `*.IntegrationTests` (run-mutation.ps1:142; constitution line 290 names `LamuFlix.UnitTests`).

## Q4. Editing tracked scripts/config the ticket does not name (§2.3 item 6)
**Ruling: `.github/workflows/ci.yml` edits are in scope (ticket: "Extend GitHub Actions CI"). A whole-project mode added to `scripts/run-mutation.ps1` is in scope as forced by Q3, provided the existing diff-scoped behaviour and 0/1/2 exit contract are unchanged and `scripts/Test-RunMutation.ps1` covers the new mode. Any edit to `harness.yml`, `stryker-config.json`, `scripts/run-property-tests.ps1`, or `gitleaks.yml` needs a cited Patron ruling first.**
- Keel may instead call `dotnet stryker` directly with the tracked `stryker-config.json`; that choice is technical.

## Q5. Core baseline below 80
**Ruling: Keel measures the Core baseline score in the plan. Raising tests to reach 80 is in scope (forced by AC "all quality gates pass in CI"); if closing the gap exceeds size:M, Keel returns it to Patron before tasks are cut.**

## Q6. Property tests: which projects, and what counts as pass?
**Ruling: CI runs `scripts/run-property-tests.ps1` once per test project that holds `Category=Property` tests (at HEAD: `LamuFlix.Test`, `LamuFlix.UnitTests`, `LamuFlix.IntegrationTests` — recon §14). Exit 0 required; exit 2 on any of those projects fails CI (it would mean tagged tests vanished or were all skipped).**
- Ticket names the script verbatim; script exit contract recon §6.

## Q7. Meaning of "all quality gates pass in CI"
**Ruling: the existing `build-test-format` and `web` jobs plus the three new checks (container build, Stryker Core, property tests) are green on the DEV-327 PR. Wiring the other harness gates (analyzers, complexity, InspectCode, vulnerable packages) or the OpenAPI drift check (constitution line 337) into CI is out of scope — noted, no ticket.**
- Adding them would add to what the ticket delivers (charter §2.3 a).

## Q8. New GitHub Actions (§2.3 item 1 by analogy)
**Ruling: approved — `actions/upload-artifact@v4` (matches the existing `@v4` pinning in ci.yml). Container builds use the runner's `docker build` CLI; any other third-party action (e.g. `docker/*`) needs a cited Patron ruling.**

## Q9. Stryker report artifact
**Ruling: upload the Stryker HTML report as a workflow artifact on every run, including a failing run (`if: always()`), so a red gate is diagnosable. Name and retention are taste — see ASSUMPTIONS.md.**
- AC: "Stryker report uploaded".

## Structural escalations
None. No `blocked: structural` items.
