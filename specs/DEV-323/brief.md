# DEV-323 - Movie Details and Playback brief

## Closure and authority

Keel closes Phase 2 grill on 2026-10-12 at 12/12 questions. Worktree: `F:/Dev/LamuFlix.worktrees/feature-323-spec`; branch: `feature/323-spec`; reviewed HEAD: `30dc9e134522f3bf4df1b14c4bace6b44ca08edc`; intake base: `cb6fe48996cd16027f5c0496876ec5c86d97f7a2` (DEV-323 note:21; recon-DEV-323:67).

Authority: ticket description in DEV-323:8; specs/PRODUCT.md sections 2-5; lamuflix-team-charter:7-31; task-pipeline:23-47,66-107; Patron CONCLUSIONS.md Q1-Q12 and ASSUMPTIONS.md. Facts come only from recon-DEV-323 and recon-DEV-323-full. The full note contains the original 64-line recon; the Q5/Q7 supplement is only in recon-DEV-323:66-83. Its absence from recon-DEV-323-full is not filled by inference. Original recon describes the removed build worktree; DEV-323:18-23 and recon-DEV-323:67 identify the corrected spec worktree.

This brief permits Quill drafting. It does not establish plan freeze, gate1: provisional, Gate 1 approval, build authorization, passing gates, or delivery. Palette design.md is required before plan challenge on this size:M, ui: ticket (DEV-323:6; task-pipeline:79). No ADR is required by Q12. No production changes, Spec Kit commands, gates or commits are part of this closure ask. Preserve the pre-existing .specify/feature.json modification.

## Open owner checkbox

- [ ] **blocked: structural — Drop `backdrop`, `tagline` and `writers` from DEV-323 and deliver them in a follow-up ticket?**

The ticket names all three (DEV-323:8), but storage and wire fields are absent (recon-DEV-323:18-20,58). This is a ticket-delivery change under PRODUCT section 5. Patron recommends yes and the draft proceeds on that recommendation. The checkbox remains unanswered; Gate 1 stays closed. No placeholder or partial rendering for these fields. If no, re-plan the schema/enrichment slice before Gate 1; if yes, Rigger records the follow-up after the owner decision. Neither this brief nor a Patron ruling checks the box.

## Loop discipline

- Closing bar: no unresolved Critical, High or Medium in-scope findings; all applicable gates meet task-pipeline Phase 3 step 5 with accurately reported verdicts. Low findings may remain documented. M review needs all three reports from Sentry, Ledger and Compass before adjudication. A missing report is blocked: missing axis <name>. Findings and summary require a posted/read-back PR receipt before review closure.
- Review cap: two rounds; at most two remediation fix commits per round. Third-round findings go to owner review on the PR, rather than another autonomous round (task-pipeline:25-27).
- Frozen scope: additive Movie Details metadata projection and response, its generated contract updates, a reachable MovieDetailsView and its load states, existing POST Playback integration and accessible 403 modal, focused backend/RTL/MSW tests and forced contract/App edits. The three excluded display fields remain subject to the open owner checkbox. DEV-320 owns codegen tooling; no new router, competing client/generator or state library beyond the constitution-mandated TanStack Query, schema, playback backend changes, feature-flag endpoint, secrets, legacy MVC changes or new architectural layer. anything else is a follow-up issue, not a finding in this round.
- The closing severity bar and concrete startup mount below are Keel recommendations at the exhausted grill cap, tagged [assumed]; the standing round and fix caps are mandatory, not new questions.

## Q5 and Q7 resolution from supplementary recon

Q5: NO OWNER FOUND in these roots for SPA routing or MovieDetailsView mounting (recon-DEV-323:71-76). DEV-20 explicitly excludes routing. Apply Patron Q5 conditional fallback: App.tsx mounts MovieDetailsView with movieId, without any router dependency. App.test.tsx changes are forced by the resulting entry behavior and its tests (recon-DEV-323:49-50; charter:17). main.tsx remains the existing App mount.

[assumed] Minimal entry contract: at initial page load, App recognizes `/movies/<positive integer>` from window.location.pathname and passes that id to MovieDetailsView. Other paths retain the existing readiness view. No hardcoded selected movie, automatic first-movie selection, browse UI, navigation framework or client-side navigation history is added. Test this App integration with an explicit location and restore it after the test. This is a startup mount decision, not a claim that deployment rewrites or a router already exist. Palette documents this entry path and the component states; it must surface any deployment requirement as needs recon before asserting direct-link hosting support.

