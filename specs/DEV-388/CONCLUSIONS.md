# DEV-388 - Conclusions (Patron rulings)

## Q1 - Delete and director/collection filter+facet parity

**Ruling:** (Director/collection part SUPERSEDED by Q3. Delete stands.) Ticket-required. DEV-388 delivers movie delete (Api endpoint + SPA screen) and director/collection filters + facets (Api + SPA) before Web is retired. Existing Api playback and its `Features:LocalPlay` gating stay unchanged. No owner checkbox.

- Basis: DEV-388 AC, "every MoviesController action (list+filters, details, play, import folder, search, delete, watchlist add/remove, watchlist view) has an Api endpoint and an SPA screen". Delete is named outright, and Web `Index` filters on Director/Collection (`MoviesController.cs:22-23`, recon-DEV-388 section 5). Ticket text counts as decided, so this is not a §2.3(a) escalation.
- Narrowing parity, or moving the gaps to another ticket, would drop from what the ticket delivers. Only that would need `blocked: structural`.
- The route, verb, status codes and DTO fields for delete and for the director/collection facets are a §2.3 item 4 API-shape decision. Patron rules on them separately at spec stage. Neither needs a new project, layer or schema change (recon section 10).
---
### Q1 full grill exchange (Keel / Patron)

**Question 1/12 (Keel):** Recon sections 5-6 establish delete missing in Api and SPA, and director/collection filter and facet gaps. I recommend interpreting every MoviesController action and list+filters parity literally: implement those missing capabilities within DEV-388 before retiring Web, keeping existing Api playback LocalPlay behavior. This honors the ticket rather than weakening parity, but expands delivery beyond mechanical deletion. Do you rule full delete plus director/collection filter/facet parity as ticket-required, with no owner scope checkbox? Cite your basis.

**Answer (Patron):** YES, ticket-required, no owner checkbox. Deliver movie delete (Api+SPA) and director/collection filters+facets (Api+SPA) in DEV-388, with Api playback/LocalPlay unchanged. Basis: the AC names delete and list+filters for every MoviesController action, and Web Index filters on Director/Collection (MoviesController.cs:22-23). Ticket text is decided. Narrowing parity would be the escalation. Route/verb/status/DTO shape is a separate Patron API-shape ruling at spec stage.

**Rationale and implications:** Apply the Patron ruling above. Both gaps are prerequisites to retirement; Quill must not describe existing parity as complete.

## Q2 - Movie delete behavior and public contract

**Ruling:** Accept Keel's recommendation. The contract is `DELETE /api/movies/{id:int}` with no request DTO. It returns 204 with no body on removal and 404 when the movie is missing. Invalid ids follow the existing validation/problem conventions. Delete removes the movie row and its library associations and never touches physical media files. In the SPA, the details screen gets a destructive action with confirmation, then returns to the library with cached results invalidated.

- Basis: the legacy `MovieService.DeleteMovie` (`src/LamuFlix.Web/Services/MovieService.cs:328-334`) only removes the `Movies` row. It does no file I/O, so a metadata-only delete is parity, not a narrowing.
- Join-table cleanup is already `ReferentialAction.Cascade` in `Persistence/Migrations/20260930153520_Initial.cs:99-153`. No schema change or migration is needed (§2.3 item 3 clear).
- API shape (§2.3 item 4): this is the same resource as `GET /api/movies/{id:int}` (`LibraryEndpoints.cs:49`). Returning 404 instead of the legacy silent `Ok()` for a missing movie (`MoviesController.cs:112-123`) matches the Api's existing conventions. The contract chain runs C# → `openapi.json` → `types.ts`.

---
### Q2 full grill exchange (Keel / Patron)

