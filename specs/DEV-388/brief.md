# DEV-388 — Retire legacy Web and Worker hosts

Status: Grill complete; Quill drafting input. Nine of twelve permitted questions used. Patron confirmed shared understanding in Q9. This brief does not approve the spec, freeze the plan, open Gate 1, authorize implementation, clear a review, or authorize merging.

## Identity and evidence

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-388-spec`; branch: `feature/388-spec`.
- Ticket: DEV-388, parent DEV-286, size L, as recorded in note `DEV-388`.
- Factual sources: original intake recon sections 0–14 are summarized in note `DEV-388:26-45`; the current `recon-DEV-388` is the 75-line bounded adjudication recon, sections 1–5, replacing the previously unavailable note contents. Do not cite its current section numbers as the original intake sections. Recon is a bounded snapshot, not a delivery-head verification.
- Decision authority: Patron Q1–Q9 and the Step 6 adjudication rulings in `CONCLUSIONS.md`; full exchanges are appended there. Q3 supersedes Q1 only on director/collection parity. The Step 6 rulings below settle SPA DELETE 404 behavior and ADR lifecycle. Taste assumptions are in `ASSUMPTIONS.md`.
- Q3 provenance limit: section 14 verifies constitution governance at `.specify/memory/constitution.md:533-536`, the single Initial constraint at `:342`, and `specs/DEV-19/spec.md:135` SC-001. The external `F:/Dev/lamuflix-react-pivot-plan.md:33,47,94` primary text was outside recon roots. Its P2 quotations remain Patron-cited authority, not independently verified text. Do not claim otherwise.
- Preserve the pre-existing `harness.yml` diff; exclude it from DEV-388 commits. No threshold or harness configuration changes are approved.

## Outcome and frozen scope

Retire all of `src/LamuFlix.Web/` and `src/LamuFlix.Worker/` after proving the required Api and React SPA replacements. Deliver the missing metadata-only movie-delete capability through the existing backend and SPA. Preserve existing P1 browse/filter/search, details, play, folder import, watchlist add/remove and watchlist view behavior.

The final interpretation of list+filters is the existing P1 filter set: director/collection filters and facets are excluded under Patron Q3. Do not add collection persistence, schema changes or migrations. Delete remains expressly required by the ticket AC. Patron rules no owner checkbox or constitution departure is required.

Include selective legacy test/project-reference cleanup, narrowly removing README references to deleted projects, the retirement ADR, acceptance evidence and conditional post-merge DEV-370/DEV-385 reconciliation. No new dependencies, package/version changes, projects, architectural layers, repository/service abstraction, local playback execution change or unrelated UI restyling. Keep Api `Features:LocalPlay` and process execution unchanged. No secrets or connection strings may enter artifacts or commits.

Out-of-scope observations follow the Q8 disposition rule below; anything else is a follow-up issue, not a finding in this round.

## Loop discipline

- Closing bar for code-review/ship-review: no unresolved Critical, High or Medium findings within frozen scope. Failed or unrunnable required gates block delivery. Low may remain only as one `noted-no-ticket` line in `CONCLUSIONS.md`.
- Out-of-scope findings receive a follow-up ticket only for Critical, High or broken behavior, per Patron Q8. Other observations are noted without a ticket; they do not expand this round.
- Round cap: two correction rounds per review loop. Unresolved blockers at the cap stop delivery as blocked; do not create a third round or lower a gate threshold.
- M/L adjudication requires all three Sentry, Ledger and Compass reports, verification at file:line, and the findings/summary posted to the PR. A missing axis is blocked, not skipped. Persist `pr-review/findings@1` at the appropriate review stage.
- Gate labels stay literal: exit-2 scope-empty is `SKIPPED (scope-empty)`; configured opt-out is `SKIP`; mutation exclusions may be N/A; unrunnable is `Could not run`. None is PASS. No merging or auto-merge by seats; only the user merges.

## All grill answers

1. **Q1 — initial parity interpretation:** Patron initially required delete plus director/collection filters/facets, without an owner checkbox, on the every-action AC. **Director/collection part superseded by Q3.** Delete and unchanged Api playback stand.
2. **Q2 — delete contract:** `DELETE /api/movies/{id:int}`, no request DTO; 204 with no body on removal, 404 when absent, existing validation/problem conventions for invalid ids. Remove movie-scoped metadata/associations, never physical media. SPA details offers confirmation; success returns to library and invalidates cached results. Section 13 subsequently supplied the missing feasibility evidence.
3. **Q3 — final filter/schema boundary:** No director/collection filters/facets, collection storage, migration or schema change. Existing P1 filters satisfy list+filters under Patron's cited product-plan interpretation. No structural escalation. Carry the external-plan provenance limitation above.
4. **Q4 — selective test retirement:** Delete the six exact legacy test/model files listed below, remove Web/Worker/Data portions of mixed EnrichmentTests and Web/Worker ProjectReferences, retain unrelated tests and the legacy test project outside the solution. Close DEV-370/385 only after delivery PR merge and live state verification by Rigger.
5. **Q5 — files and documentation:** Delete whole legacy host trees including Library, Properties and appsettings. README changes remove only dead project references; full rewrite remains DEV-23. Do not edit the constitution. No manufactured solution/Docker/CI changes; recheck existing satisfaction at delivery. Preserve harness diff and all unrelated changes.
6. **Q6 — architecture and interaction:** Use existing Core feature/port, Infrastructure persistence and Api seams; add atomic race-free deletion, relying on verified cascades. No new repository/service layer. Generate OpenAPI and TS through tooling. Confirm/cancel/pending/success/error SPA behavior as below. Draft a retirement ADR; no glossary change because no new domain term is introduced.
7. **Q7 — verification:** Behavioral handler, PostgreSQL Testcontainers, contract and SPA tests; explicit acceptance mapping, complete configured backend/web gates and retirement inventories. No opt-in Gherkin lane requested.
8. **Q8 — review loop:** Approve the closing bar, frozen scope, two-round cap, three-axis PR-posted adjudication and no merge authority, with Low noted-no-ticket and out-of-scope follow-up restrictions above. Standing chain ruling, not renegotiated each round.
9. **Q9 — close and order:** Shared understanding confirmed. Approve ordering and file boundaries below; authorize final brief and `docs/adr/0021-retire-legacy-web-and-worker-hosts.md`. Quill drafts spec/plan/tasks; no owner checkbox presently required.

## Approach and contract

Implement a vertical delete slice within the existing architecture; exact class/member names follow existing conventions during implementation. Extend the existing persistence seam. Persistence must atomically report removal versus absence, avoiding a read-then-delete race. Recon section 13 confirms deletion of a movies row removes movie metadata/watchlist columns and cascades to movie_actors, movie_directors and movie_genres; shared actors/directors/genres survive. It does not establish historical MySQL cascade configuration and the plan must not claim it does.

Contract flows strictly C# endpoint/DTO surface → generated OpenAPI (`web/src/api/openapi.json`) → generated TypeScript (`web/src/api/types.ts`). Regenerate via existing tooling and update the committed contract snapshot; never hand-edit generated types or create duplicate handwritten API types.

SPA details: a destructive action opens confirmation; cancel sends no request; pending prevents duplicate submissions. Both 204 removal and 404 confirmed absence invalidate affected library/watchlist/details caches and return to the library. DELETE 404 shows no error banner and offers no retry. Other failures leave the screen usable with a visible error and retry. Keep existing P1 interactions intact; use the existing UI conventions rather than restyling the application. Taste/copy decisions remain explicitly tagged assumed in `ASSUMPTIONS.md`.

## Step 6 adjudication decisions and bounded evidence

- **SPA DELETE 404 — Patron ruling:** Treat absence as movie gone, invalidate library/watchlist/details caches and navigate to the library without error or retry. Keep visible error/retry for network, 5xx and other non-404 failures. Add a Vitest/RTL case proving the 404 path. Basis: `CONCLUSIONS.md:191-195`; the user's removal intent is already achieved and the API contract remains 204 removed / 404 absent. This clarifies Q2/Q6 within the frozen scope.
- **ADR lifecycle — Patron ruling:** ADR 0021 stays `Proposed (becomes Accepted when the DEV-388 Gate 1 spec PR merges)` in the spec PR. The decision becomes Accepted upon that merge. The delivery PR is permitted and expected to change only the status line to `Accepted`; a decision-text change requires a new Patron ruling. Basis: `CONCLUSIONS.md:197-200`, citing ADRs 0019/0020 and the need for the next PR to record the merge. Spec-stage format corrections do not authorize implementation-stage semantic rewrites. This brief grants no Gate 1 approval.
- **Id behavior and security:** Current recon section 1 (`:9-15`) reports GET movie ids <= 0 as 404 via `MovieId.TryCreate` / `NotFoundException`, non-integer input as framework 400 with sibling enrichment-route proof, no id-specific 422 branch, and no configured Api authentication/authorization. Preserve existing conventions; add no id rule, empty validator or auth mechanism. Delete endpoint tests must verify its own invalid-id outcomes rather than treating the sibling test as DELETE proof. Existing validation exception mapping elsewhere is not evidence of a reachable delete-id 422.
- **Selective legacy cleanup:** Current recon section 2 (`:21-40`) enumerates Web/Worker/Core/Infrastructure/Api and Tests.Common project references, with no direct Data project reference. Remove only the approved Web/Worker references. T015 removes the four `OmdbMetadataProvider_*` and five `EnrichmentJobProcessor_*` tests enumerated at recon `:37`, plus exclusively supporting Worker/Data declarations. T018 removes `FilmesService_CriarFilme_InsertsAsPendingAndPublishesMessage` at recon `:38` and remaining exclusively supporting Web/Data declarations. Retain the project, other files and assembly fixture unless exact recon establishes an approved removal; do not infer whole-file deletion from absence of Core usages. Inventory remaining members before each partial edit and preserve unrelated behavior.
- **Worker replacement evidence:** Current recon section 3 (`:46-53`) names `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs`, registration in `RabbitMqServiceCollectionExtensions.cs:43-67`, and Api `Program.cs:27-30` ordering. Before retirement retain named existing processing proof in `EnrichmentConsumerTests.cs` (successful enrichment/ack and retry cases listed at recon `:50`), `ApiEndToEndEnrichmentTests.cs`, and registration proof in `tests/LamuFlix.UnitTests/RabbitMq/RabbitMqServiceCollectionExtensionsTests.cs`, alongside the already required publisher/endpoint evidence. Recheck existing evidence; do not expand DEV-314/318 or create a new consumer.
- **Details cache ownership:** Current recon section 5 (`:65-68`) identifies `['movieDetails', movieId]` in `web/src/features/movie-details/movieDetailsApi.ts:64-66`, consumed by `MovieDetailsView.tsx:89-90`. The details view owns a local QueryClient (`:78-85`), distinct from the app-level client. T012 must invalidate the actual details cache and the affected library/watchlist caches through their existing owners; invalidating only an app-level key does not prove details invalidation. Preserve the approved architecture without a cache-library or provider rewrite.
- **Format and README evidence:** Current recon section 4 (`:57-60`) confirms `dotnet format --verify-no-changes` in CI `:35` and the constitution `:398`; the post-edit format hook is not that gate. Current section 5 (`:70-75`) pins README :10 to Web/Data legacy upgrade wording, :23 to the Worker project upgrade and :26 to QueueWorker test wording. Remove only the approved dead-host references/claims, retaining unrelated test-project content. Use this content as the edit boundary and the line numbers as snapshot locators; drift stops for recon.

Plan challenge remains not ready to freeze until Quill applies the consolidated accepted fixes and the corrected artifacts are checked. Resolved evidence limits do not constitute remediation completion, gate results or owner approval. Note `DEV-388:51` now correctly records selective cleanup with the legacy test project retained.

## Approved file boundaries

| Surface | Approved change |
| --- | --- |
| Existing Core features/ports, Infrastructure persistence, Api endpoints | Delete slice using existing seams; no new architectural layer or schema |
| UnitTests and IntegrationTests | Delete behavior, persistence and endpoint/contract tests; existing parity regression evidence |
| Integration contract snapshot | Regenerate/update to the approved DELETE contract |
| `web/src/api/openapi.json`, `web/src/api/types.ts` | Tool-generated contract updates |
| `web/src/features/movie-details/`, affected existing library/cache code and their tests | Confirmed delete action, mutation, cache/navigation behavior |
| `src/LamuFlix.Web/**`, `src/LamuFlix.Worker/**` | Full removal, including support/config/assets |
| `tests/LamuFlix.Test/UnitTest1.cs` | Delete |
| `tests/LamuFlix.Test/Library/LegacyMovieSortTests.cs` | Delete |
| `tests/LamuFlix.Test/Smoke/MovieServiceEfTests.cs` | Delete |
| `tests/LamuFlix.Test/WorkerTests.cs` | Delete |
| `tests/LamuFlix.Test/GenericRepositoryGetByIdTests.cs` | Delete: no-LamuFlix.Data AC and explicit Q4 ruling |
| `tests/LamuFlix.Test/ApiDataModel.cs` | Delete: orphan, explicit Q4 section 2.3 item 6 ruling |
| `tests/LamuFlix.Test/EnrichmentTests.cs` | Remove only Web/Worker/Data portions; preserve unrelated behavior |
| `tests/LamuFlix.Test/LamuFlix.Test.csproj` | Remove Web/Worker ProjectReferences; retain project and unrelated tests outside sln |
| `README.md` | Narrow removal of stale project references identified at recon lines :10,:23,:26; no full rewrite |
| `specs/DEV-388/`, `docs/adr/0021-retire-legacy-web-and-worker-hosts.md` | Decision/planning artifacts and retirement ADR |

Do not edit the constitution, migrations, solution or deployment/CI configuration under the frozen plan. Recon sections 1 and 8 find solution/deployment ACs already satisfied; verify them again. If new facts make an additional change necessary, request recon and a Patron decision rather than silently expanding the file set.

## Test strategy and gate expectations

- Handler tests verify success/absence outcome propagation. PostgreSQL Testcontainers tests prove 204 on removal, repeat/absent 404, deletion of movie/watchlist/metadata and all three join sets, survival of shared principals and another movie, and preservation of physical media. Do not mock the driver for cascade behavior.
- Contract tests and updated snapshot prove DELETE route/status/no-payload behavior. Vitest/React Testing Library cover confirm/cancel, no cancel request, duplicate prevention while pending, visible error/retry for non-404 failures, successful navigation and refreshed library/watchlist/stale details, plus DELETE 404 invalidation/navigation with no error or retry.
- Map list+P1 filters, search, details, play, folder import, watchlist add/remove and view to explicit existing regression evidence. Confirm Api import/enrichment replacements before deleting `MovieService.cs` or the Web publisher. Recon section 7 identifies handler/endpoint coverage plus RabbitMqPublisherTests and EnrichmentEndpointTests.
- Re-run solution restore/build/tests and configured web gates. Run applicable diff-scoped Roslyn, complexity, InspectCode, property, security/package, format and pre-PR mutation gates with configured thresholds. Use existing skill/script parameters, not assumed flags. Surviving mutants require tests; never lower thresholds.
- Verify no `Pomelo`, `UseMySql` or `LamuFlix.Data` anywhere under src/tests, no surviving Web/Worker ProjectReferences, and no Docker/compose/CI reference to the retired C# hosts. `docker/Dockerfile.web` is the React SPA build and remains.
- Baseline note reports 1082 passed, one skipped and scope-empty analyzer skips at its pinned head; that is historical intake evidence, not delivery verification or analyzer PASS.

## Task ordering and handoff

1. Quill drafts spec, plan and tasks from this brief, conclusions and ADR; Keel performs read-only analyze and alignment against the brief. Resolve decisions here before changing downstream artifacts. Complete plan challenge/freeze and the separate owner Gate 1 before implementation.
2. At Phase B pickup, obtain fresh main drift/overlap recon and rerun analyze; amend task planning only for demonstrated drift. Do not rely on intake PR/HEAD snapshots as current.
3. Backend behavioral tests and delete slice precede contract generation; OpenAPI precedes generated TS; frontend TDD consumes generated types.
4. Establish required P1 parity and confirm Api import/enrichment replacements before retiring Web, including its MovieService/publisher. Worker may retire earlier. Apply only approved selective legacy tests/reference cleanup; unrelated tests survive.
5. Remove README dead references; run final complete gates, acceptance mapping and retirement inventories. Complete required PR-posted review/adjudication with all axes and the round cap. User merges.
6. Only after delivery PR merge and target-file removal, Patron routes Rigger to query current DEV-370/385 states and conditionally close as no longer needed. Keel never writes YouTrack.

Any needed fact absent from the named recon notes is `needs recon: <question>` to Conductor, then stop; no independent searches or measurements. Patron decides within section 2.3, with only ticket contradiction or necessary constitution departure escalated to the owner. No such owner question remains under Q3/Q9. Quill's next work is drafting, not implementation.
