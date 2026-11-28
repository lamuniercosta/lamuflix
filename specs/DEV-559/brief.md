# DEV-559 grill brief

Patron closed the Phase 2 grill after 10 questions in two rounds. DEV-559 is a Size M, docs-only ticket with no UI design step. Evidence is pinned to `feature/559-spec` at `e1649d533018ee7311144c8c3953895c8cfcb0e2`. The authority is the ticket text (Goal, Wanted 1–7, Done-when, Out of scope, file map). Facts come from `recon-DEV-559`, plus the facts Patron verified in Q1, Q4 and Q10. Rulings with their bases are in `CONCLUSIONS.md`. Taste items are tagged `[assumed]` in `ASSUMPTIONS.md`. There are no §2.3 structural triggers and no owner checkboxes.

## Frozen scope

The work makes the top of README short, current and correct, moves the operations sections into `docs/operations/`, updates stale text, credits OMDb and TMDb, re-points the subtitle docs test and tracks `PRODUCT.md`.

There are no app changes, no new dependency or tooling, no docs site, no landing page, no extra screenshots and no new seeding script. Anything outside this list is a follow-up candidate, not a finding. A follow-up is filed only for Critical, High or broken behaviour; otherwise it is "noted, no ticket". The missing `v0.1.0` git tag is noted, no ticket.

## Closing bar and round cap

The round cap is one review round plus one remediation round. After that, residual blockers go to Bernstein. Do not claim closure and do not start round 2.

The ticket closes when all of these hold:
1. All nine Done-when lines are evidenced in the PR body:
   - a heading inventory, before and after;
   - a command table (file, command, ran or not run, result or reason);
   - the terms URLs with the date each was checked, and the verbatim credit wording;
   - the screenshot size, under 1 MB;
   - corrected lines for README :5, the README migration claim and `c4-container.md:15`;
   - the `PRODUCT.md` tracked commit;
   - DEV-558 recorded as verified-vacuous (Q4).
2. `dotnet test` is green, including `SubtitleSetupDocumentationTests`.
3. The Roslyn, complexity (≤15) and InspectCode gates pass on the changed `.cs` file. `dotnet format --verify-no-changes` passes.
4. `npm run check:vocabulary` in `web/` passes. This is the CI `vocabulary.yml` workflow.
5. Every relative link and image in README, `docs/operations/*`, `docs/architecture/modernization.md` and `docs/architecture/README.md` resolves. The implementer checks this by hand or with a script; no new tooling is added.
6. All three axis reports are present (Sentry, Ledger, Compass).
7. No Critical or High finding is unresolved, and no unresolved finding fails a Done-when line.

## Decisions (approach)

- **README top, in this order (Wanted 1, Q7):**
  1. Name, CI badge only (Q4), and one sentence.
  2. `docs/images/library.png` with the [assumed] caption.
  3. A quick start of three commands or fewer: `git clone`, `cd`, and the Compose up command as the current Compose section has it, verified on the day. There is no tag checkout. The changelog link names `v0.1.0` as the current release.
  4. "What it does today", from the CHANGELOG `v0.1.0` entry.
  5. "What it does not do", the ticket's five gaps. Apply the DEV-572 rule from Q7: if DEV-572 is merged at Phase B, write "no video transcoding".
  6. Operations links.
  7. "Data sources" credits (Q5).

  Below the top block: a link to `docs/architecture/modernization.md`, a one-line link to `docs/adr/` for architecture decisions, and the remaining short sections. Every claim follows the PRODUCT.md copy rule: no marketing, numbers or testimonials.
- **Operations move (Q1):** each README operations H2 becomes `docs/operations/<file>.md`, using the file names in `ASSUMPTIONS.md`.
  - The H1 is the old heading verbatim, and the subheads move up one level.
  - There is no index file.
  - Docker Compose keeps only the quick start in the README; the rest moves to `docker-compose.md`.
  - If "Architecture decisions" has prose beyond links, that prose moves to the existing `docs/architecture/README.md`.
  - Relative links and images are re-pointed. `observability.md` uses `../observability/aspire-dashboard-traces.jpg`.
  - No text is lost; the heading inventory proves it.
  - The untracked `docs/operations/` in the main checkout is not used. The docs are authored from the README text at HEAD.
- **Currency pass (Wanted 3, Q2, Q6):** check every command, port, setting key, file path and service name against the code at HEAD.
  - README :5 says "OMDb and TMDb".
  - Migration claims in the README story and in `c4-container.md:15` become count-free, for example "EF Core migrations in `src/LamuFlix.Infrastructure/Persistence/Migrations`".
  - TMDb is added beside OMDb in both C4 diagrams.
  - Fix any wrong command in the doc.
- **Story and C4 (Q2):** move the 2018–2026 modernization story and its inline diagram to `docs/architecture/modernization.md`, and update `docs/architecture/c4-container.md`.
- **Screenshot (Q3):** library grid, against a stand-in library seeded through the existing import API with placeholder movies, as in ADR 0023. Replace `docs/images/library.png` in place as a PNG under 1 MB.
- **Credits (Q5):**
  - Quote the wording verbatim from the OMDb and TMDb terms pages, checked on the day.
  - Add a logo only if the terms require one in documentation. If so, use the official asset, under 1 MB, in `docs/images/`.
  - A missing in-app attribution is out of scope. It goes to Rigger through Patron only if the terms make it a requirement.