**Question 2/12 (Keel):** Please settle delete behavior and public contract. I recommend DELETE /api/movies/{id:int}, no request DTO, 204 with no body when removed, 404 for a missing movie, and existing validation/problem conventions for invalid ids. Delete removes library metadata and dependent library associations, never physical media files; SPA details offers a destructive action with confirmation and returns to the library with cached results invalidated. Basis: named delete AC plus existing /api/movies/{id} resource and Api/SPA contract chain. Cost: association cleanup and absent/error cases need behavioral tests. Recon names legacy DeleteMovie at MovieService.cs:328 but does not describe its effects; if deciding parity requires those effects or persistence feasibility, identify the exact missing fact rather than assuming it. What is your ruling?

**Answer (Patron):** Accept your recommendation as stated: DELETE /api/movies/{id:int}, no DTO, 204 / 404 / existing problem conventions. It is a metadata-only delete that never touches files, and the SPA details screen gets a confirmed destructive action that returns to the library and invalidates the cache. No missing fact. Legacy DeleteMovie (MovieService.cs:328-334) only removes the Movies row, with no file I/O. Join tables already cascade (Initial.cs:99-153), so there is no schema change. 404 replaces the legacy silent Ok to match GET /api/movies/{id}. Confirmation copy is logged [assumed] in ASSUMPTIONS.md.

**Rationale and implications:** The public contract and interaction are Patron decisions. The factual assertions about legacy deletion and current relationship cascades are outside recon-DEV-388 sections 5 and 7, which only locate the method and list replacement gaps. Under the bounded-facts instruction these assertions await a recon addendum before supporting implementation planning or a no-schema-change conclusion. Grill paused after question 2 of 12; brief not complete.

---
### Q2 recon closure

Conductor resumed the grill after Wisp filed recon-DEV-388-2 section 13. Sections 13.1-13.2 now substantiate legacy metadata-only deletion and current movie-to-join cascades, with shared actor/director/genre rows surviving. Legacy cascade configuration remains unknown and is not claimed. Q2 no-schema-change reasoning is valid for deletion only. Section 13.2 also establishes that the current seven-table schema has no collection storage, so Q1 collection parity requires a separate explicit ruling.

## Q3 - Collection persistence; director/collection filters (amends Q1)

**Ruling:** DEV-388 delivers no director/collection filters or facets, and no collection persistence, migration or schema change. The ticket's `list+filters` AC is met by the existing P1 filter set on `GET /api/movies` plus `LibraryView`/`FilterPanel`. Movie delete (Q1/Q2) stays in scope. No structural escalation.

- Basis: the locked product plan `F:\Dev\lamuflix-react-pivot-plan.md:47` says "Deferred to P2: Rating range, Director facet, Collection facet", and `:94` lists Director/Collection filters as later work. `:33` says Collections start empty and are not migrated. The constitution (`.specify/memory/constitution.md:533-536`) says that plan owns WHAT is built and outranks other documents. The ticket AC lists the action (`list+filters`), not a filter set, so reading it against the plan is not a contradiction of ticket text.
- New fact since Q1 (recon-DEV-388-2 lines 17 and 33): there is no collection storage at all. Adding it would need a second migration, which conflicts with the constitution's `single fresh Initial migration` (`constitution.md:342`) and DEV-19 SC-001's seven-table set (`specs/DEV-19/spec.md:32,111,135`). That is a departure not worth making for a P2 feature, so the constitution question is moot.
- Q1's director/collection clause was ruled without those facts. Patron's earlier rulings are not precedent against new evidence. Director/Collection facets remain P2 product-plan work: noted, no ticket.

---
### Q3 full grill exchange (Keel / Patron)

**Question 3/12 (Keel):** The recon addendum proves current schema has seven tables and no collection storage, while legacy Web uses Collections plus Movie.CollectionId. Q1 requires full collection parity; we cannot claim that parity needs no schema change. I recommend retain Q1 delivery scope and deliberately authorize the minimum collection persistence and migration needed for faithful filtering/facets under section 2.3 item 3, but first rule whether the seven-table DEV-19/constitution design is a constitutional constraint requiring an owner checkbox. Basis recon-DEV-388-2:12,17,26,32-33. Cost: schema work and migration tests; no fabricated collection values from unrelated fields. What collection approach and any structural escalation do you rule? Facts must come from the recon notes; if a necessary fact is missing, identify it so I can request recon and stop.