Q7: DEV-320 owns generated TypeScript API client work through owner-ticked OD-1 (recon-DEV-323:78-81). Apply Patron Q7 conditional deferral: DEV-323 adds no codegen npm dependency, chooses no package pin and creates no competing generator/script. DEV-320 parent status under DEV-286 is NOT EVIDENCED IN THESE ROOTS (recon-DEV-323:82). Codegen is NOT DELIVERED at this HEAD: types.ts, generator dependencies and script are absent (recon-DEV-323:83). Do not describe DEV-320 as merged, complete, or a verified DEV-286 sibling.

Dependency ordering: obtain DEV-320 delivered tooling and generated types in the build base before frontend typed implementation. Wisp verifies that prerequisite during pickup/drift recon; an absent prerequisite is needs recon: where is the delivered DEV-320 codegen prerequisite for DEV-323? Stop dependent build work until resolved. Quill can draft now with this explicit dependency. After DEV-323 DTO changes, regenerate the committed OpenAPI document/snapshot via the existing contract regeneration mechanism and regenerate types.ts with DEV-320 tooling. Consume the delivered DEV-320 openapi-fetch client and generated contract types through TanStack Query with Zod validation, per the Patron amendment below. Consume generated MSW handlers for tests; do not introduce a competing client or generator in DEV-323. Ownership of codegen infrastructure stays DEV-320, while regenerating output for this changed contract is a forced DEV-323 contract-chain task.

## Plan decisions and file scope

Implement within the existing Core/Infrastructure/Api boundaries and the feature-local web folder. New feature files are `web/src/features/movie-details/MovieDetailsView.tsx`, `MovieDetailsView.test.tsx`, `movieDetailsApi.ts`, and `MovieDetailsView.css` for the view layout/states. The feature API module consumes the DEV-320 openapi-fetch client and generated TypeScript types for GET details and POST play, with TanStack Query for server state and Zod validation. Use relative `/api/...` URLs; no duplicate DTO declarations or competing generic client abstraction.

Backend existing-file edits, scoped by Q1 and recon-DEV-323:17-20,30-34,58-59:

- `src/LamuFlix.Core/Domain/MovieMetadata.cs`: carry nullable PosterUrl, RottenTomatoesRating, MetaScore and non-null Directors/Actors collections; empty lists for absent people. Keep existing fields and behavior.
- `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs`: project those existing stored fields/joins in GetDetailsAsync without schema/migration or enrichment changes. Preserve missing/partial metadata behavior.
- `src/LamuFlix.Api/Endpoints/LibraryResponses.cs`: add nullable posterUrl, rottenTomatoesRating, metaScore and directors/actors string arrays to MovieMetadataResponse and map them. Preserve outer response, metadata nullability, route and status codes.
- `tests/LamuFlix.IntegrationTests/EfMovieCatalogDetailsTests.cs`, `LibraryEndpointsTests.cs`, `ApiEndToEndLibraryTests.cs`: extend real persistence, endpoint mapping and exact JSON shape proofs for the new fields, null scalars and empty collections.
- `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs`: use its existing regeneration facility; source modification is not preauthorized merely to refresh artifacts. Regenerate `web/src/api/openapi.json` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` through that facility.
- `web/src/api/types.ts`: generated output only, updated via delivered DEV-320 tooling, never hand-edited.
- `web/src/App.tsx` and `web/src/App.test.tsx`: the Q5 fallback mount and its tests, retaining readiness behavior outside the recognized details path.

Core port MovieDetails already carries MovieMetadata (recon-DEV-323:17); no IMovieCatalog signature change is decided. Core metadata constructor/invariant test or caller adjustments forced by the additive fields must be located by Wisp before editing; their exact additional file paths are not found in the supplied recon. Prefer an additive compatible construction change where valid. Quill must not invent extra caller/test paths. No new NuGet dependency or competing codegen npm dependency/script is authorized. TanStack Query and Zod use is authorized by the Patron amendment; DEV-320 owns openapi-typescript/openapi-fetch tooling and generated MSW infrastructure. Exact dependency pins and any forced package/provider file changes are not found in these roots: Wisp must reconcile the delivered base and identify those paths before dependent edits. main.tsx, vite.config.ts and global index.css edits remain outside this decision. If a gate or delivered dependency forces another tracked-file edit, cite its evidence and record the decision before expanding tasks.

Playback backend, FeatureOptions, Process.Start, secrets, legacy MoviesController/MovieService and schema remain untouched. The server-side LocalPlay adapter is authoritative (recon-DEV-323:23-25,44,60-61). The Play button is present in the loaded view, enabled outside an in-flight request, and never preemptively disabled by a client flag. No new feature-flag endpoint.

## Test strategy

Web uses existing RTL, Vitest and MSW. Exercise full metadata render, partial/null metadata, conditional badges/fields, placeholder poster, title/format fallback, loading, GET 404 and other/network errors. GET fixtures use generated contract types, Zod validation, and generated MSW handlers from DEV-320. Scope handler overrides to these API operations; do not replace the synthetic harness handlers globally.

Playback tests cover 204 inline success; 403 dialog title/body, focus on Close, Escape/Close dismissal and return to Play; 404 inline removal message; 400/422/5xx/network generic error; Starting state and prevention of concurrent requests. An accessible dialog also keeps keyboard focus inside it while open. Test initial App path-to-prop mounting and unchanged readiness rendering outside that path. Use the constitution stack for requests; the hand-built dialog requires no UI-library dependency.

Backend xUnit proves projection against real PostgreSQL using existing integration infrastructure, mapping, nullable fields, people arrays, missing metadata and exact frozen shape. Preserve existing GET 404 semantics and Playback 204/403/gate/no-process proofs. No data-driver mocks for query translation or serialization. Regenerate contract artifacts from C# and prove drift equality before generating TypeScript; compile/typecheck web against the new output.

## Gate expectations and task ordering

1. Quill drafts spec/plan/tasks from this brief and named evidence; no gap is silently filled. Palette supplies design.md before challenge. Keel runs read-only speckit-analyze and checks against brief; Challengers review plan; Patron provisional decision and Rigger spec PR follow. Open owner checkbox and user-owned merged spec PR are separate Gate 1 requirements.
2. Before Phase B, Wisp reconciles the approved spec with the current build base and verifies delivered DEV-320 tooling, openapi-fetch client and generated MSW handlers plus the TanStack Query/Zod integration prerequisites. Record actual prerequisite evidence, not this HEAD as proof of future readiness.
3. Implement/test additive Core data and EF projection, then API mapping and frozen-shape tests. Regenerate OpenAPI/snapshot, then generated types with the prerequisite tooling. Frontend implementation consumes those generated fields through the DEV-320 openapi-fetch client, TanStack Query and Zod, with generated MSW handlers and web-implement TDD. Mount through App and prove entry behavior. No hand-written substitute types while waiting for codegen.
4. Cog refactors the completed build; Gauge runs task-pipeline Phase 3 gates: Roslyn, complexity, InspectCode in changed scope, mutation for production C#, property tests, vulnerability audit, dotnet format verification, dotnet test and web gates. No thresholds are changed. No architect command or Stryker since mode (charter:66-76; task-pipeline:95-107).
5. This brief does not waive property tests or record an opt-out: use the existing gate, and an unaccepted exit-2 remains blocking. Mutation must exercise eligible changed C# projects and report actual results under configured thresholds. Scope-empty SKIPPED, configured SKIP and mutation NOT APPLICABLE are distinct non-blocking verdicts, never PASS. Exit 1 or Could not run blocks.
6. All three review axes are required, with bounded remediation and posted/read-back findings. Delivery remains awaiting-merge; no seat merges or enables auto-merge.

## Remaining state

No remaining decision is required to draft on Patron's recommended scope. The owner checkbox remains the explicit structural blocker for Gate 1. DEV-320 delivery is an unresolved build prerequisite, not an invented completion fact. Direct-link hosting behavior and any forced constructor/test caller paths beyond this named list are not found in these roots and require targeted Wisp recon before dependent implementation or deployment claims. No searches outside the authorized roots have been performed.

## All twelve Patron rulings (source retained verbatim)

The following preserves the closed grill, including its original unverified wording. Q6 is superseded by the 2026-10-12 Patron amendment below; it is historical text, not an active implementation instruction. Q5/Q7 are resolved above by their expressly conditional branches; the original statements of no outstanding recon do not establish codegen delivery.

# DEV-323 - Patron grill conclusions

Grill: `/grill-with-docs DEV-323`, Patron-driven, 12 questions (cap reached exactly). 2026-10-12.
Sources: ticket text (note `DEV-323`:8), `recon-DEV-323` (cited `R:<line>`), `specs/PRODUCT.md` (cited `P:§`), seat charter.
Worktree `F:/Dev/LamuFlix.worktrees/feature-323-spec`, branch `feature/323-spec`, base `cb6fe48`.

## Owner escalation (spec PR checkbox; Gate 1 stays closed until answered)

- [ ] **blocked: structural — Drop `backdrop`, `tagline` and `writers` from DEV-323 and deliver them in a follow-up ticket?**
  The ticket names them, but no column, table or wire field holds them (R:18-20, R:58), so delivering them means a schema change plus enrichment-source work beyond a 1d `size:M` ticket. Dropping them changes what the ticket delivers (P:§5 Ticket change), so it is the owner's call.
  Patron recommends **yes**. The spec proceeds on that basis. If the owner says no, the spec gains a schema/enrichment slice and is re-planned. Rigger files the follow-up ticket only once the owner ticks yes.

## Rulings

Q1. **Backend in scope: extend the GET `/api/movies/{id}` response additively.** (P:§5 item 4 API shape)
- The AC asks for complete metadata on the details page, and the wire carries only title, synopsis, year, runtime, IMDb rating and IMDb id (R:20, R:52).
- Add nullable `posterUrl`, `rottenTomatoesRating`, `metaScore`, plus `directors: string[]` and `actors: string[]` (empty array when none) to `MovieMetadataResponse`, backed by Core `MovieMetadata`/port and the `EfMovieCatalog.GetDetailsAsync` projection (R:17-19). The storage already exists (R:58), so there is **no schema change** (item 3 not triggered).
- Forced edits are in scope: frozen-shape tests (R:32, R:59), the OpenAPI baseline and snapshot regeneration (R:34). No other route, DTO or status code changes.

Q2. **Backdrop, tagline and writers:** see the escalation above. There is no partial or placeholder rendering of these fields in this ticket.

Q3. **Play endpoint and the LocalPlay gate stay unchanged.** (P:§5 item 5)
- POST `/api/movies/{id}/play` already exists with 204/400/403/404/422 (R:23). The gate stays in the adapter, server-side (R:25). The UI never decides playability itself.
- No backend playback, `FeatureOptions`, `Process.Start` or secrets edits. The legacy MVC `MoviesController`/`MovieService` play path is not named by the ticket and stays untouched (R:44, R:60-61; item 6).

Q4. **No feature-flag endpoint.** The Play button always renders and is enabled. The 403 modal is the gate UX the ticket names. (P:§5 item 4; ticket AC)

Q5. **No router dependency.** (P:§5 items 1-2)
- Deliver `MovieDetailsView` as a component that takes a `movieId` prop. `App.tsx`/`main.tsx` are not rewired (item 6; the ticket names a view only, R:61).
- Navigation and route mounting belong to the SPA epic's routing/browse work. *Unverified:* that a sibling ticket under DEV-286 owns routing. If Keel finds none, mount via the existing `App.tsx` with no new dependency, and treat it as a forced edit.

Q6. **Data access: native `fetch` in a small typed module inside the feature folder** (`web/src/features/movie-details/` or similar). No query or state library and no generic API-client layer (P:§2 Speculative Abstractions; items 1-2). Use relative `/api/...` URLs. No `vite.config.ts` proxy edit, because tests use MSW.

Q7. **Contract chain: generate `web/src/api/types.ts`.** Add the `openapi-typescript` npm **devDependency** (exact pinned version) and a `package.json` script that regenerates it from `web/src/api/openapi.json`. (P:§5 item 1; charter contract-chain rule)
- The rule forbids hand-rolled DTO duplicates, and `types.ts` does not exist yet (R:53). This is the minimal codegen with no runtime dependency.
- *Unverified:* whether another DEV-286 ticket already owns codegen. If one does, defer to it and consume its output.

Q8. **Play response handling.** 204: brief inline success status. 403: modal. 404: inline "not found" error. 400/422/5xx/network: inline generic error. The button is disabled while the request is in flight, which prevents double-launch.

Q9. **Modal implementation:** use a hand-built accessible dialog (`role="dialog"`, `aria-modal`, labelled title, focus moves to Close, Escape and Close dismiss, focus returns to Play). There is no UI-library dependency. Native `<dialog>` `showModal` is avoided because the jsdom test environment does not support it reliably (sound engineering practice).

Q10. **Details load states:** loading, 404 not-found, other error, and loaded. Loaded with `metadata: null` shows the title and format plus a no-metadata notice. Each optional field and badge renders only when its value is present: no "N/A" text and no empty badges.

Q11. **Tests:**
- Web: RTL + MSW (named in the ticket) covering full render, partial/null metadata, the GET 404/error states, and Play 204/403(modal open, close, focus)/404/error.
- Backend: xUnit for the new projection and mapping fields (EfMovieCatalog integration, endpoint mapping, e2e frozen shape updated). Existing gates and contract tests apply unchanged.

Q12. **Glossary:** the canonical terms are **Movie Details** (the view and query) and **Playback** (CONTEXT.md:74). Keel/Quill use them in the spec. Patron makes no edit to `CONTEXT.md`, and no ADR is needed (M ticket, no new layer).

## Routing
- No recon outstanding. The two *unverified* points (Q5 routing owner, Q7 codegen owner) are Keel's to confirm against the DEV-286 sibling tickets via Wisp, if needed.
- No YouTrack change now. The follow-up ticket depends on the owner's checkbox.

## Patron taste assumptions (source retained verbatim)

# DEV-323 - Taste assumptions (Patron)

- [assumed] Rating badges are labelled `IMDb`, `Rotten Tomatoes` and `Metacritic`. Each shows the stored value as-is (IMDb `x.x/10`, RT stored string, Metacritic `nn`), and a badge is hidden when its value is null.
- [assumed] Runtime displays as `2h 16m` (minutes only, `45m`, when under one hour). The year displays as plain `YYYY`.
- [assumed] Directors and cast are comma-separated lists labelled `Director`/`Directors` and `Cast`. All stored actors are shown.
- [assumed] The poster `alt` is `<title> poster`. A missing poster renders a neutral placeholder block with no text.
- [assumed] The Play button label is `Play`, and it reads `Starting…` while in flight. On 204 the status reads `Playback started.`
- [assumed] The 403 modal title is `Playback unavailable`, its body is `Local playback is disabled on this server.`, and its button is `Close`.
- [assumed] Load states read `Loading movie…`, `Movie not found.` and `Couldn't load this movie. Try again later.` The no-metadata notice reads `No metadata available yet.`
- [assumed] The Play 404 message is `This movie is no longer in the library.` The generic Play error reads `Couldn't start playback. Try again later.`

