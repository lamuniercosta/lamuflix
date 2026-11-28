# DEV-559 conclusions (Patron rulings)

Grill: Keel with Patron, two rounds, 10 of 12 questions used. Worktree `feature/559-spec` at `e1649d533018ee7311144c8c3953895c8cfcb0e2`. Facts are from `recon-DEV-559`. Patron also verified some facts directly, as noted.

## §2.3 items

- **Item 2 (new top-level folder):** not triggered. `docs/operations/` is a subfolder of the existing `docs/` and is named in the ticket file map (Q1).
- **Item 5 (LocalPlay, secrets, Process.Start):** not triggered. No LocalPlay command is executed and no secrets are used during command verification (Q6).
- **Item 6 (deleting or rewriting an unnamed file):**
  - `PRODUCT.md` is named by Wanted 7. Adding it is not a rewrite (Q8).
  - `vocabulary-check.json` is unnamed, but editing it is forced by the CI vocabulary gate, so it is in scope. The edit is limited to re-pointing `file:` on the existing exceptions (Q10).
  - `docs/architecture/README.md` is not named in the file map. It receives architecture-decisions prose only if that section contains prose beyond links (Q1). This follows "no section text lost".
- **Items 1, 3, 4 (dependency, schema, API):** not triggered. No dependency is added, there is no schema or API change, and no link-check tooling is introduced (Q10).
- **Owner checkboxes:** one, the constitution departure in the Analyze Fix 1 ruling below.

## Rulings

| Q | Ruling | Basis |
|---|---|---|
| Q1 | ACCEPT with two corrections. Layout: one file per moved section, H1 = the old H2 text, no index file. Architecture-decisions prose beyond links goes to the existing `docs/architecture/README.md`, because `docs/adr/README.md` does not exist (Patron verified). `observability.md` must re-point relative image links (`../observability/aspire-dashboard-traces.jpg`); a broken image counts as lost content. | Wanted 1–2; Done-when "one click away / no text lost" |
| Q2 | ACCEPT. The story and its inline C4 go to `docs/architecture/modernization.md`. TMDb is added in both diagrams and at README :5. Migration wording becomes count-free, a correction of a stale fact (ticket 18 vs actual 21). | Wanted 3–4; ADR 0030 |
| Q3 | ACCEPT. Library grid, seeded through the existing import API per ADR 0023, with no new seeding script. The image replaces `docs/images/library.png` in place as a PNG under 1 MB. Caption wording is [assumed]. | Open point 1; Out of scope "Changes to the app" |
| Q4 | ACCEPT. CI badge only. DEV-558 never touched README.md: `git log --grep=DEV-558 -- README.md` returns 0 commits (Patron verified). The survival line is satisfied vacuously and is not a task. | Open point 2; Done-when last line |
| Q5 | ACCEPT. Credit wording is quoted verbatim from each terms page on the day, with the URL and date in the PR body. A logo is added only if the terms require one in documentation. A missing in-app attribution is out of scope: it gets a follow-up via Rigger only if the terms make it a requirement, otherwise noted, no ticket. | Wanted 6; Out of scope |
| Q6 | ACCEPT with one addition. Read-only and idempotent commands are run on the day. Host, cluster, real-library, secret and LocalPlay commands are marked not run, with the reason. Mutation testing is marked not run ("runtime cost; covered by CI"). Addition: Compose teardown runs without `-v`. If the doc shows `-v`, the doc keeps the command and the PR marks that flag not run. | Done-when 4; Wanted 3; §2.3 item 5 |
| Q7 | ACCEPT. The quick start has no tag checkout, because no `v0.1.0` tag exists. The missing tag is the user's release act: noted, no ticket. DEV-572 caveat ruled YES: if DEV-572 is merged when Phase B starts, the list says "no video transcoding", otherwise it keeps the ticket's five gaps verbatim. Docs describe HEAD as shipped, not plans. | Wanted 1; Problem bullets; PRODUCT.md copy rule |
| Q8 | ACCEPT. Copy the root `F:\Dev\LamuFlix\PRODUCT.md` verbatim, read it in full and scrub it before commit. Do not merge it with `specs/PRODUCT.md`. The main checkout stays untouched. If the scrub finds private data, stop and report to Bernstein; do not edit the owner's text silently. | Wanted 7; worktree rule |
| Q9 | ACCEPT. Change only the path constant and the heading-level helper. Assertions stay unchanged in count and meaning. The three gates run on that one .cs file. **Amended (Keel, drafting):** there is no heading-level helper at HEAD; the test does whole-file `Contains` (recon-DEV-559 line 15), so no locator is changed or added. Scope is the path (as `Path.Combine` segments to `docs/operations/subtitles.md`) plus renaming README-named identifiers and failure messages; assertions unchanged. See brief Q9. | Wanted 5; recon-DEV-559 line 15 |
| Q10 | ACCEPT with one addition. Round cap: 1 review and 1 remediation round, then residual blockers go to Bernstein. The `vocabulary-check.json` exceptions are file-scoped to README.md (entries at lines 16, 21, 63, 68 and 132; Patron verified). Each moved phrase gets its exception re-pointed to the new file, with the same phrase and reason. No new exceptions and no widened phrases. Local command: `npm run check:vocabulary` in `web/` (workflow `.github/workflows/vocabulary.yml`). | Done-when; CI gate; §2.3 item 6 |

Follow-ups: none filed. Noted, no ticket: the missing `v0.1.0` git tag.

## Analyze Fix 1 ruling (step 3.3): README Documentation Rule

**Verdict: (b) constitution departure. `blocked: structural`, one owner checkbox on the spec PR. Gate 1 stays closed until the owner answers.**

- `constitution.md:1072-1073` says the README MUST *carry* the modernisation narrative, C4 container diagram, trace screenshot, compose run, gate run and ADR links. A link to another file does not carry the content, so ruling (a) would read a MUST as a SHOULD. Patron does not loosen a constitution MUST by interpretation.
- The ticket (Wanted 2 and 4, brief Q1 and Q2) explicitly moves the narrative, C4 and operations sections (including the trace screenshot via `docs/operations/observability.md`) to `docs/`, leaving links and the quick start. Ticket text is approved, so Patron judges the departure necessary and keeps it. The ADR links and the compose quick start stay in the README; the narrative, C4, trace screenshot, full compose section and gate commands become links.
- `plan.md:55` cited a CONCLUSIONS ruling that did not exist. This section is now that ruling. Keel re-points the plan row to it and changes its verdict from PASS to `departure, owner checkbox`.

Owner checkbox text: `- [ ] DEV-559 departs from constitution Documentation Rules (constitution.md:1072-1073): README links to the modernisation narrative, C4 diagram, trace screenshot, full compose and gate instructions in docs/ instead of carrying them. Approve the departure, and amend the rule to README MUST carry or link to these items (amendment is a separate owner act, not part of DEV-559).`