**Answer (Patron):** Q3 amends Q1: no collection persistence, no migration, no schema change, and no director/collection filters/facets in DEV-388; no escalation. Basis: the locked product plan lamuflix-react-pivot-plan.md:47 defers the Director and Collection facets to P2 (also :94, and :33 says Collections start empty). Constitution :533-536 says the plan owns WHAT is built. The AC names the list+filters action, not a filter set, so the existing P1 filter set on GET /api/movies plus FilterPanel meets it. A collections table would also conflict with constitution :342 (single fresh Initial migration) and DEV-19 SC-001 (seven tables). Delete (Q1/Q2) stands. Q1 director/collection clause is superseded.

**Rationale and implications:** Patron explicitly changed the scope interpretation; Q1 is historical, not the final scope. The locked product plan and constitution passages used to support Q3 are not present in the authorized recon notes. Request those documentary facts before treating this amendment as a supported scope freeze. No schema or implementation work authorized by this grill. Paused at question 3 of 12 pending recon; brief remains incomplete.

---
### Q3 recon disposition on resumption

Recon-DEV-388-2 section 14 verifies constitution governance, migration constraints and DEV-19 SC-001 in roots. It explicitly does not verify the external locked product-plan primary text; its P2 quotations remain Patron-cited authority, not independently verified source content. Conductor instructs resumption after this addendum. Carry that provenance limitation into the brief; do not claim direct plan verification. Q3 remains the latest Patron ruling, superseding Q1 on director/collection parity.

## Q4 - Test cleanup boundary; DEV-370/DEV-385 closure

**Ruling:** Approve Keel's boundary exactly as proposed.
- Delete `UnitTest1.cs`, `Library/LegacyMovieSortTests.cs`, `Smoke/MovieServiceEfTests.cs`, `WorkerTests.cs`, `GenericRepositoryGetByIdTests.cs` and `ApiDataModel.cs`.
- Remove the Web, Worker and Data parts of `EnrichmentTests.cs`, and remove the Web/Worker `ProjectReference`s.
- Keep `tests/LamuFlix.Test` and its other tests. It stays outside the sln, as it is today.
- Close DEV-370 and DEV-385 only after the DEV-388 PR merges with their target files gone. Rigger checks the live state of each ticket first.

- Basis: ticket Scope 3-4 names the Web-only tests, `WorkerTests.cs`, the Worker parts of `EnrichmentTests.cs` and the csproj reference edit. `GenericRepositoryGetByIdTests.cs` is a forced edit under the AC "no `LamuFlix.Data` ... under `src/` or `tests/`" (recon section 4).
- `ApiDataModel.cs` (§2.3 item 6): no file references it (recon section 9) and it is DEV-385's last remaining target. Deleting this dead test code lets the ticket's own Scope 3 closure clause ("close DEV-370/DEV-385 ... if their files are gone") apply.
- Keeping the project rather than deleting it follows Scope 4, which edits `tests/LamuFlix.Test/LamuFlix.Test.csproj`. The leftover tests running outside CI is pre-existing: noted, no ticket.

---
### Q4 full grill exchange (Keel / Patron)

**Question 4/12:** Approve bounded cleanup: delete Web-only UnitTest1.cs, Library/LegacyMovieSortTests.cs, Smoke/MovieServiceEfTests.cs; WorkerTests.cs; remove retired Web/Worker/Data portions of mixed EnrichmentTests.cs; delete GenericRepositoryGetByIdTests.cs under the no-LamuFlix.Data AC; remove Web/Worker ProjectReferences but retain tests/LamuFlix.Test and unrelated Core/Infrastructure/Api tests. Delete orphan ApiDataModel.cs by explicit section 2.3 item 6 ruling. Basis recon sections 4,7,9,10 and scope 3-4. Cost: selective cleanup; remaining project stays outside sln. Approve this exact boundary and conditional DEV-370/385 closure after target removal and Rigger live-state checks?