- **Command verification (Q6):**
  - Run the read-only and idempotent commands on the day: build, test, the gates, format, web lint, typecheck, test and build, Compose up and down, version probes and observability endpoints.
  - Compose teardown never runs with `-v`. If the doc shows `-v`, the doc keeps the command and the PR marks that flag not run.
  - Mark these as not run, with the reason: home-cluster or GitOps apply, Windows installs and services, library-relative upgrades against a real library, anything needing secrets, any LocalPlay execution, and mutation testing ("runtime cost; covered by CI").
- **PRODUCT.md (Q8):** copy `F:\Dev\LamuFlix\PRODUCT.md` verbatim to the worktree root and read it in full before committing. Do not merge it with `specs/PRODUCT.md`. Do not modify the main checkout. If the scrub finds private data, stop and report to Bernstein.
- **Test (Q9, amended by Keel at Quill's needs decision):** the original wording assumed a heading-level section locator. None exists at HEAD: the test reads the whole file with `File.ReadAllText(Path.Combine(FindRepositoryRoot(), ReadmeFileName))` and asserts with whole-file `Contains` (recon-DEV-559 line 15; file read at `e1649d53`). The H2-to-H1 promotion therefore cannot affect it, and no locator is added. In `SubtitleSetupDocumentationTests.cs`:
  - Replace `ReadmeFileName` with a constant for `docs/operations/subtitles.md`, combined as segments: `Path.Combine(FindRepositoryRoot(), "docs", "operations", "subtitles.md")`.
  - Rename only the identifiers and text that name README so nothing stale remains: the constant, `ReadReadme()` (to `ReadSubtitlesDoc()`), the `readme` locals, the `Readme_` method-name prefix (to `SubtitlesDoc_`), and the `"README.md must document ..."` failure messages (to name `docs/operations/subtitles.md`).
  - Assertions, the two `DemoServingDocumentation` / `WritableSettingsDocumentation` arrays, `FindRepositoryRoot` and the two `ApiSettingsExample_*` tests stay unchanged in count and meaning. No new assertions, no new helper, no heading parsing.
  - `Readme_CarriesNoCommittedSubtitleApiKeyValue` now guards the subtitles doc only; that is the intended scope, since the subtitle text lives there after the move.
- **Vocabulary exceptions (Q10):** in `vocabulary-check.json`, change `file:` from `README.md` to the destination doc on each exception whose phrase moved. Patron located these entries at lines 16, 21, 63, 68 and 132. Keep the same phrase and reason, add no new exceptions and do not widen any phrase.

## Files touched

- `README.md`: rewritten top, sections moved out, links added.
- `docs/operations/*.md`: nine new files (Q1 names).
- `docs/architecture/modernization.md` (new), `docs/architecture/c4-container.md`, and `docs/architecture/README.md` (only if there is architecture-decisions prose).
- `docs/images/library.png`, plus an official logo only under Q5.
- `tests/LamuFlix.UnitTests/SubtitleSetupDocumentationTests.cs`.
- `vocabulary-check.json`, with `file:` re-pointed only.
- `PRODUCT.md`, newly tracked.

Nothing under `src/` or `web/src/` changes.

## Test strategy

The existing `SubtitleSetupDocumentationTests` is the regression test against the moved text, run with `dotnet test --filter "FullyQualifiedName~SubtitleSetupDocumentationTests"`. The other checks are not unit tests:
- the vocabulary check;
- the link and image resolution check;
- the heading inventory diff;
- the on-the-day command table;
- for the screenshot: its size, and a visual check that it shows the current UI.

No new test projects or tools are added.

## Gate expectations

The .NET gates run on the single changed `.cs` file only. Mutation testing is not applicable, because no production code changes. The web gates are not applicable beyond `check:vocabulary`, because `web/src` is unchanged. The OpenAPI and contract chain is untouched.

## Task ordering

1. Phase B pickup:
   - re-run `/speckit-analyze` against `main`;
   - record whether DEV-572 is merged, for the Q7 transcoding line;
   - confirm the `vocabulary-check.json` path and its entries;
   - take the "before" heading inventory from README at the pickup base.
2. Create the `docs/operations/*` files from the README text, re-point links and images, and update `vocabulary-check.json` in the same commit as the text move, so the gate never goes red between commits.
3. In one commit, point `SubtitleSetupDocumentationTests` at `subtitles.md` (path and README-naming renames only, per Q9) and remove the Subtitles section from the README. The test must stay green at every commit.
4. Move the story and C4 to `modernization.md`, and update both diagrams and `c4-container.md:15`.
5. Run the currency pass and the on-the-day command runs, and fix the docs.
6. Rewrite the README top: lists, quick start, credits (terms checked on the day) and operations links.
7. Capture the screenshot and replace `library.png`.
8. Track `PRODUCT.md`, after the scrub.
9. Take the "after" inventory, check links and the vocabulary, run the gates and tests, and complete the PR-body evidence.

## Complete grill record

- Round 1, Q1–Q4. Ask: `C:\Users\lamun\AppData\Local\Temp\claude\058adfe972d88857\a37e6ba7-49e1-428a-a1e9-d6ff5eaf5c9d\scratchpad\DEV-559-grill-1.md`. Patron's reply: `C:\Users\lamun\AppData\Local\Temp\DEV-559-grill-1-patron.md`. All accepted; Q1 has two corrections; Q4 was verified vacuous.
- Round 2, Q5–Q10. Ask: `...\scratchpad\DEV-559-grill-2.md`. Patron's reply: `C:\Users\lamun\AppData\Local\Temp\DEV-559-grill-2-patron.md`. All accepted, with additions to Q6, Q8 and Q10. The grill closes.

The full rulings with their bases are recorded in `CONCLUSIONS.md`. The brief is self-contained for Quill; the temp files are provenance only.