## Patron stack amendment and analysis repair authority (2026-10-12)

Patron rules: **follow the constitution stack: TanStack Query, Zod, the DEV-320 openapi-fetch client, and generated MSW handlers. Prior Q6 is superseded. No constitution departure and no new owner checkbox.** Authority: DEV-323:60 finding (1), DEV-323:63-67 Patron ruling and next action, and the Conductor clearance in this ask. The cited constitution basis is the stack mandate at constitution:313-335 and generated client/MSW rules at constitution:470-472, as recorded in DEV-323:60; these are note-supplied citations, not a new read of the constitution outside the named artifacts. Plan Constitution Check must carry the principle-number citations required by constitution:529, also recorded there.

This amendment overrides the retained original Q6 and any native-fetch/no-query instruction. Q7 remains deferred to DEV-320 ownership (recon-DEV-323:78-83): DEV-323 consumes and regenerates delivered outputs for its additive contract, never creates a competing client/generator or hand-writes generated artifacts. TanStack Query and Zod are the decided stack, not a constitution exception. Exact package pins, delivered client/handler/provider paths and any necessary consuming configuration edits are not found in these roots; preserve those as targeted Phase B recon, not invented file scope or delivery evidence.

No further Patron decision is needed for Quill to repair the drafts. Apply the existing numbered findings in DEV-323:60 as one bounded round 1 fix list: (1) align stack and constitution citations across drafts; (2) separate Phase A checkbox/PR preparation from Phase B, requiring merged spec PR and answered owner checkbox before build; owner no requires re-plan; (3) remove conflicting parallel markers, serialize same-file tests and dependent Core -> EF -> API/tests -> OpenAPI -> generated outputs -> web work; (4) retain implementation TDD/contract checks but make final gates Gauge-owned after Cog refactor, with unaccepted property-test exit-2 blocking; (5) align file inventory to this brief, omit unapproved query/handler edits, list the verified snapshot output, and keep OpenApiContractTests source read-only for regeneration and unlocated caller paths deferred.

The backdrop/tagline/writers owner checkbox stays unanswered and Gate 1 stays closed. Deferred Phase B recon is preserved: delivered DEV-320 tooling/client/generated handlers and stack integration, direct-link hosting support, and forced Core constructor/test caller paths. DEV-320 delivery and parentage remain NOT DELIVERED / NOT EVIDENCED at the supplied recon HEAD. This records a decision and unblocks draft repair only; analysis is still NOT CLEAR pending Quill repair and Keel read-only re-analysis. It does not freeze the plan or authorize build, gates, commits or delivery.