**Answer (Patron):** Approve the exact boundary. Delete UnitTest1, LegacyMovieSortTests, MovieServiceEfTests, WorkerTests, GenericRepositoryGetByIdTests (forced by the no-LamuFlix.Data AC) and ApiDataModel.cs (section 2.3 item 6: no references, recon section 9, last DEV-385 target). Strip Web/Worker/Data parts of EnrichmentTests and Web/Worker ProjectReferences. Keep tests/LamuFlix.Test outside the sln, since Scope 4 edits its csproj. Close DEV-370/385 per Scope 3 only after the DEV-388 PR merges with their files gone, and Rigger checks live state first.

**Rationale and implications:** Preserve unrelated tests and ticket-implied project retention. Tracker closure is a post-merge Rigger action, not a spec-stage mutation.

## Q5 - File boundaries and restrictions

**Ruling:** Approve with one change: `.specify/memory/constitution.md` is **not** edited. Everything else stands as Keel proposed:
- Delete all of `src/LamuFlix.Web` and `src/LamuFlix.Worker`, including `Library/`, `Properties/` and the `appsettings*` files.
- Edit `README.md:10,23,26` only to remove references to the deleted projects.
- Leave the sln, Docker, compose and CI unchanged, and re-check them at delivery.
- Keep the pre-existing `harness.yml` diff out of DEV-388 commits.
- No package, dependency, project, layer, migration, LocalPlay or `Process.Start` changes.

- Basis: ticket Scope 1 and 4 delete both projects. The `Extensions` list there illustrates what goes, it is not a limit. Recon sections 1 and 8 show the sln, Docker and CI ACs are already met.
- README (§2.3 item 6): these lines would point at files this ticket deletes, so they are dead references it creates. The full README rewrite stays with DEV-23, as `constitution.md:23-24` assigns it.
- Constitution: lines 22-23 are the Follow-up list of the 1.3.0 Sync Impact Report, a historical record. Line 22 already says DEV-388 deletes the Worker folder. Editing the constitution would trigger the amendment procedure (`constitution.md:538-543`), which nothing here requires.

---
### Q5 full grill exchange (Keel / Patron)

**Question 5/12:** Authorize only retirement-fact edits at README.md:10,23,26 and constitution.md:22-23 (recon section 2), removing obsolete legacy references without changing principles; section 2.3 item 6 ruling needed. Leave sln/Docker/compose/CI unchanged since already satisfied, reverify at delivery. Preserve/exclude pre-existing harness.yml changes. No package/version/dependency/project/layer/migration/LocalPlay/process-start changes. Delete whole Web/Worker trees including Library/Properties/config omitted from illustrative ticket enumeration. Approve?

**Answer (Patron):** Approve with one change: do NOT edit constitution.md. Lines 22-23 are a historical Sync Impact Report Follow-up list; leave it intact. README.md:10,23,26: remove only references to deleted projects under section 2.3 item 6, with full rewrite left to DEV-23. Everything else approved: whole Web/Worker trees, sln/Docker/CI untouched and rechecked, harness.yml kept out of commits, no package/project/layer/migration/LocalPlay/Process.Start change.

**Rationale and implications:** Conservative documentation boundary; no constitutional amendment. The historical-report explanation is Patron-cited; no additional source verification claimed. Preserve all unrelated changes.

## Q6 - Implementation approach

**Ruling:** Approve Keel's approach as stated:
- A vertical movie-delete slice that extends the existing movie port and persistence. No new repository or service layer.
- An atomic single-row delete that reports a missing movie without a read-then-delete race and relies on the DB cascades.
- The OpenAPI and TypeScript contract regenerated through the existing tooling, with the contract snapshot updated.
- SPA confirmation behaviour: cancel sends nothing, one request at a time, success invalidates the cache and returns to the library, and a failure shows a visible error.
- An ADR under `docs/adr`. No glossary change.

