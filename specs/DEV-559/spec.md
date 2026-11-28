# Feature Specification: README overhaul and operations docs move

**Feature Branch**: `feature/559-spec`

**Created**: 2026-11-28

**Status**: Draft

**Input**: Ticket DEV-559 (parent DEV-100; size M); grill-closed `brief.md` (Q1–Q10, Q9 Keel amendment); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-559`, `recon-DEV-559`; `specs/PRODUCT.md`. Quill invents no decisions beyond the brief.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Frozen scope; `CONCLUSIONS.md` §2.3; `PRODUCT.md` §5). Ordinary owner Gate 1 remains pending until the user merges the Spec Kit PR.

## Clarifications

### Session 2026-11-28

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: README top order, operations file layout, currency corrections, screenshot, credits, command verification, PRODUCT.md scrub, subtitle-test path/renames (Q9 amended), and vocabulary re-points are settled in `brief.md` and `CONCLUSIONS.md`. Taste items tagged `[assumed]` in `ASSUMPTIONS.md` are Patron-ratified. No `[NEEDS CLARIFICATION]` markers. Conductor ask omitted `/speckit-clarify` and `/speckit-checklist`. Any gap Quill cannot close from brief/CONCLUSIONS goes to `DRAFTING_RECEIPT.md` as `needs decision:`.

## What the User Will See *(mandatory)*

Nothing visible: docs-only ticket; no app, API, schema, or `web/src` changes (`brief.md` Frozen scope; Files touched).

## Scope

**In scope** (`brief.md` Frozen scope / Decisions / Files touched; ticket Wanted 1–7 and Done when):

1. **README top** (Wanted 1, Q7): name, CI badge only, one sentence; `docs/images/library.png` with [assumed] caption; quick start of three commands or fewer (`git clone`, `cd`, Compose up as in the current Compose section, verified on the day; no tag checkout); changelog link naming `v0.1.0`; "What it does today" from CHANGELOG `v0.1.0`; "What it does not do" from the ticket's five gaps with the Q7 DEV-572 wording rule; operations links; "Data sources" credits. Below the top: link to `docs/architecture/modernization.md`, one-line link to `docs/adr/`, and remaining short sections. PRODUCT.md copy rule: no marketing, numbers, or testimonials.
2. **Operations move** (Wanted 2, Q1): each README operations H2 → `docs/operations/<file>.md` using `ASSUMPTIONS.md` names; H1 = old H2 verbatim; subheads up one level; no index; Compose keeps only quick start in README, rest in `docker-compose.md`; Architecture-decisions prose beyond links → existing `docs/architecture/README.md`; re-point relative links/images (`observability.md` → `../observability/aspire-dashboard-traces.jpg`); no text lost (heading inventory before/after); author from README at HEAD (not main-checkout untracked `docs/operations/`).
3. **Currency** (Wanted 3–4, Q2, Q6): every command, port, setting key, path, and service name checked against HEAD; README :5 says "OMDb and TMDb"; migration claims count-free (e.g. EF Core migrations path); TMDb beside OMDb in both C4 diagrams; wrong commands fixed; story + inline C4 → `docs/architecture/modernization.md`; update `docs/architecture/c4-container.md`.
4. **Screenshot** (Q3): library grid, stand-in library via existing import API (ADR 0023); replace `docs/images/library.png` in place, PNG under 1 MB; [assumed] caption.
5. **Credits** (Wanted 6, Q5): verbatim wording from OMDb and TMDb terms pages checked on the day; URL + date + wording in PR body; logo only if terms require one in documentation (official asset, under 1 MB, `docs/images/`); missing in-app attribution out of scope (follow-up via Rigger only if terms require it).
6. **Command verification** (Q6): run read-only/idempotent commands on the day; Compose teardown never with `-v` (if doc shows `-v`, keep command, mark flag not run); mark host/cluster/real-library/secret/LocalPlay/mutation as not run with reasons.
7. **PRODUCT.md** (Wanted 7, Q8): copy `F:\Dev\LamuFlix\PRODUCT.md` verbatim into worktree root; read and scrub before commit; do not merge with `specs/PRODUCT.md`; do not modify main checkout; private data → stop and report to Bernstein.
8. **Subtitle docs test** (Wanted 5, Q9 amended): path to `docs/operations/subtitles.md` via `Path.Combine` segments; rename README-named identifiers/messages only; no heading locator added; assertions unchanged in count and meaning.
9. **Vocabulary** (Q10): re-point `file:` on exceptions at lines 16, 21, 63, 68, 132 when phrases move; same phrase and reason; no new/widened exceptions. `npm run check:vocabulary` in `web/`.
10. **DEV-558**: verified-vacuous (Q4); not a task.

**Out of scope**:

- Landing page, hero clip, measured comparisons, marketing copy, docs site, GitHub Pages.
- Screenshots of every page (one is enough).
- Changes to the app (`src/`, `web/src/`).
- New dependency or tooling (including link-check tooling).
- New seeding script.
- Missing `v0.1.0` git tag (noted, no ticket).
- Anything else is a follow-up candidate, not a finding; file follow-up only for Critical, High, or broken behaviour.

**Precedence**: Ticket Wanted / Done when / Where it lands are approved authority (`PRODUCT.md` §3). Brief Q1–Q10 and `CONCLUSIONS.md` govern plan boundaries.

**Dependencies**: Prefer after DEV-547 so quick start and CHANGELOG link name `v0.1.0` (ticket Needs; DEV-547 Done per recon). DEV-572 merge state recorded at Phase B pickup for the Q7 wording rule.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**README top block**
Ordered front matter of `README.md`: name + CI badge + one sentence; screenshot; quick start; what it does today; what it does not do; operations links; Data sources credits.
_Avoid_: marketing claims; usage numbers; testimonials; tag checkout; non-CI badges

**Operations doc**
One Markdown file under `docs/operations/` per moved README operations H2; H1 is the old H2 text; no index page.
_Avoid_: inventing an operations index; leaving section text only in README after the move

**Currency pass**
On-the-day check that every retained command, port, setting key, file path, and service name matches HEAD, with the PR command table recording ran / not run.
_Avoid_: claiming a command was run when it was not; running LocalPlay, secret-bearing, or Compose `-v` teardown

**Stand-in library screenshot**
Library-grid PNG under 1 MB showing placeholder movies seeded through the existing import API (ADR 0023), with the [assumed] caption under the image.
_Avoid_: real personal library data; new seeding script; multi-page screenshot sets

Consumed vocabulary (already in use; not redefined): PRODUCT.md copy rule, `Features:LocalPlay`, contract chain (untouched), ADR 0023, ADR 0030, CHANGELOG `v0.1.0`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - README top is short, current, and honest (Priority: P1) 🎯 MVP

An operator opening `README.md` sees name, CI badge, one sentence, current screenshot, a three-command-or-fewer quick start with `v0.1.0` changelog link, what the app does today, what it does not do, operations links, and Data sources credits — then short remaining links/sections.

**Why this priority**: Wanted 1 and the Goal are the primary deliverable; without a correct top, the rest of the move does not meet Done when.

**Independent Test**: Heading inventory and visual read of README top match the ordered Wanted 1 / brief Q7 list; PRODUCT.md copy rule holds; no tag checkout.

**Acceptance Scenarios**:

1. **Given** the delivery branch, **When** the README top is read in order, **Then** it contains exactly the seven top-block items from brief Wanted 1 / Q7 (name+CI+sentence; screenshot+caption; quick start ≤3 commands; what it does today; what it does not do; operations links; Data sources).
2. **Given** Phase B pickup, **When** the "What it does not do" list is written, **Then** if DEV-572 is merged it says "no video transcoding", otherwise it keeps the ticket's five gaps verbatim.
3. **Given** the quick start, **When** compared to HEAD, **Then** it uses `git clone`, `cd`, and the Compose up command as the Compose section has it (verified on the day), with no tag checkout, and links CHANGELOG naming `v0.1.0`.

---

### User Story 2 - Operations sections live under docs/operations (Priority: P1)

Each former README operations H2 is one click away as its own `docs/operations/*.md` file; no section text is lost; vocabulary exceptions follow moved phrases.

**Why this priority**: Wanted 2 and Done-when "one click away / no text lost" are the structural half of the overhaul.

**Independent Test**: Before/after heading inventory; every operations link from README resolves; `vocabulary-check.json` `file:` values match phrase locations; `npm run check:vocabulary` green.

**Acceptance Scenarios**:

1. **Given** the nine `ASSUMPTIONS.md` file names, **When** the move completes, **Then** each file exists with H1 equal to the old H2 text, subheads one level up, and no operations index file.
2. **Given** Docker Compose content, **When** the move completes, **Then** README keeps only the quick-start commands and the remainder is in `docs/operations/docker-compose.md`.
3. **Given** Architecture decisions prose beyond links, **When** the move completes, **Then** that prose is in `docs/architecture/README.md` and README keeps a one-line link to `docs/adr/`.
4. **Given** vocabulary exceptions at lines 16, 21, 63, 68, and 132, **When** a phrase moved, **Then** only its `file:` is re-pointed to the destination doc with the same phrase and reason.

---

### User Story 3 - Story, C4, and stale facts are current (Priority: P1)

The modernization story and C4 live under `docs/architecture/`; README :5, migration wording, and both diagrams credit OMDb and TMDb; `c4-container.md:15` is count-free.

**Why this priority**: Wanted 3–4 correct known stale claims called out in the ticket.

**Independent Test**: Diff of named lines; both diagrams include TMDb; relative links from `modernization.md` and `c4-container.md` resolve.

**Acceptance Scenarios**:

1. **Given** the 2018–2026 story and inline C4, **When** the move completes, **Then** they are in `docs/architecture/modernization.md` and README links to that file.
2. **Given** README :5 and migration claims in the story and `c4-container.md:15`, **When** the currency pass finishes, **Then** README :5 says "OMDb and TMDb" and migration wording is count-free (path to Migrations folder), and TMDb appears beside OMDb in both C4 diagrams.

---

### User Story 4 - Subtitle documentation test follows the moved text (Priority: P1)

`SubtitleSetupDocumentationTests` reads `docs/operations/subtitles.md` and keeps every existing assertion's count and meaning.

**Why this priority**: Wanted 5; Done when requires the test green against the moved text; test must stay green at every commit with the text move.

**Independent Test**: `dotnet test --filter "FullyQualifiedName~SubtitleSetupDocumentationTests"` green; diff shows path/`Path.Combine` segments and README-name renames only (Q9 amended).

**Acceptance Scenarios**:

1. **Given** `SubtitleSetupDocumentationTests.cs` at HEAD, **When** Subtitles leave README, **Then** the doc path is `Path.Combine(FindRepositoryRoot(), "docs", "operations", "subtitles.md")`, README-named identifiers and failure messages are renamed, and no heading locator is added.
2. **Given** the two documentation assertion methods and the no-committed-key guard, **When** the test runs, **Then** assertion count and meaning are unchanged and the guard applies to the subtitles doc only.

---

### User Story 5 - Credits, screenshot, and PRODUCT.md (Priority: P2)

OMDb/TMDb are credited per terms; the library screenshot is current and under 1 MB; root `PRODUCT.md` is tracked after scrub.

**Why this priority**: Wanted 6–7 and Q3 complete Done when but can follow the structural move.

**Independent Test**: PR body carries terms URLs, check dates, and verbatim credit wording; image size under 1 MB with [assumed] caption; `PRODUCT.md` tracked commit after full read/scrub.

**Acceptance Scenarios**:

1. **Given** OMDb and TMDb terms pages checked on the day, **When** Data sources is written, **Then** wording is quoted verbatim, URLs and dates are in the PR body, and a logo is added only if terms require one in documentation.
2. **Given** a stand-in library seeded via the existing import API, **When** `docs/images/library.png` is replaced, **Then** it is a PNG under 1 MB showing the library grid with the [assumed] caption.
3. **Given** `F:\Dev\LamuFlix\PRODUCT.md`, **When** it is copied into the worktree root, **Then** it is unchanged from the source, scrubbed for private data (stop to Bernstein if found), not merged with `specs/PRODUCT.md`, and tracked.

---

### User Story 6 - On-the-day verification and closing evidence (Priority: P2)

Commands are run or marked not run with reasons; links resolve; gates and vocabulary pass; Done-when evidence is in the PR body.

**Why this priority**: Done-when evidence and closing bar are required for ticket closure, after content lands.

**Independent Test**: Command table in PR body; link/image resolution over README, `docs/operations/*`, `modernization.md`, and `docs/architecture/README.md`; Roslyn/complexity/InspectCode on the changed `.cs`; `dotnet format`; `dotnet test`; `npm run check:vocabulary`; three axis reports; round cap 1+1.

**Acceptance Scenarios**:

1. **Given** every command left in README and `docs/operations/`, **When** verification runs, **Then** each is either run on the day or marked not run with the Q6 reason, and Compose teardown is never executed with `-v`.
2. **Given** the delivery diff, **When** gates run, **Then** the three static gates pass on the changed `.cs`, format and tests are green, vocabulary check is green, mutation is N/A (no production code), and DEV-558 is recorded verified-vacuous.

### Edge Cases

- DEV-572 not merged at Phase B: keep ticket five-gap wording (including "no transcoding"), not "no video transcoding".
- Terms require a documentation logo: add official asset under 1 MB; otherwise no logo file.
- Terms require in-app attribution: out of scope; escalate via Patron → Rigger only if required, else noted no ticket.
- Doc shows Compose `-v`: keep the documented command; mark the `-v` flag not run in the PR table.
- PRODUCT.md scrub finds private data: stop and report to Bernstein; do not silently edit.
- Architecture decisions is links-only: do not rewrite `docs/architecture/README.md` beyond what Q1 requires.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: README top MUST present the seven ordered items in brief Wanted 1 / Q7, with CI badge only and PRODUCT.md copy rule.
- **FR-002**: Quick start MUST be three commands or fewer (`git clone`, `cd`, Compose up as at HEAD), with no tag checkout, and MUST link CHANGELOG naming `v0.1.0`.
- **FR-003**: "What it does today" MUST derive from CHANGELOG `v0.1.0`; "What it does not do" MUST follow the Q7 DEV-572 wording rule.
- **FR-004**: Each README operations H2 MUST become one `docs/operations/<ASSUMPTIONS name>.md` file (H1 = old H2; subheads up one level; no index); Compose detail beyond quick start MUST live in `docker-compose.md`.
- **FR-005**: No operations or moved architecture-decisions prose MUST be lost; before/after heading inventory MUST appear in the PR body.
- **FR-006**: Relative links and images in moved docs MUST be re-pointed (including `observability.md` → `../observability/aspire-dashboard-traces.jpg`).
- **FR-007**: README :5 MUST say "OMDb and TMDb"; migration claims in the story and `c4-container.md:15` MUST be count-free; TMDb MUST appear in both C4 diagrams.
- **FR-008**: Modernization story and inline C4 MUST move to `docs/architecture/modernization.md`; `c4-container.md` MUST be updated for currency.
- **FR-009**: `docs/images/library.png` MUST be replaced in place as a PNG under 1 MB of the library grid against the ADR 0023 stand-in library, with the [assumed] caption.
- **FR-010**: Data sources MUST quote OMDb and TMDb terms wording verbatim from pages checked on the day; PR body MUST carry URLs, dates, and wording; logo only if terms require one in documentation.
- **FR-011**: On-the-day command table MUST record ran/not-run per Q6 (no `-v` teardown execution; no LocalPlay/secrets/host-cluster/real-library/mutation runs without the stated reasons).
- **FR-012**: Root `PRODUCT.md` MUST be a verbatim scrubbed copy of `F:\Dev\LamuFlix\PRODUCT.md`, tracked, not merged with `specs/PRODUCT.md`.
- **FR-013**: `SubtitleSetupDocumentationTests` MUST read `docs/operations/subtitles.md` via `Path.Combine` segments, rename README-named items only, add no heading locator, and keep assertion count/meaning (Q9 amended).
- **FR-014**: `vocabulary-check.json` exceptions at lines 16, 21, 63, 68, 132 MUST re-point `file:` only when phrases move; phrase and reason unchanged.
- **FR-015**: DEV-558 survival MUST be recorded as verified-vacuous (Q4) and MUST NOT be implemented as a task.
- **FR-016**: Nothing under `src/` or `web/src/` MUST change; no new dependency or tooling.

### Key Entities

- **Operations topic file**: One moved README H2 as a Markdown file under `docs/operations/`.
- **Heading inventory**: Before/after list of headings proving no text lost.
- **Command verification row**: File, command, ran or not run, result or reason.
- **Credit record**: Provider terms URL, check date, verbatim wording (and optional required logo).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: README top matches the seven-item Wanted 1 order with ≤3 quick-start commands and a `v0.1.0` changelog link.
- **SC-002**: All nine operations files from `ASSUMPTIONS.md` exist; every operations section is one click from README; heading inventory shows no lost section text.
- **SC-003**: Named stale lines (README :5, migration claims, `c4-container.md:15`) are corrected; both C4 diagrams include TMDb.
- **SC-004**: `SubtitleSetupDocumentationTests` passes against `docs/operations/subtitles.md`.
- **SC-005**: Screenshot PNG is under 1 MB with the [assumed] caption; Data sources credits and PR body terms evidence are present; `PRODUCT.md` is tracked.
- **SC-006**: Command table, link/image resolution, vocabulary check, format, `dotnet test`, and the three static gates on the changed `.cs` are evidenced; mutation N/A; three axis reports present; no unresolved Critical/High; round cap 1+1 respected.

## Assumptions

- Taste `[assumed]` items in `ASSUMPTIONS.md` (caption, Data sources heading/placement, today/not-do wording, nine file names) stand unless Gate 1 changes them.
- Grill-closed brief and CONCLUSIONS are complete for inventable behaviour; Phase B records DEV-572 merge state and pickup heading inventory against then-current main.
- Authoring source is README at delivery HEAD; main-checkout untracked `docs/operations/` is unused (`brief.md` Q1; recon).