- Basis: this is the §2.3 item 2 test. It adds no project or layer and stays inside existing Core, Infrastructure and Api seams (recon section 10 item 2). The contract chain rule is C# DTO → `openapi.json` → `types.ts`, and generated types are never hand-edited.
- Cascades verified: recon-DEV-388-2 section 13.2 (`Initial.cs:94-153`, snapshot `:234-270`). Shared actor, director and genre rows survive.
- The ADR records parity-before-retirement and why delete is metadata-only. The Q3 product-plan citation is Patron-cited, outside the recon roots.

---
### Q6 full grill exchange (Keel / Patron)

**Question 6/12:** Approve a vertical movie-delete slice within existing Core feature/port, Infrastructure persistence and Api endpoint conventions, extending the existing persistence seam without a new repository/service layer. Atomically remove one movie, report absence without read-then-delete race, use verified cascades, preserve shared actors/directors/genres and files. Regenerate OpenAPI then TypeScript through tooling and update contract snapshot. SPA confirmation: cancel sends nothing, one request while pending, success invalidates cache and returns to library, failure retains usable screen with visible error. Exact names follow existing conventions at implementation. Draft L-ticket ADR under docs/adr for parity-before-retirement and metadata-only delete, labeling Q3 plan provenance. No new domain term/glossary change. Cost: backend/contract/frontend work beyond deletion.

**Answer (Patron):** Approved as stated. Existing Core port, Infrastructure persistence and Api seams, no new repository/service layer (section 2.3 item 2 clear). Atomic race-free delete relies on verified cascades (recon-DEV-388-2 section 13.2). Regenerate OpenAPI/TS via tooling and update snapshot; no hand edits to generated types. Approve SPA behavior and ADR with Patron-cited Q3 provenance. No glossary change.

**Rationale and implications:** Reuse architecture and authoritative contract chain; implementation must exercise actual persistence semantics.

## Q7 - Test strategy and delivery gates

**Ruling:** Approve Keel's verification bar as stated:
- **Backend tests:** handler tests, plus PostgreSQL Testcontainers integration tests covering 204 then 404, an absent id, row and column removal, all three join cascades, shared principals and other movies surviving, and the media file staying on disk.
- **Contract:** contract and snapshot tests for DELETE with no payload.
- **SPA:** Vitest/RTL tests for the confirm/cancel/pending/error/navigation/cache flows.
- **Evidence:** existing parity regression evidence kept, and every AC mapped to explicit evidence.
- **Gates:** the full gate set at pre-PR with thresholds unchanged, plus the Pomelo/UseMySql/LamuFlix.Data and Web/Worker reference re-checks.
- **Status labels:** SKIPPED, SKIP/N/A and Could not run reported as three different states.
- **No Gherkin lane.**

- Basis: the CLAUDE.md static-analysis gates are required, and "never treat a skipped gate as a passed gate". The ticket AC needs a clean build, passing tests and the zero-reference re-checks.
- Testcontainers PostgreSQL matches the constitution's database mandate (`constitution.md:342`), and only the database proves the cascade behaviour (recon-DEV-388-2 section 13.2).
- Gherkin stays opt-in (the `gherkin` skill), and nobody asked for it.

---
### Q7 full grill exchange (Keel / Patron)

**Question 7/12:** Approve handler outcome tests; PostgreSQL Testcontainers tests for successful 204 then repeat 404, absent id, movie/watchlist/metadata removal, all three join cascades, shared principals and another movie surviving, physical media preserved; DELETE contract/snapshot with no payload; Vitest/RTL tests for confirm/cancel, pending duplicate prevention, error retry, navigation/cache refresh including watchlist/stale details. Retain browse/search/details/play/import/watchlist and Api queue replacement regression evidence; map every AC to explicit evidence. Run full solution build/tests, web gates, diff-scoped Roslyn/complexity/InspectCode, property/security/package/format gates and configured pre-PR mutation without threshold reductions. Recheck no forbidden src/tests references and no Web/Worker deployment/project refs. Distinguish scope-empty SKIPPED, disabled SKIP, mutation N/A and Could not run from PASS. No opt-in Gherkin lane. Approve?

**Answer (Patron):** Approve the verification bar as stated. Basis: required gates and skipped never passed; AC clean build/tests/zero-reference rechecks; PostgreSQL Testcontainers for actual cascades (recon-DEV-388-2 section 13.2). Gherkin remains opt-in and was not requested.

**Rationale and implications:** Baseline counts do not prove delete behavior or delivery-head gates. Acceptance requires both behavior and retirement inventory evidence.

## Q8 - Loop terms (closing bar, frozen scope, round cap)

**Ruling:** Approve Keel's terms with one clarification. A Low finding left open is recorded as one `noted, no ticket` line in this file, not as a YouTrack follow-up. Anything outside the frozen scope gets a follow-up ticket only if it is Critical, High or broken behaviour.

- **Closing bar:** no Critical, High or Medium finding left open in the frozen scope. A failed or unrunnable required gate blocks; a configured skip keeps its exact label.
- **Frozen scope:**
  - In: Q1/Q2 delete across backend, contract and SPA; full retirement of Web and Worker; Q4 tests; Q5 README; post-merge closure of DEV-370 and DEV-385; Q6 ADR.
  - Out: director/collection facets, collection schema, new layers or dependencies, and unrelated cleanup.
  - Existing P1 behaviour must not regress.
- **Round cap:** two correction rounds per review loop. Blocking findings still open at the cap stop delivery, reported as `blocked` to Bernstein. M/L adjudication uses the Sentry, Ledger and Compass reports, file:line verification and PR-posted outcomes. Nobody gets merge authority.
- **Basis:** the Patron role's follow-up-ticket rule (Critical/High/broken only, otherwise `noted, no ticket`); "A review round is not finished until it is on the PR"; "Never merge"; frozen scope taken from rulings Q1-Q7 above. This is the standing ruling for this chain and is not re-ruled per round.

---
### Q8 full grill exchange (Keel / Patron)

**Question 8/12:** Approve closing bar: no unresolved Critical/High/Medium in scope; Low documented nonblocking. Failed/unrunnable required gates block; nonblocking skips preserve labels. Frozen scope covers delete backend/contract/SPA/evidence, legacy project retirement, selective tests, README dead refs, conditional post-merge tracker reconciliation, ADR/planning docs, and existing P1 parity regression protection. Exclude director/collection facets/schema, new layers/dependencies and unrelated cleanup. End frozen scope with: anything else is a follow-up issue, not a finding in this round. Two correction rounds per review loop; blockers at cap stop delivery, never round three. Require all Sentry/Ledger/Compass reports, file:line verification, PR-posted adjudication; no merge authority. Approve?

**Answer (Patron):** Approve with clarification: open Low is one noted-no-ticket line in CONCLUSIONS.md, not a YouTrack follow-up. Out-of-scope items receive a follow-up ticket only for Critical, High or broken behavior under the Patron follow-up rule. Closing bar, frozen scope, two-round cap stopping blocked, all three axes with PR-posted adjudication and no merge authority approved. Standing ruling for this chain.

**Rationale and implications:** Do not inflate low-priority observations into tracker work or bypass the cap. Out-of-scope follow-up disposition follows the severity/behavior rule.

## Q9 - Shared understanding; finalize brief and ADR

**Ruling:** I confirm the shared understanding: Keel's ordering, file set and the provenance limit on the Q3 citation are approved. Keel is authorized to finalize `brief.md` and the ADR. No owner checkbox is required. One correction: the ADR follows the existing naming, `docs/adr/0021-<kebab-subject>.md` (for example `0021-retire-legacy-web-and-worker-hosts.md`), not `DEV-388-retire-legacy-hosts.md`.

- Basis: `docs/adr/` numbers its files `NNNN-kebab-subject.md` and the latest is `0020-request-correlated-import-enrichment-proof.md`. Ordering the parity evidence before the Web deletion follows ticket Scope 2 ("before deleting ... confirm the Api import flow has its own replacement").
- Rulings Q1-Q8 raise neither §2.3 escalation (a change to the ticket text, or a constitution departure), so Gate 1 needs no `blocked: structural` checkbox.

---
### Q9 full grill exchange (Keel / Patron)

**Question 9/12:** Confirm final ordering: Quill drafts spec/plan/tasks from brief and ADR; Keel analyzes read-only against brief; plan challenge/freeze then owner Gate 1. Phase B fresh main drift/overlap recon and analysis precede implementation. Backend tests/slice then OpenAPI/TS generation then frontend TDD. Verify import/enrichment replacements and full P1 parity before deleting Web/MovieService/publisher. Worker may retire earlier. Approved selective test/README cleanup then final gates and PR-posted reviews. Rigger closes DEV-370/385 only post-merge with live-state checks. File set: existing Core features/ports, Infrastructure persistence, Api endpoints; Unit/Integration tests and contract snapshot; generated openapi/types; web details/cache/tests; whole legacy trees and Q4 tests/csproj; README; specs/DEV-388 and a retirement ADR. No independent unknown-fact searches; recon through Conductor. No owner checkbox under current rulings; no unrelated UI restyling. Confirm shared understanding and authorize final brief/ADR carrying Q3 provenance limit?

**Answer (Patron):** Shared understanding confirmed. Ordering, file set and Q3 provenance limit approved; finalize brief.md and ADR; no owner checkbox required. Correction: use docs/adr convention, 0021-kebab-subject.md, for example 0021-retire-legacy-web-and-worker-hosts.md.

**Rationale and implications:** Grill complete at 9 of 12 questions. Quill may draft from the final brief; this is not plan freeze, Gate 1 approval, implementation authority, review clearance or merge authority. ADR filename is an explicit Patron direction.

## Step 6 adjudication - SPA delete 404 and ADR 0021 status

**Ruling 1 (SPA DELETE 404):** A 404 from the details-view DELETE means the movie is gone. Handle it like 204: invalidate the library, watchlist and details caches and navigate to the library, with no error banner and no retry. The Q2/Q6 visible-error-plus-retry path stays for every other failure (network, 5xx, problem responses other than 404).

- Basis: Q2 fixes the API contract (204 removed, 404 absent) and says nothing about how the SPA treats 404. Q6's "failure" means the user's intent was not achieved, and after a 404 the intent (movie absent) is achieved. Retrying a confirmed-absent resource can never succeed, so offering a retry would be a dead end, and keeping the stale details view would show a movie that no longer exists.
- Within frozen scope (Q8): this is SPA behavior on the already-approved contract. It adds no API shape and no new copy.
- Test delta: the Q7 Vitest/RTL set gains one case. DELETE 404 must produce cache invalidation plus navigation to the library, with no error shown.

**Ruling 2 (ADR 0021 status):** The decision becomes Accepted when the Gate 1 spec PR merges, because that merge is the owner's approval of the decision. In the spec PR, the status line reads `Proposed (becomes Accepted when the DEV-388 Gate 1 spec PR merges)`. The delivery PR is permitted, and expected, to make a status-only edit to `Accepted`. That edit changes only the status line. Any change to the decision text needs a new Patron ruling.

- Basis: repo precedent ties ADR status to Gate 1 (`0019`: "Proposed (pending Gate 1 spec PR merge)"; `0020`: "decision approval depends on Gate 1 spec merge"). A PR cannot record its own merge, so the flip falls to the next PR on the chain, which is the delivery PR.
- This is a forced edit under Section 2.3 item 6 (it records the approved status) and is not a content rewrite.
